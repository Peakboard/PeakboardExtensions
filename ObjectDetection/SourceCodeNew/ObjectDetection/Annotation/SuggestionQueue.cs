using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using OpenCvSharp;
using PeakboardExtensionObjectDetection.Inference;

namespace PeakboardExtensionObjectDetection.Annotation
{
    /// <summary>What the operator decided about one uncertain detection.</summary>
    public enum SuggestionDecision { Open, Accepted, Rejected }

    /// <summary>One uncertain detection on a suggested frame.</summary>
    public sealed class SuggestionCandidate
    {
        public string ClassName { get; set; } = "";
        public int ClassId { get; set; }
        public double Confidence { get; set; }
        public PixelBox Box { get; set; }
        public SuggestionDecision Decision { get; set; }
        /// <summary>The class it is saved with - the model's, unless the operator corrected it.</summary>
        public string AcceptedClass { get; set; } = "";
    }

    /// <summary>A frame on which the model was unsure about at least one object.</summary>
    public sealed class Suggestion
    {
        public string Id { get; set; } = "";
        public byte[] FrameJpeg { get; set; }
        public int FrameWidth { get; set; }
        public int FrameHeight { get; set; }
        public string CapturedAt { get; set; } = "";
        public List<SuggestionCandidate> Candidates { get; set; } = new List<SuggestionCandidate>();
    }

    /// <summary>A consistent copy of the queue, taken under the lock.</summary>
    public sealed class SuggestionSnapshot
    {
        public Suggestion Current { get; set; }
        public int CandidateIndex { get; set; } = -1;
        public byte[] PreviewJpeg { get; set; }
        public int Queued { get; set; }
        public int AcceptedTotal { get; set; }
        public int RejectedTotal { get; set; }
        public string Message { get; set; } = "";
    }

    /// <summary>
    /// Detections the model was unsure about, offered to the operator one by one.
    ///
    /// The engine keeps every detection between MinConfidence and the confidence threshold apart
    /// as "uncertain". A timer looks at the engine twice a second and, at most every
    /// CaptureIntervalSeconds, queues the frame with its uncertain detections. An object that was
    /// already offered - same class, overlapping box - is not offered again for RepeatAfterSeconds,
    /// so a scene that does not change does not flood the operator with the same question.
    ///
    /// The operator accepts (optionally with a corrected class) or rejects each detection. When a
    /// frame is decided, the accepted detections are saved with the frame and uploaded to the Hub
    /// dataset as boxes of origin "AcceptedSuggestion"; a frame with nothing accepted is dropped
    /// and nothing of it is uploaded. Instead of deciding, the operator can hand the frame to the
    /// Annotation list and mark it by hand.
    ///
    /// The queue lives in memory: suggestions not decided before the Runtime stops are gone,
    /// decided ones are in the outbox and survive.
    /// </summary>
    public static class SuggestionQueue
    {
        private const double SameObjectIoU = 0.5;

        private static readonly object _lock = new object();
        private static readonly List<Suggestion> _queue = new List<Suggestion>();
        private static readonly List<(string cls, PixelBox box, DateTime at)> _recent = new List<(string, PixelBox, DateTime)>();

        private static Timer _timer;
        private static int _candidateIndex;
        private static byte[] _preview;
        private static string _message = "Waiting for detections the model is unsure about.";
        private static int _acceptedTotal;
        private static int _rejectedTotal;
        private static string _lastTimestamp = "";
        private static DateTime _lastCapture = DateTime.MinValue;

        private static TimeSpan _captureInterval = TimeSpan.FromSeconds(10);
        private static TimeSpan _repeatAfter = TimeSpan.FromMinutes(5);
        private static int _maxQueued = 20;

        // Colours are BGR, as in the Annotation list: the detection in question orange and thick,
        // accepted ones green, the rest of the frame's open ones grey.
        private static readonly Scalar CurrentColor = new Scalar(0, 140, 255);
        private static readonly Scalar AcceptedColor = new Scalar(80, 200, 0);
        private static readonly Scalar OpenColor = new Scalar(160, 160, 160);

