using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using OpenCvSharp;
using Peakboard.ExtensionKit;
using PeakboardExtensionObjectDetection.Camera;
using PeakboardExtensionObjectDetection.Data;

namespace PeakboardExtensionObjectDetection.Inference
{
    /// <summary>
    /// Shared singleton detection engine. Runs inference on a background thread,
    /// caches results + annotated frame for all data sources to consume.
    /// </summary>
    public static class DetectionEngine
    {
        private static Thread _thread;
        private static volatile bool _running;
        private static readonly object _startLock = new object();
        private static readonly object _cacheLock = new object();

        // How many data sources are currently using the engine. It is shared, so
        // it must outlive any single list's cleanup.
        private static int _subscribers;
        private static volatile bool _hasStarted;
        private static ILoggingService _log;

        // The live inference session, hoisted out of RunLoop so Start can retune
        // a running engine instead of dropping the new thresholds.
        private static InferenceService _inference;

        // Sticky note about the loaded model -- e.g. that it is not the one asked
        // for. Re-published on every poll so it cannot scroll away.
        private static string _modelWarning = "";

        // What actually got loaded, as opposed to what was asked for.
        private static string _loadedModelPath = "";
        // The model's logical name, not its filename. Every custom model is stored
        // as model.onnx, so a filename would report "model" for all of them and
        // the LoadedModel column would identify nothing.
        private static string _loadedModelName = "";
        private static int _loadedClassCount;

        public static bool IsRunning => _running && _thread != null && _thread.IsAlive;

        /// <summary>
        /// True once Start has been called in this process. This is what tells
        /// "Designer preview, the engine never ran" apart from "the engine ran
        /// and then failed" -- without it, a camera that will not open is
        /// reported to the user as a design-time preview.
        /// </summary>
        public static bool HasStarted => _hasStarted;

        public static string RequestedModel => _modelName ?? "";
        public static string CameraSourceName => _cameraSource ?? "";
        public static string LoadedModelPath => _loadedModelPath ?? "";
        public static string LoadedModelName => _loadedModelName ?? "";
        public static int LoadedClassCount => _loadedClassCount;

        /// <summary>Give the engine somewhere to log. Any list may supply it.</summary>
        public static void SetLogger(ILoggingService log)
        {
            if (log != null) _log = log;
        }

        private static void Info(string m) { try { _log?.Info("[ObjectDetection] " + m); } catch { } }
        private static void Warn(string m) { try { _log?.Warning("[ObjectDetection] " + m); } catch { } }
        private static void Fail(string m) { try { _log?.Error("[ObjectDetection] " + m); } catch { } }

        // Configuration
        private static string _cameraSource;
        private static string _modelName;
        private static float _confThreshold;
        private static float _nmsThreshold;
        // Cached results
        private static List<Detection> _detections = new List<Detection>();
        private static byte[] _annotatedJpeg;
        private static byte[] _rawJpeg;
        private static string _timestamp = "";
        private static int _frameWidth;
        private static int _frameHeight;
        private static string _status = "idle";
        private static string _error = "";

        public static void Start(string cameraSource, string modelName,
            float confThreshold = 0.25f, float nmsThreshold = 0.45f)
        {
            lock (_startLock)
            {
                _subscribers++;
                _hasStarted = true;

                if (_running && _thread != null && _thread.IsAlive
                    && _cameraSource == cameraSource && _modelName == modelName)
                {
                    // Same camera and model, so keep the running engine -- but do
                    // not silently drop this subscriber's thresholds, which is
                    // what the previous bare `return` did.
                    ApplyThresholds(confThreshold, nmsThreshold);
                    return;
                }

                if (_running && _thread != null && _thread.IsAlive)
                {
                    Warn($"Data sources disagree: the engine is running camera '{_cameraSource}' " +
                         $"with model '{_modelName}', and another list asked for camera " +
                         $"'{cameraSource}' with model '{modelName}'. Restarting on the new " +
                         $"settings. Set both lists to the same values to avoid this.");
                }

                StopInternal();
                _modelWarning = "";

                _cameraSource = cameraSource;
                _modelName = modelName;
                _confThreshold = confThreshold;
                _nmsThreshold = nmsThreshold;
                _running = true;

                _thread = new Thread(RunLoop)
                {
                    IsBackground = true,
                    Name = "DetectionEngine"
                };
                _thread.Start();
            }
        }

