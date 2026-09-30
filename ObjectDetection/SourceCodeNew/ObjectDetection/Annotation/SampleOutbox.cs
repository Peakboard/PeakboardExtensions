using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using Peakboard.ExtensionKit;
using PeakboardExtensionObjectDetection.Data;

namespace PeakboardExtensionObjectDetection.Annotation
{
    /// <summary>
    /// Saved samples, on their way to the Hub dataset.
    ///
    /// Saving writes the frame and its boxes to disk FIRST and uploads afterwards, from a
    /// background thread. A Lua function only gets a short slice of time - an HTTP request does
    /// not fit into it (see the ExchangeRates extension) - and an operator whose Hub is
    /// unreachable must not lose the objects they just marked. A sample stays in the outbox
    /// until the Hub accepted it, and is retried every 30 seconds.
    ///
    /// Per sample, in &lt;ProgramData&gt;\Peakboard\ObjectDetection\datasets\&lt;dataset&gt;\outbox:
    ///   &lt;id&gt;.jpg            the frozen frame, exactly as the camera delivered it
    ///   &lt;id&gt;.json           the boxes, in pixels and normalised
    ///   &lt;id&gt;.drawings.json  the operator's outlines the boxes were derived from
    /// Uploaded samples move to ...\uploaded.
    /// </summary>
    public static class SampleOutbox
    {
        private static readonly object _lock = new object();
        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        private static readonly AutoResetEvent _wake = new AutoResetEvent(false);
        private static readonly TimeSpan RetryInterval = TimeSpan.FromSeconds(30);

        private static Thread _worker;
        private static string _hubUrl = "";
        private static string _userGroupKey = "";
        private static string _dataset = "";
        private static string _uploadStatus = "Nothing to upload.";
        private static ILoggingService _log;

        public static string Dataset { get { lock (_lock) return _dataset; } }
        public static string UploadStatus { get { lock (_lock) return _uploadStatus; } }

        /// <summary>Saved samples not yet accepted by the Hub - in every dataset's outbox.</summary>
        public static int PendingCount
        {
            get
            {
                try { return WaitingSamples().Count; }
                catch { return 0; }
            }
        }

        /// <summary>Takes the Hub connection from the list's properties and starts the upload thread.</summary>
        public static void Configure(string hubUrl, string userGroupKey, string dataset, ILoggingService log)
        {
            lock (_lock)
            {
                _dataset = (dataset ?? "").Trim();
            }
            ConfigureHub(hubUrl, userGroupKey, log);
        }

        /// <summary>
        /// The Hub connection only; the dataset stays what the Annotation list set.
        ///
        /// The outbox is shared by the Annotation and the Suggestions list. A list that leaves
        /// HubUrl or UserGroupKey empty does not take them away from the other one - otherwise one
        /// tap on the Suggestions list stranded every sample the Annotation list had saved.
        /// </summary>
        public static void ConfigureHub(string hubUrl, string userGroupKey, ILoggingService log)
        {
            lock (_lock)
            {
                var url = (hubUrl ?? "").Trim().TrimEnd('/');
                var key = (userGroupKey ?? "").Trim();
                if (url.Length > 0) _hubUrl = url;
                if (key.Length > 0) _userGroupKey = key;
                if (log != null) _log = log;

                if (_worker == null || !_worker.IsAlive)
                {
                    _worker = new Thread(Run) { IsBackground = true, Name = "ObjectDetection sample upload" };
                    _worker.Start();
                }
            }
            _wake.Set();
        }

        /// <summary>Writes the sample to the outbox and wakes the uploader. Returns the sample id.</summary>
        public static string Save(AnnotationSnapshot s) => Save(s, Dataset);

        /// <summary>Same, for a list that names its own dataset (the Suggestions list).</summary>
        public static string Save(AnnotationSnapshot s, string dataset)
        {
            dataset = (dataset ?? "").Trim();
            var dir = OutboxDir(dataset);
            Directory.CreateDirectory(dir);

            var sampleId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)
                + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);

            // The JSON last: the uploader only picks up a sample whose .json exists, so it never
            // sees one half written.
            File.WriteAllBytes(Path.Combine(dir, sampleId + ".jpg"), s.FrameJpeg);
            File.WriteAllText(Path.Combine(dir, sampleId + ".drawings.json"), DrawingsJson(s), Encoding.UTF8);
            File.WriteAllText(Path.Combine(dir, sampleId + ".json"), SampleJson(sampleId, dataset, s), Encoding.UTF8);