        /// <summary>Starts collecting. Safe to call again with new settings.</summary>
        public static void Start(double captureIntervalSeconds, double repeatAfterSeconds, int maxQueued)
        {
            lock (_lock)
            {
                _captureInterval = TimeSpan.FromSeconds(Math.Max(1, captureIntervalSeconds));
                _repeatAfter = TimeSpan.FromSeconds(Math.Max(0, repeatAfterSeconds));
                _maxQueued = Math.Max(1, maxQueued);
                if (_timer == null)
                    _timer = new Timer(_ => Collect(false), null, 500, 500);
            }
        }

        public static void Stop()
        {
            lock (_lock)
            {
                _timer?.Dispose();
                _timer = null;
            }
        }

        /// <summary>Queues the current frame's uncertain detections now, ignoring interval and repeats.</summary>
        public static bool CaptureNow()
        {
            if (!DetectionEngine.HasStarted)
                return Fail("The camera is not running. Add the Camera or Detections list to the board - it runs in the Peakboard Runtime only.");
            if (Collect(true)) return true;
            lock (_lock)
            {
                if (_queue.Count >= _maxQueued)
                    return FailLocked($"{_queue.Count} suggestions are waiting already. Decide some of them first.");
                return FailLocked("The model is not unsure about anything in the current frame.");
            }
        }