        /// <summary>
        /// One data source is going away. The engine is shared, so it stops only
        /// when the last subscriber lets go -- previously the Detections list's
        /// cleanup stopped the feed the Camera list was still reading from.
        /// </summary>
        public static void Release()
        {
            lock (_startLock)
            {
                if (_subscribers > 0) _subscribers--;
                if (_subscribers == 0)
                {
                    Info("Last data source released the engine; stopping.");
                    StopInternal();
                }
            }
        }

        /// <summary>Stop regardless of who is still subscribed.</summary>
        public static void Stop()
        {
            lock (_startLock)
            {
                _subscribers = 0;
                StopInternal();
            }
        }

        private static void StopInternal()
        {
            _running = false;
            var t = _thread;
            if (t != null && t.IsAlive) t.Join(5000);
            _thread = null;
        }

        private static void ApplyThresholds(float conf, float nms)
        {
            if (Math.Abs(_confThreshold - conf) < 0.0001f &&
                Math.Abs(_nmsThreshold - nms) < 0.0001f)
                return;

            _confThreshold = conf;
            _nmsThreshold = nms;

            var inf = _inference;
            if (inf != null)
            {
                inf.ConfidenceThreshold = conf;
                inf.NmsThreshold = nms;
            }
            Info($"Thresholds updated: confidence {conf:0.###}, NMS {nms:0.###}.");
        }

        public static DetectionResult GetLatest()
        {
            lock (_cacheLock)
            {
                return new DetectionResult
                {
                    Detections = _detections,
                    AnnotatedJpeg = _annotatedJpeg,
                    RawJpeg = _rawJpeg,
                    Timestamp = _timestamp,
                    FrameWidth = _frameWidth,
                    FrameHeight = _frameHeight,
                    Status = _status,
                    Error = _error
                };
            }
        }

        /// <summary>
        /// Resolve the model to load.
        /// Returns (onnxPath, classesPath, isFallback), where isFallback means the
        /// requested model was not found and a bundled one was used instead.
        /// </summary>
        private static (string onnxPath, string classesPath, bool isFallback) ResolveModel()
        {
            // Standard mode: try ModelManager first
            var manager = new ModelManager();
            var model = manager.GetModel(_modelName);
            if (model != null)
            {
                _loadedModelName = model.Name;
                return (model.OnnxPath, model.ClassesPath, false);
            }

            // Not found — fall back to a bundled model, and say so. This used to
            // pass `false`, so even a caller that looked at the flag was told the
            // requested model had loaded.
            return GetPretrainedFallback(true);
        }

        private static (string onnxPath, string classesPath, bool isFallback) GetPretrainedFallback(bool isFallback)
        {
            var pretrainedDir = PathHelper.GetPretrainedModelsDir();
            var fallbackOnnx = Directory.Exists(pretrainedDir)
                ? Directory.GetFiles(pretrainedDir, "*.onnx").FirstOrDefault()
                : null;
            _loadedModelName = fallbackOnnx != null
                ? Path.GetFileNameWithoutExtension(fallbackOnnx) : "";
            return (fallbackOnnx ?? "", PathHelper.GetDefaultClassesPath(), isFallback);
        }

