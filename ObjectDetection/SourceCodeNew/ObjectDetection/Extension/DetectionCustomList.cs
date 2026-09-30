using System;
using Peakboard.ExtensionKit;
using PeakboardExtensionObjectDetection.Data;
using PeakboardExtensionObjectDetection.Inference;

namespace PeakboardExtensionObjectDetection.Extension
{
    [Serializable]
    [CustomListIcon("PeakboardExtensionObjectDetection.ObjectDetection.png")]
    public class DetectionCustomList : CustomListBase
    {
        private const string HubUrlProperty = "HubUrl";
        private const string UserGroupKeyProperty = "UserGroupKey";
        private const string DatasetProperty = "DatasetName";
        private const string HubCheckSecondsProperty = "HubCheckSeconds";

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
                    // Optional: keep ModelName in step with a Hub dataset's current model.
                    // Left empty, the board runs the local model only.
                    new CustomListPropertyDefinition { Name = HubUrlProperty, Value = "" },
                    new CustomListPropertyDefinition
                    {
                        Name = UserGroupKeyProperty,
                        Value = "",
                        TypeDefinition = new CustomListPropertyStringTypeDefinition { Masked = true },
                    },
                    new CustomListPropertyDefinition { Name = DatasetProperty, Value = "" },
                    new CustomListPropertyDefinition { Name = HubCheckSecondsProperty, Value = "60" },
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
            try
            {
                // SetupOverride is called when the runtime starts — this is where we start the engine
                var source = data.Properties["CameraSource"] ?? "0";
                var model = data.Properties["ModelName"] ?? "yolov9t";

                var conf = (float)NumberProperty.Parse(data.Properties["ConfidenceThreshold"], 0.4);
                var nms = (float)NumberProperty.Parse(data.Properties["NmsThreshold"], 0.45);

                DetectionEngine.SetLogger(Log);
                DetectionEngine.Start(source, model, conf, nms);

                // After the engine: it starts on the fallback when the first Hub model has
                // not arrived yet, and switches to it as soon as the sync installed it.
                var checkSeconds = NumberProperty.Parse(Property(data, HubCheckSecondsProperty, "60"), 60);
                HubModelSync.Configure(
                    Property(data, HubUrlProperty, ""),
                    Property(data, UserGroupKeyProperty, ""),
                    Property(data, DatasetProperty, ""),
                    model,
                    checkSeconds > 0 ? checkSeconds : 60,
                    Log);
            }
            catch (Exception ex)
            {
                Log?.Error($"[ObjectDetection] Detections setup failed: {ex}");
            }
        }

        /// <summary>A board saved with an older version of the extension does not carry the newer properties.</summary>
        private static string Property(CustomListData data, string name, string fallback)
        {
            return data.Properties.TryGetValue(name, out var value) && value != null ? value : fallback;
        }

        protected override void CleanupOverride(CustomListData data)
        {
            try
            {
                HubModelSync.Stop();
                // Release, not Stop. Stopping here killed the camera feed that the
                // Camera list was still using.
                DetectionEngine.Release();
            }
            catch (Exception ex)
            {
                Log?.Error($"[ObjectDetection] Detections cleanup failed: {ex}");
            }
        }
    }
}
