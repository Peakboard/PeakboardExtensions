using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using Peakboard.ExtensionKit;
using PeakboardExtensionObjectDetection.Inference;

namespace PeakboardExtensionObjectDetection.Data
{
    /// <summary>
    /// Keeps the model of a Hub dataset on this device: asks the Peakboard Hub for the dataset's
    /// current model version, downloads it when it is not the one installed, checks it, and puts
    /// it where the engine loads ModelName from. The engine then swaps it in while it runs.
    ///
    /// The Box always pulls; the Hub never pushes. The device switches whenever the Hub's version
    /// is DIFFERENT from its own - not only when it is higher - so a rollback in the Hub reaches
    /// the device the same way a new version does. While the Hub cannot be reached nothing is
    /// touched and the model that runs keeps running.
    ///
    /// Installed as &lt;ProgramData&gt;\Peakboard\ObjectDetection\models\&lt;ModelName&gt;\:
    ///   model.onnx   the model
    ///   classes.txt  one class name per line, line n = class id n
    ///   hub.json     dataset, version and SHA-256 of what is installed - written last
    /// </summary>
    public static class HubModelSync
    {
        private const string InstalledInfoFile = "hub.json";
        private static readonly TimeSpan FailedVersionPause = TimeSpan.FromMinutes(10);

        private static readonly object _lock = new object();
        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        private static readonly AutoResetEvent _wake = new AutoResetEvent(false);

        private static Thread _worker;
        private static volatile bool _running;
        private static string _hubUrl = "";
        private static string _userGroupKey = "";
        private static string _dataset = "";
        private static string _modelName = "";
        private static TimeSpan _interval = TimeSpan.FromSeconds(60);
        private static ILoggingService _log;

        private static string _status = "Hub model sync is off.";
        private static int _hubVersion;
        private static string _lastCheck = "";

        // A version that failed to download or to load is not fetched again every minute.
        private static string _failedKey = "";
        private static DateTime _failedAt;
        private static string _failedReason = "";

        /// <summary>What the sync did last, in words; never empty.</summary>
        public static string Status { get { lock (_lock) return _status; } }

        /// <summary>The dataset's current version as the Hub reported it last; 0 = unknown / no model.</summary>
        public static int HubVersion { get { lock (_lock) return _hubVersion; } }

        public static string LastCheck { get { lock (_lock) return _lastCheck; } }

        /// <summary>
        /// Takes the settings of the Detections list and starts checking. Without a Hub address,
        /// key and dataset the sync stays off and says so.
        /// </summary>
        public static void Configure(string hubUrl, string userGroupKey, string dataset,
            string modelName, double intervalSeconds, ILoggingService log)
        {
            lock (_lock)
            {
                _hubUrl = (hubUrl ?? "").Trim().TrimEnd('/');
                _userGroupKey = (userGroupKey ?? "").Trim();
                _dataset = (dataset ?? "").Trim();
                _modelName = (modelName ?? "").Trim();
                _interval = TimeSpan.FromSeconds(Math.Max(10, double.IsNaN(intervalSeconds) ? 60 : intervalSeconds));
                if (log != null) _log = log;

                if (_hubUrl.Length == 0 && _userGroupKey.Length == 0 && _dataset.Length == 0)
                {
                    // Not asked for - the board uses a local model.
                    _status = "Hub model sync is off.";
                    _running = false;
                    _wake.Set();
                    return;
                }

                _running = true;
                if (_worker == null || !_worker.IsAlive)
                {
                    _worker = new Thread(Run) { IsBackground = true, Name = "ObjectDetection Hub model sync" };
                    _worker.Start();
                }
            }
            _wake.Set();
        }

        public static void Stop()
        {
            _running = false;
            _wake.Set();
        }