        private static void RunLoop()
        {
            InferenceService inference = null;
            string onnxPath = "", classesPath = "";

            try
            {
                SetStatus("loading", "Loading model...");

                bool isFallback;
                (onnxPath, classesPath, isFallback) = ResolveModel();

                if (isFallback && File.Exists(onnxPath))
                {
                    // The requested model was not found. Loading a different one
                    // without saying so is how a board ends up quietly reporting
                    // the wrong classes.
                    _modelWarning = $"Model '{_modelName}' was not found. Using " +
                                    $"'{Path.GetFileNameWithoutExtension(onnxPath)}' instead.";
                    Warn(_modelWarning);
                }

                if (File.Exists(onnxPath))
                {
                    inference = new InferenceService
                    {
                        ConfidenceThreshold = _confThreshold,
                        NmsThreshold = _nmsThreshold
                    };
                    inference.LoadModel(onnxPath, classesPath);
                    _inference = inference;
                    _loadedModelPath = onnxPath;
                    _loadedClassCount = inference.ClassNames.Length;
                    Info($"Loaded model '{Path.GetFileName(onnxPath)}' " +
                         $"({inference.ClassNames.Length} classes, input {inference.InputSize}).");
                }
                else
                {
                    var msg = $"No model available. Looked for '{_modelName}' and found no " +
                              $"usable .onnx in {PathHelper.GetPretrainedModelsDir()}.";
                    Fail(msg);
                    SetStatus("error", msg);
                    return;
                }
            }
            catch (Exception ex)
            {
                Fail($"Model load failed: {ex}");
                SetStatus("error", $"Model error: {ex.Message}");
                return;
            }

            // Track the model file so an updated custom model is picked up without
            // restarting the board.
            DateTime lastModelWrite = File.Exists(onnxPath)
                ? File.GetLastWriteTimeUtc(onnxPath) : DateTime.MinValue;
            int cyclesSinceModelCheck = 0;
            const int ModelCheckInterval = 600; // check every ~60 seconds (600 cycles * 100ms)

            // Open persistent camera connection
            var camera = new CameraService();
            try
            {
                SetStatus("connecting", "Connecting to camera...");
                camera.Open(_cameraSource);
            }
            catch (Exception ex)
            {
                Fail($"Camera '{_cameraSource}' could not be opened: {ex.Message}");
                SetStatus("error", $"Camera error: {ex.Message}");
                try { inference?.Dispose(); } catch { }
                _inference = null;
                return;
            }

            int consecutiveFailures = 0;

            while (_running)
            {
                try
                {
                    using (var frame = camera.GrabFrame())
                    {
                        if (frame != null && !frame.Empty())
                        {
                            consecutiveFailures = 0;

                            var detections = inference != null
                                ? inference.Detect(frame)
                                : new List<Detection>();

                            var annotated = DrawDetections(frame, detections);
                            var encParams = new ImageEncodingParam(ImwriteFlags.JpegQuality, 75);
                            Cv2.ImEncode(".jpg", annotated, out byte[] jpeg, new[] { encParams });
                            Cv2.ImEncode(".jpg", frame, out byte[] rawJpeg, new[] { encParams });
                            if (annotated != frame) annotated.Dispose();

                            lock (_cacheLock)
                            {
                                _detections = detections;
                                _annotatedJpeg = jpeg;
                                _rawJpeg = rawJpeg;
                                _timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                                _frameWidth = frame.Width;
                                _frameHeight = frame.Height;
                                _status = _modelWarning.Length > 0 ? "ok_model_fallback" : "ok";
                                _error = _modelWarning;
                            }
                        }
                        else
                        {
                            consecutiveFailures++;
                            if (consecutiveFailures >= 3)
                            {
                                SetStatus("reconnecting", "Reconnecting to camera...");
                                if (camera.Reconnect())
                                {
                                    consecutiveFailures = 0;
                                }
                                else
                                {
                                    Thread.Sleep(5000);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    consecutiveFailures++;
                    SetStatus("error", ex.Message);

                    if (consecutiveFailures >= 3)
                    {
                        try
                        {
                            if (camera.Reconnect())
                            {
                                consecutiveFailures = 0;
                            }
                            else
                            {
                                Thread.Sleep(5000);
                            }
                        }
                        catch { Thread.Sleep(5000); }
                    }
                }

                // Periodically check for model updates
                cyclesSinceModelCheck++;
                if (cyclesSinceModelCheck >= ModelCheckInterval)
                {
                    cyclesSinceModelCheck = 0;
                    try
                    {
                        {
                            // The model file changed on disk -- reload it.
                            var currentWrite = File.GetLastWriteTimeUtc(onnxPath);
                            if (currentWrite != lastModelWrite && File.Exists(onnxPath))
                            {
                                SetStatus("reloading", "Reloading model...");
                                Thread.Sleep(2000);
                                inference.LoadModel(onnxPath, classesPath);
                                _loadedClassCount = inference.ClassNames.Length;
                                lastModelWrite = File.GetLastWriteTimeUtc(onnxPath);
                                SetStatus("ok", "");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Keep serving the model already loaded, but do not pretend
                        // the reload happened -- a silently stale model is exactly
                        // the failure this extension is prone to.
                        Fail($"Model reload failed, continuing on the previously loaded model: {ex.Message}");
                    }
                }

                Thread.Sleep(100);
            }

            try { camera?.Dispose(); } catch { }
            try { inference?.Dispose(); } catch { }
            _inference = null;
            _loadedModelPath = "";
            _loadedModelName = "";
            _loadedClassCount = 0;

            // The loop only reaches here on a clean stop. Failures return early
            // and leave their own status in place.
            SetStatus("stopped", "");
            Info("Detection engine stopped.");
        }

        private static string SanitizeForHershey(string text)
        {
            // OpenCV Hershey fonts only support ASCII — replace non-ASCII characters
            var sb = new System.Text.StringBuilder(text.Length);
            foreach (char c in text)
            {
                if (c < 128) sb.Append(c);
                else
                {
                    // Common replacements for German umlauts
                    switch (c)
                    {
                        case 'ä': sb.Append("ae"); break;
                        case 'ö': sb.Append("oe"); break;
                        case 'ü': sb.Append("ue"); break;
                        case 'Ä': sb.Append("Ae"); break;
                        case 'Ö': sb.Append("Oe"); break;
                        case 'Ü': sb.Append("Ue"); break;
                        case 'ß': sb.Append("ss"); break;
                        default: sb.Append('?'); break;
                    }
                }
            }
            return sb.ToString();
        }

        private static Mat DrawDetections(Mat frame, List<Detection> detections)
        {
            var output = frame.Clone();

            foreach (var det in detections)
            {
                int x = Math.Max(0, (int)det.X);
                int y = Math.Max(0, (int)det.Y);
                int w = (int)det.Width;
                int h = (int)det.Height;

                var color = GetColor(det.ClassId);

                Cv2.Rectangle(output, new Rect(x, y, w, h), color, 2);

                var label = SanitizeForHershey($"{det.ClassName} {det.Confidence:P0}");
                var textSize = Cv2.GetTextSize(label, HersheyFonts.HersheySimplex, 0.5, 1, out int baseline);
                int labelY = Math.Max(y, textSize.Height + 4);
                Cv2.Rectangle(output,
                    new Rect(x, labelY - textSize.Height - 4, textSize.Width + 4, textSize.Height + 8),
                    color, -1);

                Cv2.PutText(output, label, new Point(x + 2, labelY - 2),
                    HersheyFonts.HersheySimplex, 0.5, new Scalar(255, 255, 255), 1);
            }

            return output;
        }

        private static Scalar GetColor(int classId)
        {
            var colors = new Scalar[]
            {
                new Scalar(0, 255, 0),     // green
                new Scalar(255, 0, 0),     // blue
                new Scalar(0, 0, 255),     // red
                new Scalar(255, 255, 0),   // cyan
                new Scalar(0, 255, 255),   // yellow
                new Scalar(255, 0, 255),   // magenta
                new Scalar(128, 255, 0),   // lime
                new Scalar(255, 128, 0),   // light blue
                new Scalar(0, 128, 255),   // orange
                new Scalar(255, 0, 128),   // purple
            };
            return colors[classId % colors.Length];
        }

        private static void SetStatus(string status, string error)
        {
            lock (_cacheLock)
            {
                _status = status;
                _error = error;
            }
        }
    }

    public class DetectionResult
    {
        public List<Detection> Detections { get; set; }
        public byte[] AnnotatedJpeg { get; set; }
        public byte[] RawJpeg { get; set; }
        public string Timestamp { get; set; }
        public int FrameWidth { get; set; }
        public int FrameHeight { get; set; }
        public string Status { get; set; }
        public string Error { get; set; }
    }
}
