using System;
using System.IO;
using System.Runtime.InteropServices;
using Peakboard.ExtensionKit;

namespace PeakboardExtensionObjectDetection.Extension
{
    [ExtensionIcon("PeakboardExtensionObjectDetection.ObjectDetection.png")]
    public class ObjectDetectionExtension : ExtensionBase
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        static ObjectDetectionExtension()
        {
            try
            {
                var assemblyDir = Path.GetDirectoryName(typeof(ObjectDetectionExtension).Assembly.Location);
                if (!string.IsNullOrEmpty(assemblyDir))
                {
                    SetDllDirectory(assemblyDir);
                    var currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
                    if (!currentPath.Contains(assemblyDir))
                    {
                        Environment.SetEnvironmentVariable("PATH", assemblyDir + ";" + currentPath);
                    }
                }
            }
            catch { }
        }

        public ObjectDetectionExtension() : base() { }
        public ObjectDetectionExtension(IExtensionHost host) : base(host) { }

        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "PeakboardExtensionObjectDetection",
                Name = "Object Detection",
                Description = "On-device object detection with YOLO. Supports USB, RTSP, and IP cameras.",
                Version = "1.0",
                MinVersion = "1.0",
                Author = "Peakboard",
                Company = "Peakboard GmbH",
                Copyright = "Copyright © Peakboard GmbH",
            };
        }

        protected override CustomListCollection GetCustomListsOverride()
        {
            return new CustomListCollection
            {
                new CameraCustomList(),
                new DetectionCustomList(),
                new ModelCustomList(),
                new StatusCustomList(),
                new CameraDeviceCustomList(),
            };
        }

        protected override void SetupOverride() { }
        protected override void CleanupOverride() { }
    }
}
