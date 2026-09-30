using System;
using System.Collections.Generic;
using System.Globalization;
using OpenCvSharp;
using PeakboardExtensionObjectDetection.Inference;

namespace PeakboardExtensionObjectDetection.Annotation
{
    /// <summary>One object the operator marked and confirmed.</summary>
    public sealed class AnnotatedObject
    {
        public string ClassName { get; set; } = "";
        public PixelBox Box { get; set; }
        /// <summary>The operator's outline, as getstrokes() returned it. Saved separately from the box.</summary>
        public string DrawingJson { get; set; } = "";
        public string CreatedAt { get; set; } = "";
        /// <summary>
        /// Who made the box, in the Hub's words: "Box" for a drawing, "AcceptedSuggestion" for a
        /// detection the operator accepted from the Suggestions list.
        /// </summary>
        public string Origin { get; set; } = OriginBox;
        /// <summary>The model's confidence for an accepted suggestion; null for a drawing.</summary>
        public double? Confidence { get; set; }

        public const string OriginBox = "Box";
        public const string OriginAcceptedSuggestion = "AcceptedSuggestion";
    }

    /// <summary>A consistent copy of the session, taken under the lock.</summary>
    public sealed class AnnotationSnapshot
    {
        public bool IsFrozen { get; set; }
        public byte[] FrameJpeg { get; set; }
        public byte[] PreviewJpeg { get; set; }
        public int FrameWidth { get; set; }
        public int FrameHeight { get; set; }
        public string CapturedAt { get; set; } = "";
        public List<AnnotatedObject> Objects { get; set; } = new List<AnnotatedObject>();
        public AnnotatedObject Pending { get; set; }
        public string Message { get; set; } = "";
        public string LastSampleId { get; set; } = "";
        /// <summary>Nothing happened since the last save - a second tap on Save lands here.</summary>
        public bool JustSaved { get; set; }
    }

    /// <summary>
    /// The frame an operator is annotating on this device: freeze, propose a box from a
    /// drawing, confirm or discard it, save. One session per device, like the one camera
    /// engine it takes its frame from - two lists on one board see the same session.
    /// </summary>
    public static class AnnotationSession
    {
        private static readonly object _lock = new object();

        private static byte[] _frameJpeg;
        private static byte[] _previewJpeg;
        private static int _frameWidth;
        private static int _frameHeight;
        private static string _capturedAt = "";
        private static readonly List<AnnotatedObject> _objects = new List<AnnotatedObject>();
        private static AnnotatedObject _pending;
        private static string _message = "Freeze a frame to start.";
        private static string _lastSampleId = "";
        private static bool _justSaved;

        // Colours are BGR. Confirmed boxes green, the one waiting for confirmation orange
        // and thicker, so the two can never be mistaken for each other.
        private static readonly Scalar ConfirmedColor = new Scalar(80, 200, 0);
        private static readonly Scalar PendingColor = new Scalar(0, 140, 255);

        public static bool Freeze()
        {
            if (!DetectionEngine.HasStarted)
                return Fail("The camera is not running. Add the Camera list to the board - it runs in the Peakboard Runtime only.");

            var latest = DetectionEngine.GetLatest();
            if (latest?.RawJpeg == null || latest.FrameWidth <= 0 || latest.FrameHeight <= 0)
                return Fail("No camera frame yet. " + (latest?.Error ?? ""));

            return FreezeFrame(latest.RawJpeg, latest.FrameWidth, latest.FrameHeight,
                "Frame frozen. Draw around an object.");
        }

        /// <summary>
        /// Freezes a given frame instead of the live one - the Suggestions list hands over the
        /// frame of a suggestion the operator wants to annotate by hand.
        /// </summary>
        public static bool FreezeFrame(byte[] frameJpeg, int frameWidth, int frameHeight, string message)
        {
            if (frameJpeg == null || frameWidth <= 0 || frameHeight <= 0)
                return Fail("There is no frame to freeze.");

            lock (_lock)
            {
                // One tap on Freeze must not throw away the objects an operator just marked.
                var unsaved = _objects.Count + (_pending != null ? 1 : 0);
                if (_frameJpeg != null && unsaved > 0)
                    return FailLocked($"This frame has {unsaved} unsaved object(s). Save it, or Cancel to drop it, before freezing a new frame.");

                _frameJpeg = frameJpeg;
                _frameWidth = frameWidth;
                _frameHeight = frameHeight;
                _capturedAt = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
                _objects.Clear();
                _pending = null;
                _justSaved = false;
                _message = message;
                RenderPreview();
                return true;
            }
        }

        /// <summary>The message the Annotation list shows - for another list that could not freeze.</summary>
        public static string Message
        {
            get { lock (_lock) return _message; }
        }

        public static bool ProposeBox(string drawingJson, string className, string imageStretch)
        {
            lock (_lock)
            {
                if (_frameJpeg == null) return FailLocked("Freeze a frame first.");

                className = (className ?? "").Trim();
                if (className.Length == 0) return FailLocked("Choose a class for the object.");

                var outline = DrawingOutline.Parse(drawingJson, out var error);
                if (outline == null) return FailLocked(error);

                var box = outline.ToFrame(_frameWidth, _frameHeight, imageStretch);
                if (box.Width < 4 || box.Height < 4)
                    return FailLocked("The drawing is too small. Draw all the way around the object.");

                _pending = new AnnotatedObject
                {
                    ClassName = className,
                    Box = box,
                    DrawingJson = outline.RawJson,
                    CreatedAt = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture),
                };
                _message = string.Format(CultureInfo.InvariantCulture,
                    "Add \"{0}\" at X {1}, Y {2}, {3} x {4} px?", className, box.X, box.Y, box.Width, box.Height);
                RenderPreview();
                return true;
            }
        }

