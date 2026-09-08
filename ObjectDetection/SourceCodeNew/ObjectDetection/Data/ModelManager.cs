using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PeakboardExtensionObjectDetection.Data
{
    public class ModelInfo
    {
        public string Name { get; set; }
        public string OnnxPath { get; set; }
        public string ClassesPath { get; set; }
        public string Source { get; set; }
        public long SizeBytes { get; set; }
    }

    public class ModelManager
    {
        public List<ModelInfo> GetAvailableModels()
        {
            var models = new List<ModelInfo>();

            // 1. Scan PretrainedModels bundled with the extension
            var pretrainedDir = PathHelper.GetPretrainedModelsDir();
            if (Directory.Exists(pretrainedDir))
            {
                foreach (var onnxFile in Directory.GetFiles(pretrainedDir, "*.onnx"))
                {
                    var baseName = Path.GetFileNameWithoutExtension(onnxFile);
                    models.Add(new ModelInfo
                    {
                        Name = baseName,
                        OnnxPath = onnxFile,
                        ClassesPath = PathHelper.GetDefaultClassesPath(),
                        Source = "Pretrained",
                        SizeBytes = new FileInfo(onnxFile).Length
                    });
                }
            }

            // 2. Scan Peakboard Resources directory for user-added ONNX models
            var resourcesDir = PathHelper.GetResourcesDir();
            if (Directory.Exists(resourcesDir))
            {
                foreach (var onnxFile in Directory.GetFiles(resourcesDir, "*.onnx"))
                {
                    var baseName = Path.GetFileNameWithoutExtension(onnxFile);

                    // Check for a matching classes.txt next to the model
                    var classesPath = Path.Combine(resourcesDir, baseName + "_classes.txt");
                    if (!File.Exists(classesPath))
                    {
                        classesPath = Path.Combine(resourcesDir, "classes.txt");
                    }
                    if (!File.Exists(classesPath))
                    {
                        classesPath = PathHelper.GetDefaultClassesPath();
                    }

                    models.Add(new ModelInfo
                    {
                        Name = baseName,
                        OnnxPath = onnxFile,
                        ClassesPath = classesPath,
                        Source = "Resource",
                        SizeBytes = new FileInfo(onnxFile).Length
                    });
                }
            }

            // 3. Scan custom models directory
            PathHelper.EnsureDirectories();
            foreach (var modelsRoot in PathHelper.GetCustomModelDirs())
            {
                foreach (var dir in Directory.GetDirectories(modelsRoot))
                {
                    var onnxPath = Path.Combine(dir, "model.onnx");
                    var classesPath = Path.Combine(dir, "classes.txt");
                    var name = Path.GetFileName(dir);
                    if (File.Exists(onnxPath) && !models.Exists(m => m.Name == name))
                    {
                        models.Add(new ModelInfo
                        {
                            Name = name,
                            OnnxPath = onnxPath,
                            ClassesPath = File.Exists(classesPath) ? classesPath : PathHelper.GetDefaultClassesPath(),
                            Source = "Custom",
                            SizeBytes = new FileInfo(onnxPath).Length
                        });
                    }
                }
            }

            return models;
        }

        public ModelInfo GetModel(string name)
        {
            var models = GetAvailableModels();
            // Exact match first
            var match = models.FirstOrDefault(m => m.Name == name);
            if (match != null) return match;

            // Try case-insensitive match
            match = models.FirstOrDefault(m =>
                string.Equals(m.Name, name, System.StringComparison.OrdinalIgnoreCase));
            if (match != null) return match;

            // Try matching with underscores/dots normalized: Peakboard Resources
            // store a model added to a board with '_' where the filename had '.'
            var normalized = name.Replace("_", ".").Replace("-", ".");
            match = models.FirstOrDefault(m =>
                m.Name.Replace("_", ".").Replace("-", ".").Equals(normalized, System.StringComparison.OrdinalIgnoreCase));
            if (match != null) return match;

            // Deliberately no prefix matching. It used to accept a match in either
            // direction, so "yolo" resolved to whichever model happened to sort
            // first and "yolov9t_v2" silently resolved to "yolov9t" -- and because
            // that counts as a hit, the caller was never told a substitution had
            // happened. Returning null instead routes a near-miss through the
            // fallback path, which names what it loaded and why.
            return null;
        }
    }
}