            lock (_lock) { _uploadStatus = $"Sample {sampleId} saved, uploading..."; }
            _wake.Set();
            return sampleId;
        }

        private static void Run()
        {
            while (true)
            {
                try { UploadPending(); }
                catch (Exception ex)
                {
                    SetStatus($"Upload failed: {ex.Message}");
                    _log?.Error($"[ObjectDetection] Sample upload failed: {ex}");
                }
                _wake.WaitOne(RetryInterval);
            }
        }

        private static void UploadPending()
        {
            string hubUrl, key;
            lock (_lock) { hubUrl = _hubUrl; key = _userGroupKey; }

            // Every outbox, not only the one of the current DatasetName: a sample belongs to the
            // dataset it was saved for. Renaming DatasetName while samples wait must not strand them.
            var samples = WaitingSamples();
            if (samples.Count == 0) return;

            if (hubUrl.Length == 0 || key.Length == 0)
            {
                SetStatus($"{samples.Count} sample(s) kept on this device: HubUrl or UserGroupKey is not set.");
                return;
            }

            var done = 0;
            foreach (var sampleFile in samples)
            {
                var dir = Path.GetDirectoryName(sampleFile);
                var sampleId = Path.GetFileNameWithoutExtension(sampleFile);
                var dataset = DatasetOf(sampleFile);
                var error = Upload(hubUrl, key, dataset, dir, sampleId, out var permanent);
                if (error != null && permanent)
                {
                    // The Hub will never take this one. Keeping it at the head of the queue would
                    // block every sample saved after it, so it is put aside and the queue goes on.
                    MoveTo("rejected", dir, sampleId);
                    done++;
                    SetStatus($"Sample {sampleId} was rejected by the Hub ({error}) and put aside in the \"rejected\" folder.");
                    _log?.Error($"[ObjectDetection] Sample {sampleId} rejected: {error}");
                    continue;
                }
                if (error != null)
                {
                    SetStatus($"{samples.Count - done} sample(s) waiting: {error}");
                    return;
                }

                MoveTo("uploaded", dir, sampleId);
                done++;
                SetStatus($"Sample {sampleId} uploaded to dataset \"{dataset}\".");
            }
        }

        /// <summary>The samples in every dataset's outbox, oldest first.</summary>
        private static List<string> WaitingSamples()
        {
            var root = Path.Combine(PathHelper.BaseDir, "datasets");
            if (!Directory.Exists(root)) return new List<string>();
            return Directory.GetDirectories(root)
                .Select(d => Path.Combine(d, "outbox"))
                .Where(Directory.Exists)
                .SelectMany(d => Directory.GetFiles(d, "*.json"))
                .Where(f => !f.EndsWith(".drawings.json", StringComparison.OrdinalIgnoreCase))
                .OrderBy(f => Path.GetFileName(f), StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>The dataset a sample was saved for - written into its .json at save time.</summary>
        private static string DatasetOf(string sampleFile)
        {
            try
            {
                using (var doc = JsonDocument.Parse(File.ReadAllText(sampleFile, Encoding.UTF8)))
                {
                    if (doc.RootElement.TryGetProperty("dataset", out var d)
                        && d.ValueKind == JsonValueKind.String && d.GetString().Length > 0)
                        return d.GetString();
                }
            }
            catch (JsonException) { }
            return Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(sampleFile)));
        }

        /// <summary>
        /// Returns null on success, otherwise the reason in words an operator can act on.
        /// permanent: the Hub understood the request and refused THIS sample (a 4xx other than the
        /// key, a missing endpoint, a timeout or throttling) - sending it again cannot help.
        ///
        /// The Hub's contract (ObjectDetectionManager/UploadSample): form fields <c>file</c> (the
        /// JPEG), <c>dataset</c>, <c>sourceDevice</c> and <c>annotations</c> - a JSON array of boxes
        /// relative to the image (0..1, top-left origin) naming their class by <c>className</c>, with
        /// <c>origin</c> "AcceptedSuggestion" for an accepted detection. The operator's drawings stay
        /// on the device (the Hub stores boxes only), in the "uploaded" folder beside the frame.
        /// </summary>
        private static string Upload(string hubUrl, string key, string dataset, string dir, string sampleId, out bool permanent)
        {
            permanent = false;
            var url = $"{hubUrl}/api/ObjectDetectionManager/UploadSample";

            string annotations, device;
            try { annotations = HubAnnotations(Path.Combine(dir, sampleId + ".json"), out device); }
            catch (Exception ex) when (ex is JsonException || ex is InvalidOperationException || ex is KeyNotFoundException)
            {
                permanent = true;
                return $"the saved boxes cannot be read ({ex.Message}).";
            }

            using (var content = new MultipartFormDataContent())
            {
                var image = new ByteArrayContent(File.ReadAllBytes(Path.Combine(dir, sampleId + ".jpg")));
                image.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                content.Add(image, "file", sampleId + ".jpg");
                content.Add(new StringContent(dataset, Encoding.UTF8), "dataset");
                content.Add(new StringContent(device, Encoding.UTF8), "sourceDevice");
                content.Add(new StringContent(annotations, Encoding.UTF8), "annotations");

                using (var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content })
                {
                    request.Headers.Add("UserGroupKey", key);

                    HttpResponseMessage response;
                    try { response = _http.SendAsync(request).GetAwaiter().GetResult(); }
                    catch (Exception ex) when (ex is HttpRequestException || ex is System.Threading.Tasks.TaskCanceledException)
                    {
                        return $"the Hub at {hubUrl} cannot be reached ({ex.Message}).";
                    }

                    using (response)
                    {
                        var body = "";
                        try { body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult(); } catch { }
                        var hubError = HubError(body);

                        // The Hub answers errors with their status code AND lists them in the body.
                        if (response.IsSuccessStatusCode && hubError.Length == 0) return null;
                        var code = (int)response.StatusCode;
                        var reason = hubError.Length > 0 ? hubError : response.ReasonPhrase;
                        switch (response.StatusCode)
                        {
                            case HttpStatusCode.Unauthorized:
                            case HttpStatusCode.Forbidden:
                                return "the Hub refused the UserGroupKey.";
                            case HttpStatusCode.NotFound:
                                return hubError.Length > 0
                                    ? $"the Hub answered 404: {hubError}"
                                    : "the Hub has no Object Detection endpoint (404) - is it a Hub with Object Detection?";
                            case HttpStatusCode.RequestTimeout:
                            case (HttpStatusCode)429:
                                return $"the Hub answered {code}: {reason}";
                            default:
                                permanent = code >= 400 && code < 500;
                                return $"the Hub answered {code}: {reason}";
                        }
                    }
                }
            }
        }

        /// <summary>
        /// The saved boxes (pixels) as the Hub takes them: relative to the frame, clamped into it.
        /// A box that reaches past the frame edge would otherwise be refused, and with it the frame.
        /// </summary>
        private static string HubAnnotations(string sampleFile, out string device)
        {
            using (var doc = JsonDocument.Parse(File.ReadAllText(sampleFile, Encoding.UTF8)))
            {
                var root = doc.RootElement;
                device = root.TryGetProperty("device", out var d) && d.ValueKind == JsonValueKind.String
                    ? d.GetString() ?? "" : Environment.MachineName;
                var image = root.GetProperty("image");
                var frameW = image.GetProperty("width").GetDouble();
                var frameH = image.GetProperty("height").GetDouble();
                if (frameW <= 0 || frameH <= 0) throw new InvalidOperationException("the frame size is missing");

                using (var buffer = new MemoryStream())
                {
                    using (var w = new Utf8JsonWriter(buffer))
                    {
                        w.WriteStartArray();
                        foreach (var a in root.GetProperty("annotations").EnumerateArray())
                        {
                            var x1 = Clamp(a.GetProperty("x").GetDouble(), 0, frameW);
                            var y1 = Clamp(a.GetProperty("y").GetDouble(), 0, frameH);
                            var x2 = Clamp(a.GetProperty("x").GetDouble() + a.GetProperty("width").GetDouble(), 0, frameW);
                            var y2 = Clamp(a.GetProperty("y").GetDouble() + a.GetProperty("height").GetDouble(), 0, frameH);
                            if (x2 - x1 < 1 || y2 - y1 < 1) continue;

                            // Samples saved by 1.1 wrote "box"; the Hub knows the names below.
                            var origin = a.TryGetProperty("origin", out var o) && o.ValueKind == JsonValueKind.String
                                && string.Equals(o.GetString(), AnnotatedObject.OriginAcceptedSuggestion, StringComparison.OrdinalIgnoreCase)
                                ? AnnotatedObject.OriginAcceptedSuggestion : AnnotatedObject.OriginBox;

                            w.WriteStartObject();
                            w.WriteString("className", a.GetProperty("className").GetString() ?? "");
                            w.WriteNumber("x", Math.Round(x1 / frameW, 6));
                            w.WriteNumber("y", Math.Round(y1 / frameH, 6));
                            w.WriteNumber("width", Math.Round((x2 - x1) / frameW, 6));
                            w.WriteNumber("height", Math.Round((y2 - y1) / frameH, 6));
                            w.WriteString("origin", origin);
                            w.WriteEndObject();
                        }
                        w.WriteEndArray();
                    }
                    return Encoding.UTF8.GetString(buffer.ToArray());
                }
            }
        }

        private static double Clamp(double v, double min, double max) => Math.Max(min, Math.Min(max, v));

        /// <summary>The first entry of the Hub's <c>errors</c> array, or "".</summary>
        private static string HubError(string body)
        {
            if (string.IsNullOrWhiteSpace(body)) return "";
            try
            {
                using (var doc = JsonDocument.Parse(body))
                {
                    if (doc.RootElement.ValueKind != JsonValueKind.Object) return "";
                    foreach (var p in doc.RootElement.EnumerateObject())
                    {
                        if (!string.Equals(p.Name, "errors", StringComparison.OrdinalIgnoreCase)) continue;
                        if (p.Value.ValueKind != JsonValueKind.Array) return "";
                        foreach (var e in p.Value.EnumerateArray())
                            if (e.ValueKind == JsonValueKind.String) return e.GetString() ?? "";
                    }
                }
            }
            catch (JsonException) { }
            return "";
        }

        private static void MoveTo(string folder, string dir, string sampleId)
        {
            var target = Path.Combine(Path.GetDirectoryName(dir), folder);
            Directory.CreateDirectory(target);
            foreach (var suffix in new[] { ".jpg", ".drawings.json", ".json" })
            {
                var from = Path.Combine(dir, sampleId + suffix);
                var to = Path.Combine(target, sampleId + suffix);
                if (File.Exists(to)) File.Delete(to);
                if (File.Exists(from)) File.Move(from, to);
            }
        }

        private static string SampleJson(string sampleId, string dataset, AnnotationSnapshot s)
        {
            var device = Environment.MachineName;
            using (var buffer = new MemoryStream())
            {
                using (var w = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
                {
                    w.WriteStartObject();
                    w.WriteString("sampleId", sampleId);
                    w.WriteString("dataset", dataset);
                    w.WriteString("device", device);
                    w.WriteString("capturedAt", s.CapturedAt);
                    w.WriteStartObject("image");
                    w.WriteString("fileName", sampleId + ".jpg");
                    w.WriteNumber("width", s.FrameWidth);
                    w.WriteNumber("height", s.FrameHeight);
                    w.WriteEndObject();
                    w.WriteStartArray("annotations");
                    foreach (var o in s.Objects)
                    {
                        w.WriteStartObject();
                        w.WriteString("className", o.ClassName);
                        w.WriteNumber("x", o.Box.X);
                        w.WriteNumber("y", o.Box.Y);
                        w.WriteNumber("width", o.Box.Width);
                        w.WriteNumber("height", o.Box.Height);
                        // YOLO convention: centre and size relative to the frame.
                        w.WriteNumber("xCenter", Math.Round((o.Box.X + o.Box.Width / 2) / s.FrameWidth, 6));
                        w.WriteNumber("yCenter", Math.Round((o.Box.Y + o.Box.Height / 2) / s.FrameHeight, 6));
                        w.WriteNumber("widthNormalized", Math.Round(o.Box.Width / s.FrameWidth, 6));
                        w.WriteNumber("heightNormalized", Math.Round(o.Box.Height / s.FrameHeight, 6));
                        w.WriteString("origin", string.IsNullOrEmpty(o.Origin) ? AnnotatedObject.OriginBox : o.Origin);
                        if (o.Confidence.HasValue) w.WriteNumber("confidence", Math.Round(o.Confidence.Value, 4));
                        w.WriteString("author", device);
                        w.WriteString("createdAt", o.CreatedAt);
                        w.WriteEndObject();
                    }
                    w.WriteEndArray();
                    w.WriteEndObject();
                }
                return Encoding.UTF8.GetString(buffer.ToArray());
            }
        }

        private static string DrawingsJson(AnnotationSnapshot s)
        {
            using (var buffer = new MemoryStream())
            {
                using (var w = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
                {
                    w.WriteStartArray();
                    for (var i = 0; i < s.Objects.Count; i++)
                    {
                        w.WriteStartObject();
                        w.WriteNumber("annotationIndex", i);
                        w.WriteString("className", s.Objects[i].ClassName);
                        w.WritePropertyName("drawing");
                        // An accepted suggestion was never drawn.
                        if (string.IsNullOrWhiteSpace(s.Objects[i].DrawingJson))
                            w.WriteNullValue();
                        else
                            using (var doc = JsonDocument.Parse(s.Objects[i].DrawingJson))
                                doc.RootElement.WriteTo(w);
                        w.WriteEndObject();
                    }
                    w.WriteEndArray();
                }
                return Encoding.UTF8.GetString(buffer.ToArray());
            }
        }

        private static void SetStatus(string status)
        {
            lock (_lock) { _uploadStatus = status; }
        }

        private static string DatasetDir(string dataset)
        {
            var safe = string.Concat((dataset.Length == 0 ? "default" : dataset)
                .Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
            return Path.Combine(PathHelper.BaseDir, "datasets", safe);
        }

        private static string OutboxDir(string dataset) => Path.Combine(DatasetDir(dataset), "outbox");
    }
}