        /// <summary>
        /// The Hub version of the model file at <paramref name="onnxPath"/>, read from hub.json
        /// beside it; 0 when the model did not come from the Hub.
        /// </summary>
        public static int ReadInstalledVersion(string onnxPath)
        {
            try
            {
                var dir = Path.GetDirectoryName(onnxPath ?? "");
                if (string.IsNullOrEmpty(dir)) return 0;
                var info = ReadInstalled(dir);
                return info?.Version ?? 0;
            }
            catch { return 0; }
        }

        private static void Run()
        {
            while (_running)
            {
                try { CheckOnce(); }
                catch (Exception ex)
                {
                    SetStatus($"Hub model check failed: {ex.Message}");
                    _log?.Error($"[ObjectDetection] Hub model check failed: {ex}");
                }

                TimeSpan wait;
                lock (_lock) wait = _interval;
                _wake.WaitOne(wait);
            }
        }

        /// <summary>
        /// One round: ask, compare, download, check, install. Never throws for a Hub problem.
        /// Rounds never overlap - two of them would share the staging folder.
        /// </summary>
        public static void CheckOnce()
        {
            lock (_checkLock) CheckOnceLocked();
        }

        private static readonly object _checkLock = new object();

        private static void CheckOnceLocked()
        {
            string hubUrl, key, dataset, modelName;
            lock (_lock)
            {
                hubUrl = _hubUrl; key = _userGroupKey; dataset = _dataset; modelName = _modelName;
                _lastCheck = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            }

            var missing = new[] { ("HubUrl", hubUrl), ("UserGroupKey", key), ("DatasetName", dataset), ("ModelName", modelName) }
                .Where(p => p.Item2.Length == 0).Select(p => p.Item1).ToList();
            if (missing.Count > 0)
            {
                SetStatus($"Hub model sync needs {string.Join(", ", missing)} on the Detections data source.");
                return;
            }

            var targetDir = TargetDir(modelName, out var nameProblem);
            if (targetDir == null)
            {
                SetStatus(nameProblem);
                return;
            }

            var installed = ReadInstalled(targetDir);
            var installedText = installed != null ? $"version {installed.Version}" : "no Hub model";

            // 1. Which version is current in the Hub?
            var current = GetCurrent(hubUrl, key, dataset, out var problem);
            if (current == null)
            {
                SetStatus($"{problem} Keeping {installedText}.");
                return;
            }
            lock (_lock) _hubVersion = current.Version;

            if (current.Version == 0)
            {
                SetStatus($"Dataset \"{dataset}\" has no model in the Hub yet. Keeping {installedText}.");
                return;
            }

            if (installed != null && installed.Version == current.Version
                && string.Equals(installed.Sha256, current.Sha256, StringComparison.OrdinalIgnoreCase)
                && File.Exists(Path.Combine(targetDir, "model.onnx")))
            {
                SetStatus($"Up to date: version {current.Version} of \"{dataset}\".");
                return;
            }

            var attemptKey = $"{current.Version}:{current.Sha256}";
            lock (_lock)
            {
                if (_failedKey == attemptKey && DateTime.UtcNow - _failedAt < FailedVersionPause)
                {
                    _status = $"Version {current.Version} was not installed: {_failedReason} Keeping {installedText}; trying again at {(_failedAt + FailedVersionPause).ToLocalTime():HH:mm}.";
                    return;
                }
            }

            // 2. Download, check, install.
            SetStatus($"Downloading version {current.Version} of \"{dataset}\"...");
            var error = DownloadAndInstall(hubUrl, key, dataset, current, targetDir);
            if (error != null)
            {
                lock (_lock)
                {
                    _failedKey = attemptKey;
                    _failedAt = DateTime.UtcNow;
                    _failedReason = error;
                }
                SetStatus($"Version {current.Version} was not installed: {error} Keeping {installedText}.");
                _log?.Error($"[ObjectDetection] Hub model version {current.Version} not installed: {error}");
                return;
            }

            lock (_lock) _failedKey = "";
            var direction = installed == null ? "installed"
                : current.Version < installed.Version ? $"rolled back from version {installed.Version}"
                : $"replaces version {installed.Version}";
            SetStatus($"Version {current.Version} of \"{dataset}\" {direction}; the engine is loading it.");
            _log?.Info($"[ObjectDetection] Hub model version {current.Version} of '{dataset}' {direction}.");
            DetectionEngine.RequestModelCheck();
        }