        /// <summary>Looks at the engine's latest frame. True when a suggestion was queued.</summary>
        private static bool Collect(bool force)
        {
            try
            {
                if (!DetectionEngine.HasStarted) return false;
                var latest = DetectionEngine.GetLatest();
                if (latest?.RawJpeg == null || latest.Uncertain == null || latest.Uncertain.Count == 0) return false;
                if (latest.FrameWidth <= 0 || latest.FrameHeight <= 0) return false;

                lock (_lock)
                {
                    var now = DateTime.UtcNow;
                    if (!force && (latest.Timestamp == _lastTimestamp || now - _lastCapture < _captureInterval)) return false;
                    if (_queue.Count >= _maxQueued) return false;

                    _recent.RemoveAll(r => now - r.at > _repeatAfter);
                    var candidates = latest.Uncertain
                        .Select(d => new SuggestionCandidate
                        {
                            ClassName = d.ClassName ?? "",
                            ClassId = d.ClassId,
                            Confidence = Math.Round(d.Confidence, 4),
                            Box = new PixelBox(Math.Round(d.X, 1), Math.Round(d.Y, 1), Math.Round(d.Width, 1), Math.Round(d.Height, 1)),
                            AcceptedClass = d.ClassName ?? "",
                        })
                        .Where(c => force || !_recent.Any(r => r.cls == c.ClassName && IoU(r.box, c.Box) >= SameObjectIoU))
                        .OrderByDescending(c => c.Confidence)
                        .ToList();
                    if (candidates.Count == 0) return false;

                    foreach (var c in candidates) _recent.Add((c.ClassName, c.Box, now));
                    _lastTimestamp = latest.Timestamp ?? "";
                    _lastCapture = now;

                    _queue.Add(new Suggestion
                    {
                        Id = now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture) + "-" + Guid.NewGuid().ToString("N").Substring(0, 6),
                        FrameJpeg = latest.RawJpeg,
                        FrameWidth = latest.FrameWidth,
                        FrameHeight = latest.FrameHeight,
                        CapturedAt = now.ToString("o", CultureInfo.InvariantCulture),
                        Candidates = candidates,
                    });

                    if (_queue.Count == 1)
                    {
                        _candidateIndex = 0;
                        _message = Question();
                        RenderPreview();
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceWarning($"[ObjectDetection] Collecting suggestions failed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Accepts the detection in question, as <paramref name="className"/> when given (the
        /// model named the wrong class) or else as the model's class. saveFrame is called with the
        /// decided frame when this was its last open detection.
        /// </summary>
        public static bool Accept(string className, Func<Suggestion, string> saveFrame)
        {
            lock (_lock)
            {
                var candidate = CurrentCandidate();
                if (candidate == null) return FailLocked("There is no suggestion to accept.");
                className = (className ?? "").Trim();
                candidate.Decision = SuggestionDecision.Accepted;
                candidate.AcceptedClass = className.Length > 0 ? className : candidate.ClassName;
                _acceptedTotal++;
                return AdvanceLocked($"\"{candidate.AcceptedClass}\" accepted.", true, saveFrame);
            }
        }

        /// <summary>Rejects the detection in question. A rejected detection is never uploaded.</summary>
        public static bool Reject(Func<Suggestion, string> saveFrame)
        {
            lock (_lock)
            {
                var candidate = CurrentCandidate();
                if (candidate == null) return FailLocked("There is no suggestion to reject.");
                candidate.Decision = SuggestionDecision.Rejected;
                _rejectedTotal++;
                return AdvanceLocked($"\"{candidate.ClassName}\" rejected.", false, saveFrame);
            }
        }

        /// <summary>
        /// Hands the frame to the Annotation list to be marked by hand and drops it here, with
        /// whatever was decided on it. Nothing of it is uploaded from here.
        /// </summary>
        public static bool AnnotateByHand()
        {
            Suggestion current;
            lock (_lock)
            {
                if (_queue.Count == 0) return FailLocked("There is no suggestion to annotate.");
                current = _queue[0];
            }

            if (!AnnotationSession.FreezeFrame(current.FrameJpeg, current.FrameWidth, current.FrameHeight,
                    "Frame from the Suggestions list frozen. Draw around each object."))
                return Fail("Could not hand the frame over: " + AnnotationSession.Message);

            lock (_lock)
            {
                if (_queue.Count > 0 && ReferenceEquals(_queue[0], current)) _queue.RemoveAt(0);
                NextFrameLocked("Frame handed to the Annotation list.");
                return true;
            }
        }

        /// <summary>Drops every waiting suggestion. Nothing is uploaded.</summary>
        public static int Clear()
        {
            lock (_lock)
            {
                var n = _queue.Count;
                _queue.Clear();
                _candidateIndex = 0;
                _preview = null;
                _message = n == 0 ? "No suggestions were waiting." : $"{n} suggestion(s) dropped.";
                return n;
            }
        }

        public static void SetMessage(string message)
        {
            lock (_lock) _message = message;
        }

        public static SuggestionSnapshot Snapshot()
        {
            lock (_lock)
            {
                return new SuggestionSnapshot
                {
                    Current = _queue.Count > 0 ? _queue[0] : null,
                    CandidateIndex = _queue.Count > 0 ? _candidateIndex : -1,
                    PreviewJpeg = _preview,
                    Queued = _queue.Count,
                    AcceptedTotal = _acceptedTotal,
                    RejectedTotal = _rejectedTotal,
                    Message = _message,
                };
            }
        }

        private static SuggestionCandidate CurrentCandidate()
        {
            if (_queue.Count == 0) return null;
            var s = _queue[0];
            return _candidateIndex >= 0 && _candidateIndex < s.Candidates.Count ? s.Candidates[_candidateIndex] : null;
        }

        /// <summary>To the next open detection of the frame, or - the frame decided - save it and go on.</summary>
        private static bool AdvanceLocked(string done, bool wasAccept, Func<Suggestion, string> saveFrame)
        {
            var s = _queue[0];
            var next = s.Candidates.FindIndex(c => c.Decision == SuggestionDecision.Open);
            if (next >= 0)
            {
                _candidateIndex = next;
                _message = done + " " + Question();
                RenderPreview();
                return true;
            }

            var accepted = s.Candidates.Count(c => c.Decision == SuggestionDecision.Accepted);
            if (accepted == 0)
            {
                _queue.RemoveAt(0);
                NextFrameLocked(done + " Nothing accepted on this frame - nothing uploaded.");
                return true;
            }

            string saved;
            try { saved = saveFrame(s); }
            catch (Exception ex) { saved = "Could not save on this device: " + ex.Message; }

            if (saved != null)
            {
                // The frame stays, with its decisions, so nothing the operator accepted is lost.
                // The last detection goes back to open: the next Accept or Reject saves again.
                s.Candidates[_candidateIndex].Decision = SuggestionDecision.Open;
                if (wasAccept) _acceptedTotal--;
                else _rejectedTotal--;
                _message = saved;
                RenderPreview();
                return false;
            }

            _queue.RemoveAt(0);
            NextFrameLocked($"{done} Frame saved with {accepted} accepted detection(s) and queued for upload.");
            return true;
        }

        private static void NextFrameLocked(string done)
        {
            _candidateIndex = 0;
            if (_queue.Count == 0)
            {
                _preview = null;
                _message = done + " No more suggestions.";
                return;
            }
            _message = done + " " + Question();
            RenderPreview();
        }

        private static string Question()
        {
            var s = _queue[0];
            var c = s.Candidates[_candidateIndex];
            return string.Format(CultureInfo.InvariantCulture,
                "Is this \"{0}\"? The model is {1:0}% sure. ({2} of {3} on this frame)",
                c.ClassName, c.Confidence * 100, _candidateIndex + 1, s.Candidates.Count);
        }

        private static void RenderPreview()
        {
            if (_queue.Count == 0) { _preview = null; return; }
            var s = _queue[0];
            try
            {
                using (var image = Cv2.ImDecode(s.FrameJpeg, ImreadModes.Color))
                {
                    for (var i = 0; i < s.Candidates.Count; i++)
                    {
                        var c = s.Candidates[i];
                        if (i == _candidateIndex || c.Decision == SuggestionDecision.Rejected) continue;
                        var accepted = c.Decision == SuggestionDecision.Accepted;
                        AnnotationSession.DrawBox(image, new AnnotatedObject { Box = c.Box },
                            accepted ? AcceptedColor : OpenColor, 2, accepted ? c.AcceptedClass : c.ClassName);
                    }
                    if (_candidateIndex >= 0 && _candidateIndex < s.Candidates.Count)
                    {
                        var c = s.Candidates[_candidateIndex];
                        AnnotationSession.DrawBox(image, new AnnotatedObject { Box = c.Box }, CurrentColor, 4,
                            string.Format(CultureInfo.InvariantCulture, "{0} {1:0}% ?", c.ClassName, c.Confidence * 100));
                    }
                    Cv2.ImEncode(".jpg", image, out var jpeg, new[] { new ImageEncodingParam(ImwriteFlags.JpegQuality, 85) });
                    _preview = jpeg;
                }
            }
            catch (Exception ex)
            {
                _preview = s.FrameJpeg;
                System.Diagnostics.Trace.TraceWarning($"[ObjectDetection] Suggestion preview failed: {ex.Message}");
            }
        }

        private static double IoU(PixelBox a, PixelBox b)
        {
            var x1 = Math.Max(a.X, b.X);
            var y1 = Math.Max(a.Y, b.Y);
            var x2 = Math.Min(a.X + a.Width, b.X + b.Width);
            var y2 = Math.Min(a.Y + a.Height, b.Y + b.Height);
            var inter = Math.Max(0, x2 - x1) * Math.Max(0, y2 - y1);
            var union = a.Width * a.Height + b.Width * b.Height - inter;
            return union <= 0 ? 0 : inter / union;
        }

        private static bool Fail(string message)
        {
            lock (_lock) return FailLocked(message);
        }

        private static bool FailLocked(string message)
        {
            _message = message;
            return false;
        }
    }
}
