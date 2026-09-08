using System;
using System.Collections.Generic;
using System.IO;

namespace PeakboardExtensionObjectDetection.Data
{
    public static class PathHelper
    {
        /// <summary>
        /// ProgramData, not the root of C:. A Peakboard Box runs the Runtime under
        /// an account that generally cannot create a directory at C:\, and the old
        /// hardcoded path was created unguarded from inside the model listing --
        /// so on a locked-down machine an access-denied exception took out the
        /// entire model list rather than just the custom-model folder.
        /// </summary>
        public static readonly string BaseDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Peakboard", "ObjectDetection");

        public static readonly string ModelsDir = Path.Combine(BaseDir, "models");

        /// <summary>
        /// Where custom models lived before the move. Still read, never created,
        /// so an existing install does not lose its models.
        /// </summary>
        public static readonly string LegacyModelsDir = @"C:\PeakboardObjectDetection\models";

        /// <summary>Best effort. Never throws -- callers treat absence as "no custom models".</summary>
        public static void EnsureDirectories()
        {
            try { Directory.CreateDirectory(ModelsDir); }
            catch { }
        }

        /// <summary>Custom-model folders to scan, newest location first.</summary>
        public static IEnumerable<string> GetCustomModelDirs()
        {
            foreach (var dir in new[] { ModelsDir, LegacyModelsDir })
            {
                bool exists;
                try { exists = Directory.Exists(dir); }
                catch { exists = false; }
                if (exists) yield return dir;
            }
        }

        public static string GetExtensionDir()
        {
            return Path.GetDirectoryName(typeof(PathHelper).Assembly.Location);
        }

        public static string GetPretrainedModelsDir()
        {
            return Path.Combine(GetExtensionDir(), "PretrainedModels");
        }

        /// <summary>
        /// Returns the Peakboard Resources directory (../../Resources/ relative to extension dir).
        /// This is where user-added resources (like ONNX models) are stored at runtime.
        /// </summary>
        public static string GetResourcesDir()
        {
            var extDir = GetExtensionDir();
            // Extension is at .../Extensions/PeakboardExtensionObjectDetection/
            // Resources are at .../Resources/
            var resourcesDir = Path.GetFullPath(Path.Combine(extDir, "..", "..", "Resources"));
            return resourcesDir;
        }

        public static string GetDefaultClassesPath()
        {
            return Path.Combine(GetPretrainedModelsDir(), "coco_classes.txt");
        }
    }
}