        private sealed class HubModel
        {
            public int Version;
            public string Sha256 = "";
            public int ClassCount;
        }

        private sealed class InstalledModel
        {
            public int Version;
            public string Sha256 = "";
            public string Dataset = "";
        }

        private static HubModel GetCurrent(string hubUrl, string key, string dataset, out string problem)
        {
            problem = null;
            var url = $"{hubUrl}/api/ObjectDetectionManager/CurrentModel?dataset={Uri.EscapeDataString(dataset)}";
            string body;
            HttpStatusCode code;
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    request.Headers.Add("UserGroupKey", key);
                    using (var response = _http.SendAsync(request).GetAwaiter().GetResult())
                    {
                        code = response.StatusCode;
                        body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    }
                }
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is System.Threading.Tasks.TaskCanceledException)
            {
                problem = $"The Hub at {hubUrl} cannot be reached ({ex.Message}).";
                return null;
            }

            if (code == HttpStatusCode.Unauthorized || code == HttpStatusCode.Forbidden)
            {
                problem = "The Hub refused the UserGroupKey.";
                return null;
            }

            try
            {
                using (var doc = JsonDocument.Parse(body))
                {
                    var root = doc.RootElement;
                    if ((int)code >= 400 || HasErrors(root))
                    {
                        problem = code == HttpStatusCode.NotFound && FirstError(root).Length == 0
                            ? "The Hub has no object detection endpoint (404) - is it a Hub with Object Detection?"
                            : $"The Hub answered {(int)code}: {FirstError(root)}";
                        return null;
                    }

                    if (!TryGet(root, "data", out var data) || data.ValueKind != JsonValueKind.Object)
                    {
                        problem = "The Hub's answer has no data.";
                        return null;
                    }

                    var model = new HubModel();
                    if (TryGet(data, "version", out var v) && v.ValueKind == JsonValueKind.Number)
                        model.Version = v.GetInt32();
                    if (TryGet(data, "model", out var m) && m.ValueKind == JsonValueKind.Object)
                    {
                        if (TryGet(m, "sha256", out var sha) && sha.ValueKind == JsonValueKind.String)
                            model.Sha256 = sha.GetString() ?? "";
                        if (TryGet(m, "classCount", out var cc) && cc.ValueKind == JsonValueKind.Number)
                            model.ClassCount = cc.GetInt32();
                    }
                    return model;
                }
            }
            catch (JsonException)
            {
                problem = $"The Hub answered {(int)code} with something that is not JSON - is {hubUrl} a Peakboard Hub?";
                return null;
            }
        }

        /// <summary>Returns null when the model is installed, otherwise why not.</summary>
        private static string DownloadAndInstall(string hubUrl, string key, string dataset, HubModel current, string targetDir)
        {
            var url = $"{hubUrl}/api/ObjectDetectionManager/DownloadModel?dataset={Uri.EscapeDataString(dataset)}" +
                      $"&version={current.Version.ToString(CultureInfo.InvariantCulture)}";
            byte[] package;
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    request.Headers.Add("UserGroupKey", key);
                    using (var response = _http.SendAsync(request).GetAwaiter().GetResult())
                    {
                        package = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                        var mediaType = response.Content.Headers.ContentType?.MediaType ?? "";
                        if (!response.IsSuccessStatusCode || mediaType.Contains("json"))
                        {
                            var text = Encoding.UTF8.GetString(package);
                            string reason = text;
                            try { using (var doc = JsonDocument.Parse(text)) reason = FirstError(doc.RootElement); }
                            catch (JsonException) { }
                            return $"the Hub answered {(int)response.StatusCode} to the download: {reason}";
                        }
                    }
                }
            }
            catch (Exception ex) when (ex is HttpRequestException || ex is System.Threading.Tasks.TaskCanceledException)
            {
                return $"the download from {hubUrl} broke off ({ex.Message}).";
            }

            var staging = Path.Combine(PathHelper.ModelsDir, ".staging-" + Path.GetFileName(targetDir));
            try
            {
                if (Directory.Exists(staging)) Directory.Delete(staging, true);
                Directory.CreateDirectory(staging);
                var stagedOnnx = Path.Combine(staging, "model.onnx");
                var stagedClasses = Path.Combine(staging, "classes.txt");

                using (var zip = new ZipArchive(new MemoryStream(package), ZipArchiveMode.Read))
                {
                    var modelEntry = FindModelEntry(zip);
                    if (modelEntry == null) return "the package from the Hub contains no .onnx model.";
                    var classesEntry = zip.Entries.FirstOrDefault(e => string.Equals(e.FullName, "classes.txt", StringComparison.OrdinalIgnoreCase));
                    if (classesEntry == null) return "the package from the Hub contains no classes.txt.";
                    modelEntry.ExtractToFile(stagedOnnx, true);
                    classesEntry.ExtractToFile(stagedClasses, true);
                }

                // The file must be the one the Hub announced - a cut-off download or a proxy
                // that rewrote it would otherwise replace a working model.
                var sha = Sha256Of(stagedOnnx);
                if (current.Sha256.Length > 0 && !string.Equals(sha, current.Sha256, StringComparison.OrdinalIgnoreCase))
                    return $"the model's SHA-256 is {sha}, the Hub announced {current.Sha256}.";

                // Load it once before it replaces anything: a model that does not load, or whose
                // class list does not match its output, must not overwrite the one on disk - after
                // a restart there would be nothing working left.
                using (var probe = new InferenceService())
                    probe.LoadModel(stagedOnnx, stagedClasses);

                Directory.CreateDirectory(targetDir);
                // Classes first, model second, hub.json last: the engine only takes a model whose
                // files are both in place, and hub.json is what says the install is complete.
                // File.Copy keeps the source's time stamp, and the engine notices a new model by
                // its time stamp - so each file is stamped with the moment it was installed.
                var now = DateTime.UtcNow;
                File.Copy(stagedClasses, Path.Combine(targetDir, "classes.txt"), true);
                File.SetLastWriteTimeUtc(Path.Combine(targetDir, "classes.txt"), now);
                File.Copy(stagedOnnx, Path.Combine(targetDir, "model.onnx"), true);
                File.SetLastWriteTimeUtc(Path.Combine(targetDir, "model.onnx"), now);
                WriteInstalled(targetDir, new InstalledModel { Version = current.Version, Sha256 = sha, Dataset = dataset }, hubUrl);
                return null;
            }
            catch (InvalidDataException ex) { return $"the package from the Hub is not a readable ZIP ({ex.Message})."; }
            catch (IOException ex) { return $"it could not be written on this device ({ex.Message})."; }
            catch (UnauthorizedAccessException ex) { return $"it could not be written on this device ({ex.Message})."; }
            catch (Exception ex) when (ex is InvalidOperationException || ex is Microsoft.ML.OnnxRuntime.OnnxRuntimeException)
            {
                return $"the model does not load: {ex.Message}";
            }
            finally
            {
                try { if (Directory.Exists(staging)) Directory.Delete(staging, true); } catch { }
            }
        }

        /// <summary>The model file the package's classes.json names, else its only .onnx.</summary>
        private static ZipArchiveEntry FindModelEntry(ZipArchive zip)
        {
            var manifest = zip.Entries.FirstOrDefault(e => string.Equals(e.FullName, "classes.json", StringComparison.OrdinalIgnoreCase));
            if (manifest != null)
            {
                try
                {
                    using (var stream = manifest.Open())
                    using (var doc = JsonDocument.Parse(stream))
                    {
                        if (TryGet(doc.RootElement, "modelFile", out var f) && f.ValueKind == JsonValueKind.String)
                        {
                            var entry = zip.Entries.FirstOrDefault(e => e.FullName == f.GetString());
                            if (entry != null) return entry;
                        }
                    }
                }
                catch (JsonException) { }
            }
            return zip.Entries.FirstOrDefault(e => e.FullName.EndsWith(".onnx", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// The folder the Hub model goes to - the custom-model folder of ModelName. Null when
        /// ModelName is taken by a model shipped with the extension or added as a Resource: those
        /// are found first, so a Hub model under the same name would never be loaded.
        /// </summary>
        private static string TargetDir(string modelName, out string problem)
        {
            problem = null;
            if (modelName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || modelName.StartsWith("."))
            {
                problem = $"ModelName \"{modelName}\" cannot be a folder name. Use letters, digits, '-' or '_'.";
                return null;
            }

            var taken = new ModelManager().GetAvailableModels()
                .FirstOrDefault(m => m.Source != "Custom" && string.Equals(m.Name, modelName, StringComparison.OrdinalIgnoreCase));
            if (taken != null)
            {
                problem = $"ModelName \"{modelName}\" is the {taken.Source.ToLowerInvariant()} model of that name, so a Hub model would never be loaded under it. Give the Hub model a name of its own, e.g. the dataset name.";
                return null;
            }

            PathHelper.EnsureDirectories();
            return Path.Combine(PathHelper.ModelsDir, modelName);
        }

        private static InstalledModel ReadInstalled(string dir)
        {
            var path = Path.Combine(dir, InstalledInfoFile);
            if (!File.Exists(path)) return null;
            try
            {
                using (var doc = JsonDocument.Parse(File.ReadAllText(path, Encoding.UTF8)))
                {
                    var root = doc.RootElement;
                    var info = new InstalledModel();
                    if (TryGet(root, "version", out var v) && v.ValueKind == JsonValueKind.Number) info.Version = v.GetInt32();
                    if (TryGet(root, "sha256", out var s) && s.ValueKind == JsonValueKind.String) info.Sha256 = s.GetString() ?? "";
                    if (TryGet(root, "dataset", out var d) && d.ValueKind == JsonValueKind.String) info.Dataset = d.GetString() ?? "";
                    return info;
                }
            }
            catch (JsonException) { return null; }
        }

        private static void WriteInstalled(string dir, InstalledModel info, string hubUrl)
        {
            using (var buffer = new MemoryStream())
            {
                using (var w = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
                {
                    w.WriteStartObject();
                    w.WriteString("dataset", info.Dataset);
                    w.WriteNumber("version", info.Version);
                    w.WriteString("sha256", info.Sha256);
                    w.WriteString("hubUrl", hubUrl);
                    w.WriteString("installedAt", DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));
                    w.WriteEndObject();
                }
                File.WriteAllBytes(Path.Combine(dir, InstalledInfoFile), buffer.ToArray());
            }
        }

        private static string Sha256Of(string path)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path))
                return string.Concat(sha.ComputeHash(stream).Select(b => b.ToString("x2")));
        }

        private static bool HasErrors(JsonElement root)
        {
            return TryGet(root, "errors", out var e) && e.ValueKind == JsonValueKind.Array && e.GetArrayLength() > 0;
        }

        private static string FirstError(JsonElement root)
        {
            if (TryGet(root, "errors", out var e) && e.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in e.EnumerateArray())
                    if (item.ValueKind == JsonValueKind.String) return item.GetString() ?? "";
            }
            return "";
        }

        /// <summary>Property lookup that does not care about the casing the Hub serialises with.</summary>
        private static bool TryGet(JsonElement element, string name, out JsonElement value)
        {
            value = default;
            if (element.ValueKind != JsonValueKind.Object) return false;
            foreach (var p in element.EnumerateObject())
            {
                if (string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    value = p.Value;
                    return true;
                }
            }
            return false;
        }

        private static void SetStatus(string status)
        {
            lock (_lock) _status = status;
        }
    }
}
