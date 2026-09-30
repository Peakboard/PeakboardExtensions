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

        // Sticky note that the last hot-reload failed and the previous model is
        // still the one running. Cleared by the next reload that succeeds.
        private static string _reloadWarning = "";

        // What actually got loaded, as opposed to what was asked for.
        private static string _loadedModelPath = "";
        // The model's logical name, not its filename. Every custom model is stored
        // as model.onnx, so a filename would report "model" for all of them and
        // the LoadedModel column would identify nothing.
        private static string _loadedModelName = "";
        private static int _loadedClassCount;
        // The Hub model version of what is running (hub.json beside the model);
        // 0 when the model did not come from the Hub.
        private static int _loadedModelVersion;

        // Detections below the confidence threshold but at or above this one are
        // kept apart as "uncertain" for the Suggestions list. 0 = nobody asked.
        private static float _suggestionThreshold;

        // Set by the Hub model sync after it installed a model, so the engine
        // does not wait for its next periodic check to load it.
        private static volatile bool _modelCheckRequested;

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
        public static int LoadedModelVersion => _loadedModelVersion;
        public static float ConfidenceThreshold => _confThreshold;

        /// <summary>Look at the model files on the next loop cycle instead of in up to a minute.</summary>
        public static void RequestModelCheck() => _modelCheckRequested = true;

        /// <summary>
        /// Keep detections between <paramref name="threshold"/> and the confidence threshold as
        /// uncertain ones (<see cref="DetectionResult.Uncertain"/>). 0 switches it off. The
        /// Detections list, the Camera overlay and every count still see only the confident ones.
        /// </summary>
        public static void SetSuggestionThreshold(float threshold)
        {
            lock (_startLock)
            {
                _suggestionThreshold = threshold > 0 ? threshold : 0;
                var inf = _inference;
                if (inf != null) inf.ConfidenceThreshold = InferenceThreshold();
            }
        }

        /// <summary>What the model is asked for: the lower of the two thresholds.</summary>
        private static float InferenceThreshold()
        {
            return _suggestionThreshold > 0 && _suggestionThreshold < _confThreshold
                ? _suggestionThreshold : _confThreshold;
        }

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
        private static List<Detection> _uncertain = new List<Detection>();
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
                inf.ConfidenceThreshold = InferenceThreshold();
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
                    Uncertain = _uncertain,
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
            _reloadWarning = "";

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
                        ConfidenceThreshold = InferenceThreshold(),
                        NmsThreshold = _nmsThreshold
                    };
                    inference.LoadModel(onnxPath, classesPath);
                    _inference = inference;
                    _loadedModelPath = onnxPath;
                    _loadedClassCount = inference.ClassNames.Length;
                    _loadedModelVersion = HubModelSync.ReadInstalledVersion(onnxPath);
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

            // Track the model and its class file so an updated custom model is
            // picked up without restarting the board. Both, because a new model
            // usually arrives with a new class list, and not always in one go.
            var lastModelWrite = ModelStamp(onnxPath, classesPath);
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

                            var all = inference != null
                                ? inference.Detect(frame)
                                : new List<Detection>();
                            // The model ran at the lower of the two thresholds; what
                            // falls short of the confidence threshold is only a
                            // suggestion and is never counted as a detection.
                            var conf = _confThreshold;
                            var detections = all.Where(d => d.Confidence >= conf).ToList();
                            var uncertain = _suggestionThreshold > 0
                                ? all.Where(d => d.Confidence < conf && d.Confidence >= _suggestionThreshold).ToList()
                                : new List<Detection>();

                            var annotated = DrawDetections(frame, detections);
                            var encParams = new ImageEncodingParam(ImwriteFlags.JpegQuality, 75);
                            Cv2.ImEncode(".jpg", annotated, out byte[] jpeg, new[] { encParams });
                            Cv2.ImEncode(".jpg", frame, out byte[] rawJpeg, new[] { encParams });
                            if (annotated != frame) annotated.Dispose();

                            lock (_cacheLock)
                            {
                                _detections = detections;
                                _uncertain = uncertain;
                                _annotatedJpeg = jpeg;
                                _rawJpeg = rawJpeg;
                                _timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                                _frameWidth = frame.Width;
                                _frameHeight = frame.Height;
                                _status = _reloadWarning.Length > 0 ? "ok_reload_failed"
                                        : _modelWarning.Length > 0 ? "ok_model_fallback"
                                        : "ok";
                                _error = string.Join(" ", new[] { _reloadWarning, _modelWarning }
                                    .Where(w => w.Length > 0));
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
                var requested = _modelCheckRequested;
                if (cyclesSinceModelCheck >= ModelCheckInterval || requested)
                {
                    cyclesSinceModelCheck = 0;
                    _modelCheckRequested = false;

                    // Running on the bundled fallback because the requested model did
                    // not exist at start. It may have arrived since -- the Hub model
                    // sync installs the first model while the board is already up.
                    if (_modelWarning.Length > 0 &&
                        TrySwitchFromFallback(inference, ref onnxPath, ref classesPath))
                    {
                        lastModelWrite = ModelStamp(onnxPath, classesPath);
                    }

                    // The model or its class file changed on disk -- reload it.
                    var currentWrite = ModelStamp(onnxPath, classesPath);
                    if (currentWrite != lastModelWrite && File.Exists(onnxPath))
                    {
                        try
                        {
                            SetStatus("reloading", "Reloading model...");
                            // A file copied in by hand may still be growing. The Hub
                            // sync writes complete files and asks for the check.
                            if (!requested) Thread.Sleep(2000);
                            currentWrite = ModelStamp(onnxPath, classesPath);
                            inference.LoadModel(onnxPath, classesPath);
                            _loadedClassCount = inference.ClassNames.Length;
                            _loadedModelVersion = HubModelSync.ReadInstalledVersion(onnxPath);
                            _reloadWarning = "";
                            Info($"Reloaded model '{Path.GetFileName(onnxPath)}' " +
                                 $"({inference.ClassNames.Length} classes, input {inference.InputSize}).");
                            SetStatus("ok", "");
                        }
                        catch (Exception ex)
                        {
                            // LoadModel only replaces the running model once the new
                            // one has loaded and passed its checks, so the previous
                            // model is still serving. Say so on every poll -- a
                            // silently stale model is exactly the failure this
                            // extension is prone to.
                            _reloadWarning = $"Model reload failed, still running the previous " +
                                             $"model: {ex.Message}";
                            Fail(_reloadWarning);
                            SetStatus("ok_reload_failed", _reloadWarning);
                        }

                        // Not retried until one of the files changes again, so a
                        // broken upload is reported once instead of every minute.
                        lastModelWrite = currentWrite;
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
            _loadedModelVersion = 0;

            // The loop only reaches here on a clean stop. Failures return early
            // and leave their own status in place.
            SetStatus("stopped", "");
            Info("Detection engine stopped.");
        }

        /// <summary>
        /// Resolve ModelName again and, when it now names a real model, load that one in place
        /// of the fallback. The fallback keeps running if the new model does not load.
        /// </summary>
        private static bool TrySwitchFromFallback(InferenceService inference,
            ref string onnxPath, ref string classesPath)
        {
            var fallbackName = _loadedModelName;
            var (newOnnx, newClasses, stillFallback) = ResolveModel();
            if (stillFallback || !File.Exists(newOnnx))
            {
                _loadedModelName = fallbackName;
                return false;
            }

            try
            {
                SetStatus("reloading", "Loading model...");
                inference.LoadModel(newOnnx, newClasses);
                onnxPath = newOnnx;
                classesPath = newClasses;
                _loadedModelPath = newOnnx;
                _loadedClassCount = inference.ClassNames.Length;
                _loadedModelVersion = HubModelSync.ReadInstalledVersion(newOnnx);
                _modelWarning = "";
                _reloadWarning = "";
                Info($"Model '{_modelName}' is available now and replaces the fallback " +
                     $"({inference.ClassNames.Length} classes).");
                return true;
            }
            catch (Exception ex)
            {
                _loadedModelName = fallbackName;
                _reloadWarning = $"Model '{_modelName}' could not be loaded, still running " +
                                 $"'{fallbackName}': {ex.Message}";
                Fail(_reloadWarning);
                SetStatus("ok_reload_failed", _reloadWarning);
                return false;
            }
        }

        private static (DateTime onnx, DateTime classes) ModelStamp(string onnxPath, string classesPath)
        {
            return (File.Exists(onnxPath) ? File.GetLastWriteTimeUtc(onnxPath) : DateTime.MinValue,
                    File.Exists(classesPath) ? File.GetLastWriteTimeUtc(classesPath) : DateTime.MinValue);
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
        /// <summary>Below the confidence threshold, above the suggestion threshold. Empty unless asked for.</summary>
        public List<Detection> Uncertain { get; set; }
        public byte[] AnnotatedJpeg { get; set; }
        public byte[] RawJpeg { get; set; }
        public string Timestamp { get; set; }
        public int FrameWidth { get; set; }
        public int FrameHeight { get; set; }
        public string Status { get; set; }
        public string Error { get; set; }
    }
}