        public static bool ConfirmBox()
        {
            lock (_lock)
            {
                if (_pending == null) return FailLocked("There is no box waiting for confirmation.");
                _objects.Add(_pending);
                _message = $"\"{_pending.ClassName}\" added. {_objects.Count} object(s) on this frame.";
                _pending = null;
                RenderPreview();
                return true;
            }
        }

        public static bool DiscardBox()
        {
            lock (_lock)
            {
                if (_pending == null) return FailLocked("There is no box waiting for confirmation.");
                _pending = null;
                _message = "Box discarded. Draw around the object again.";
                RenderPreview();
                return true;
            }
        }

        public static bool RemoveLastBox()
        {
            lock (_lock)
            {
                if (_objects.Count == 0) return FailLocked("There is no object to remove.");
                var removed = _objects[_objects.Count - 1];
                _objects.RemoveAt(_objects.Count - 1);
                _message = $"\"{removed.ClassName}\" removed. {_objects.Count} object(s) on this frame.";
                RenderPreview();
                return true;
            }
        }

        /// <summary>Drops the frozen frame and everything marked on it.</summary>
        public static void Cancel(string message)
        {
            lock (_lock)
            {
                _frameJpeg = null;
                _previewJpeg = null;
                _frameWidth = _frameHeight = 0;
                _objects.Clear();
                _pending = null;
                _justSaved = false;
                _message = message;
            }
        }

        /// <summary>Called after a sample was written: the frame is done, the next one can be frozen.</summary>
        public static void Saved(string sampleId, int objectCount)
        {
            Cancel($"Saved with {objectCount} object(s). Freeze the next frame.");
            lock (_lock) { _lastSampleId = sampleId; _justSaved = true; }
        }

        public static void SetMessage(string message)
        {
            lock (_lock) { _message = message; }
        }

        public static AnnotationSnapshot Snapshot()
        {
            lock (_lock)
            {
                return new AnnotationSnapshot
                {
                    IsFrozen = _frameJpeg != null,
                    FrameJpeg = _frameJpeg,
                    PreviewJpeg = _previewJpeg,
                    FrameWidth = _frameWidth,
                    FrameHeight = _frameHeight,
                    CapturedAt = _capturedAt,
                    Objects = new List<AnnotatedObject>(_objects),
                    Pending = _pending,
                    Message = _message,
                    LastSampleId = _lastSampleId,
                    JustSaved = _justSaved,
                };
            }
        }

        private static bool Fail(string message)
        {
            lock (_lock) { return FailLocked(message); }
        }

        private static bool FailLocked(string message)
        {
            _message = message;
            return false;
        }

        /// <summary>The frozen frame with every confirmed box and the pending one drawn on it.</summary>
        private static void RenderPreview()
        {
            try
            {
                using (var image = Cv2.ImDecode(_frameJpeg, ImreadModes.Color))
                {
                    foreach (var o in _objects)
                        DrawBox(image, o, ConfirmedColor, 2, o.ClassName);
                    if (_pending != null)
                        DrawBox(image, _pending, PendingColor, 4, _pending.ClassName + " ?");

                    Cv2.ImEncode(".jpg", image, out var jpeg, new[] { new ImageEncodingParam(ImwriteFlags.JpegQuality, 85) });
                    _previewJpeg = jpeg;
                }
            }
            catch (Exception ex)
            {
                // The preview is a courtesy; the frame and the boxes are what gets saved.
                _previewJpeg = _frameJpeg;
                System.Diagnostics.Trace.TraceWarning($"[ObjectDetection] Annotation preview failed: {ex.Message}");
            }
        }

        /// <summary>
        /// The label as the Hershey font can draw it: German umlauts spelled out, other accents
        /// dropped. Only the picture is affected - the class name is saved exactly as entered.
        /// </summary>
        internal static string AsciiLabel(string label)
        {
            var spelled = label.Replace("ä", "ae").Replace("ö", "oe").Replace("ü", "ue")
                .Replace("Ä", "Ae").Replace("Ö", "Oe").Replace("Ü", "Ue").Replace("ß", "ss");
            var result = new System.Text.StringBuilder(spelled.Length);
            foreach (var c in spelled.Normalize(System.Text.NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
                result.Append(c < 128 ? c : '?');
            }
            return result.ToString();
        }

        internal static void DrawBox(Mat image, AnnotatedObject o, Scalar color, int thickness, string label)
        {
            var rect = new Rect((int)o.Box.X, (int)o.Box.Y, (int)o.Box.Width, (int)o.Box.Height);
            Cv2.Rectangle(image, rect, color, thickness);

            label = AsciiLabel(label);
            var size = Cv2.GetTextSize(label, HersheyFonts.HersheySimplex, 0.6, 2, out var baseline);
            var top = Math.Max(0, rect.Y - size.Height - baseline - 4);
            Cv2.Rectangle(image, new Rect(rect.X, top, size.Width + 8, size.Height + baseline + 4), color, -1);
            Cv2.PutText(image, label, new Point(rect.X + 4, top + size.Height + 2),
                HersheyFonts.HersheySimplex, 0.6, new Scalar(0, 0, 0), 2);
        }
    }
}
