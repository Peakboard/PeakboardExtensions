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
            lock (_lock)
            {
                _session?.Dispose();

                if (!File.Exists(onnxPath))
                    throw new FileNotFoundException("ONNX model not found", onnxPath);

                var options = new SessionOptions();
                options.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
                options.InterOpNumThreads = 4;
                options.IntraOpNumThreads = 4;

                _session = new InferenceSession(onnxPath, options);
                _modelPath = onnxPath;
                _boxesAreXyxy = null;   // re-probe after a hot-reload

                // Determine input size from model metadata
                var inputMeta = _session.InputMetadata.First();
                if (inputMeta.Value.Dimensions.Length == 4)
                {
                    _inputSize = inputMeta.Value.Dimensions[2]; // NCHW: [1, 3, H, W]
                    if (_inputSize <= 0) _inputSize = 320;
                }

                // Load class names
                if (File.Exists(classNamesPath))
                {
                    _classNames = File.ReadAllLines(classNamesPath)
                        .Where(l => !string.IsNullOrWhiteSpace(l))
                        .ToArray();
                }
                else
                {
                    _classNames = new string[0];
                }

                ValidateClassNames(onnxPath, classNamesPath);
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
        private void ValidateClassNames(string onnxPath, string classNamesPath)
        {
            var dims = _session.OutputMetadata.First().Value.Dimensions;
            if (dims.Length != 3 || dims[1] <= 4)
                return;                       // dynamic or unexpected shape: nothing to check against

            int expected = dims[1] - 4;

            if (_classNames.Length == 0)
            {
                throw new InvalidOperationException(
                    $"No class names for model '{Path.GetFileName(onnxPath)}', which predicts " +
                    $"{expected} classes. Expected a class file at '{classNamesPath}' with one " +
                    $"name per line.");
            }

            if (_classNames.Length != expected)
            {
                throw new InvalidOperationException(
                    $"Class list does not match the model: '{Path.GetFileName(onnxPath)}' predicts " +
                    $"{expected} classes but '{Path.GetFileName(classNamesPath)}' lists " +
                    $"{_classNames.Length}. Every reported label would be wrong. Ship the class " +
                    $"file that belongs to this model.");
            }
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
