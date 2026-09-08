using System;
using Peakboard.ExtensionKit;
using PeakboardExtensionObjectDetection.Inference;

namespace PeakboardExtensionObjectDetection.Extension
{
    [CustomListIcon("PeakboardExtensionObjectDetection.ObjectDetection.png")]
    public class DetectionCustomList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "ObjectDetectionDetections",
                Name = "Object Detection - Detections",
                Description = "Runs YOLO inference on the current camera frame. Returns one row per detected object.",
                PropertyInputPossible = true,
                PropertyInputDefaults =
                {
                    new CustomListPropertyDefinition { Name = "CameraSource", Value = "0" },
                    new CustomListPropertyDefinition { Name = "ModelName", Value = "yolov9t" },
                    new CustomListPropertyDefinition { Name = "ConfidenceThreshold", Value = "0.4" },
                    new CustomListPropertyDefinition { Name = "NmsThreshold", Value = "0.45" },
                }
            };
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            return new CustomListColumnCollection
            {
                new CustomListColumn("ClassName", CustomListColumnTypes.String),
                new CustomListColumn("ClassId", CustomListColumnTypes.Number),
                new CustomListColumn("Confidence", CustomListColumnTypes.Number),
                new CustomListColumn("X", CustomListColumnTypes.Number),
                new CustomListColumn("Y", CustomListColumnTypes.Number),
                new CustomListColumn("Width", CustomListColumnTypes.Number),
                new CustomListColumn("Height", CustomListColumnTypes.Number),
                new CustomListColumn("Timestamp", CustomListColumnTypes.String),
                new CustomListColumn("FrameWidth", CustomListColumnTypes.Number),
                new CustomListColumn("FrameHeight", CustomListColumnTypes.Number),
                new CustomListColumn("Status", CustomListColumnTypes.String),
            };
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            var items = new CustomListObjectElementCollection();

            // Designer preview: one empty row so the columns are discoverable.
            // A started-then-failed engine is NOT this case and must fall through.
            if (!DetectionEngine.HasStarted)
            {
                items.Add(new CustomListObjectElement
                {
                    { "ClassName", "" },
                    { "ClassId", 0.0 },
                    { "Confidence", 0.0 },
                    { "X", 0.0 },
                    { "Y", 0.0 },
                    { "Width", 0.0 },
                    { "Height", 0.0 },
                    { "Timestamp", "" },
                    { "FrameWidth", 0.0 },
                    { "FrameHeight", 0.0 },
                    { "Status", "Designer preview - the engine runs in the Peakboard Runtime" },
                });
                return items;
            }

            try
            {
                var result = DetectionEngine.GetLatest();

                // One row per detected object, and no rows when nothing is
                // detected. The previous all-zero placeholder row made `count` 1
                // instead of 0, so every `for i = 0, count-1` in Lua processed a
                // detection that was not there. Health and errors belong on the
                // Status list, which always has exactly one row.
                if (result.Detections != null)
                {
                    foreach (var det in result.Detections)
                    {
                        items.Add(new CustomListObjectElement
                        {
                            { "ClassName", det.ClassName ?? "" },
                            { "ClassId", (double)det.ClassId },
                            { "Confidence", Math.Round(det.Confidence, 4) },
                            { "X", Math.Round(det.X, 1) },
                            { "Y", Math.Round(det.Y, 1) },
                            { "Width", Math.Round(det.Width, 1) },
                            { "Height", Math.Round(det.Height, 1) },
                            { "Timestamp", result.Timestamp ?? "" },
                            { "FrameWidth", (double)result.FrameWidth },
                            { "FrameHeight", (double)result.FrameHeight },
                            { "Status", string.IsNullOrEmpty(result.Error) ? result.Status ?? "" : result.Error },
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Log?.Error($"GetItems error: {ex.Message}");
            }

            return items;
        }

        protected override void SetupOverride(CustomListData data)
        {
            // SetupOverride is called when the runtime starts — this is where we start the engine
            var source = data.Properties["CameraSource"] ?? "0";
            var model = data.Properties["ModelName"] ?? "yolov9t";

            float conf = 0.4f, nms = 0.45f;
            float.TryParse(data.Properties["ConfidenceThreshold"] ?? "0.4",
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out conf);
            float.TryParse(data.Properties["NmsThreshold"] ?? "0.45",
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out nms);

            DetectionEngine.SetLogger(Log);
            DetectionEngine.Start(source, model, conf, nms);
        }

        protected override void CleanupOverride(CustomListData data)
        {
            // Release, not Stop. Stopping here killed the camera feed that the
            // Camera list was still using.
            DetectionEngine.Release();
        }
    }
}
