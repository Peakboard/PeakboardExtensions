using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;

namespace PeakboardExtensionObjectDetection.Inference
{
    public sealed class InferenceService : IDisposable
    {
        private InferenceSession _session;
        private string[] _classNames;
        private string _modelPath;
        private int _inputSize = 320;
        private readonly object _lock = new object();

        // Box convention of output rows 0-3. Determined from the first inference
        // because it cannot be read from the ONNX graph. null = not yet probed.
        private bool? _boxesAreXyxy;

        public float ConfidenceThreshold { get; set; } = 0.25f;
        public float NmsThreshold { get; set; } = 0.45f;
        public bool IsLoaded => _session != null;
        public string ModelPath => _modelPath;
        public string[] ClassNames => _classNames;
        public int InputSize => _inputSize;

        public void LoadModel(string onnxPath, string classNamesPath)
        {
            if (!File.Exists(onnxPath))
                throw new FileNotFoundException("ONNX model not found", onnxPath);

            var options = new SessionOptions();
            options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
            options.InterOpNumThreads = 4;
            options.IntraOpNumThreads = 4;

            // Build and check the new model completely before touching the one in
            // use. This used to dispose the running session first, so a reload that
            // failed left a disposed session behind -- or, when only the class check
            // failed, the new model running with the wrong class list.
            var session = new InferenceSession(onnxPath, options);
            try
            {
                // Determine input size from model metadata
                int inputSize = 320;
                var inputMeta = session.InputMetadata.First();
                if (inputMeta.Value.Dimensions.Length == 4)
                {
                    inputSize = inputMeta.Value.Dimensions[2]; // NCHW: [1, 3, H, W]
                    if (inputSize <= 0) inputSize = 320;
                }

                // Load class names
                var classNames = File.Exists(classNamesPath)
                    ? File.ReadAllLines(classNamesPath)
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .ToArray()
                    : new string[0];

                ValidateClassNames(session, inputSize, classNames, onnxPath, classNamesPath);

                lock (_lock)
                {
                    var previous = _session;
                    _session = session;
                    _classNames = classNames;
                    _inputSize = inputSize;
                    _modelPath = onnxPath;
                    _boxesAreXyxy = null;   // re-probe after a hot-reload
                    previous?.Dispose();
                }
            }
            catch
            {
                session.Dispose();
                throw;
            }
        }

        /// <summary>
        /// The model's own output tensor states how many classes it predicts:
        /// shape is [1, 4 + numClasses, numAnchors]. If the class-name file
        /// disagrees, every label the extension reports is wrong -- a 3-class
        /// custom model paired with COCO's 80 names silently reports "person",
        /// "bicycle", "car". Fail here instead, while there is something useful
        /// to say about it.
        /// </summary>
        private static void ValidateClassNames(InferenceSession session, int inputSize,
            string[] classNames, string onnxPath, string classNamesPath)
        {
            int expected = PredictedClassCount(session, inputSize, onnxPath);

            if (classNames.Length == 0)
            {
                throw new InvalidOperationException(
                    $"No class names for model '{Path.GetFileName(onnxPath)}', which predicts " +
                    $"{expected} classes. Expected a class file at '{classNamesPath}' with one " +
                    $"name per line.");
            }

            if (classNames.Length != expected)
            {
                throw new InvalidOperationException(
                    $"Class list does not match the model: '{Path.GetFileName(onnxPath)}' predicts " +
                    $"{expected} classes but '{Path.GetFileName(classNamesPath)}' lists " +
                    $"{classNames.Length}. Every reported label would be wrong. Ship the class " +
                    $"file that belongs to this model.");
            }
        }

        /// <summary>
        /// Read the class count from the output shape. LibreYOLO exports declare
        /// every output dimension as dynamic, so the metadata says nothing and the
        /// check used to be skipped for exactly the models this extension ships
        /// and trains. In that case run one inference on a blank frame and read
        /// the shape the model really produces.
        /// </summary>
        private static int PredictedClassCount(InferenceSession session, int inputSize, string onnxPath)
        {
            var dims = session.OutputMetadata.First().Value.Dimensions;
            if (dims.Length == 3 && dims[1] > 4)
                return dims[1] - 4;

            var input = new DenseTensor<float>(new[] { 1, 3, inputSize, inputSize });
            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor(session.InputMetadata.First().Key, input)
            };

            int[] shape;
            using (var results = session.Run(inputs))
            {
                shape = results.First().AsTensor<float>().Dimensions.ToArray();
            }

            if (shape.Length != 3 || shape[1] <= 4)
            {
                throw new InvalidOperationException(
                    $"Model '{Path.GetFileName(onnxPath)}' has an unsupported output shape " +
                    $"[{string.Join(", ", shape)}]. Expected [1, 4 + classes, anchors].");
            }

            return shape[1] - 4;
        }

        public List<Detection> Detect(Mat frame)
        {
            if (frame == null || frame.Empty()) return new List<Detection>();

            lock (_lock)
            {
                if (_session == null) return new List<Detection>();

                float ratioX, ratioY;
                int padX, padY;
                var tensorData = ImagePreprocessor.Preprocess(frame, _inputSize, out ratioX, out ratioY, out padX, out padY);

                var inputTensor = new DenseTensor<float>(tensorData, new[] { 1, 3, _inputSize, _inputSize });
                var inputName = _session.InputMetadata.First().Key;
                var inputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor(inputName, inputTensor)
                };

                using (var results = _session.Run(inputs))
                {
                    var outputTensor = results.First().AsTensor<float>();
                    var shape = outputTensor.Dimensions.ToArray();

                    // shape: [1, 84, 8400] for YOLOv8
                    int numClassesPlusFour = shape[1];
                    int numDetections = shape[2];
                    int numClasses = numClassesPlusFour - 4;

                    float[] outputArray = outputTensor.ToArray();

                    if (!_boxesAreXyxy.HasValue)
                        _boxesAreXyxy = DetectBoxFormat(outputArray, numDetections);

                    return PostProcessor.Process(outputArray, numClasses, numDetections,
                        _classNames, ConfidenceThreshold, NmsThreshold,
                        ratioX, ratioY, padX, padY, _boxesAreXyxy.Value);
                }
            }
        }

        /// <summary>
        /// Decide whether output rows 0-3 are (x1,y1,x2,y2) or (cx,cy,w,h).
        /// In corner format x2>=x1 and y2>=y1 holds for every anchor; in centre
        /// format rows 2-3 are width/height with no such relation. Measured
        /// separation on real models is ~100% vs ~3%, so 0.9 is a safe cut.
        /// </summary>
        private static bool DetectBoxFormat(float[] output, int numDetections)
        {
            if (numDetections <= 0) return false;

            int ordered = 0;
            for (int i = 0; i < numDetections; i++)
            {
                if (output[2 * numDetections + i] >= output[0 * numDetections + i] &&
                    output[3 * numDetections + i] >= output[1 * numDetections + i])
                {
                    ordered++;
                }
            }

            return (float)ordered / numDetections >= 0.9f;
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _session?.Dispose();
                _session = null;
            }
        }
    }
}
