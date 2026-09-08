using System;
using Peakboard.ExtensionKit;
using PeakboardExtensionObjectDetection.Inference;

namespace PeakboardExtensionObjectDetection.Extension
{
    /// <summary>
    /// One row, always, describing the health of the detection engine.
    ///
    /// This exists because the Detections list returns one row per detected
    /// object and no rows when there is nothing to report -- which is the
    /// correct shape for a table, but leaves nowhere to say "the camera did not
    /// open". A board can bind this list to a status label and see the real
    /// reason instead of an empty screen.
    ///
    /// It is a passive observer: it never starts the engine and never keeps it
    /// alive. Add it alongside the Camera or Detections list.
    /// </summary>
    [CustomListIcon("PeakboardExtensionObjectDetection.ObjectDetection.png")]
    public class StatusCustomList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "ObjectDetectionStatus",
                Name = "Object Detection - Status",
                Description = "Health of the detection engine: camera state, which model is actually loaded, and the last error.",
                PropertyInputPossible = false,
            };
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            return new CustomListColumnCollection
            {
                new CustomListColumn("Status", CustomListColumnTypes.String),
                new CustomListColumn("Error", CustomListColumnTypes.String),
                new CustomListColumn("IsRunning", CustomListColumnTypes.Boolean),
                new CustomListColumn("CameraSource", CustomListColumnTypes.String),
                new CustomListColumn("RequestedModel", CustomListColumnTypes.String),
                new CustomListColumn("LoadedModel", CustomListColumnTypes.String),
                new CustomListColumn("ClassCount", CustomListColumnTypes.Number),
                new CustomListColumn("DetectionCount", CustomListColumnTypes.Number),
                new CustomListColumn("FrameWidth", CustomListColumnTypes.Number),
                new CustomListColumn("FrameHeight", CustomListColumnTypes.Number),
                new CustomListColumn("Timestamp", CustomListColumnTypes.String),
            };
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            var items = new CustomListObjectElementCollection();

            if (!DetectionEngine.HasStarted)
            {
                items.Add(Row("not_started",
                    "Designer preview - the engine runs in the Peakboard Runtime",
                    false, "", "", "", 0, 0, 0, 0, ""));
                return items;
            }

            try
            {
                var r = DetectionEngine.GetLatest();

                items.Add(Row(
                    r.Status ?? "",
                    r.Error ?? "",
                    DetectionEngine.IsRunning,
                    DetectionEngine.CameraSourceName,
                    DetectionEngine.RequestedModel,
                    DetectionEngine.LoadedModelName,
                    DetectionEngine.LoadedClassCount,
                    r.Detections?.Count ?? 0,
                    r.FrameWidth,
                    r.FrameHeight,
                    r.Timestamp ?? ""));
            }
            catch (Exception ex)
            {
                Log?.Error($"[ObjectDetection] Status list failed: {ex}");
                items.Add(Row("error", ex.Message, false, "", "", "", 0, 0, 0, 0, ""));
            }

            return items;
        }

        private static CustomListObjectElement Row(string status, string error, bool running,
            string cameraSource, string requestedModel, string loadedModel, int classCount,
            int detectionCount, int frameWidth, int frameHeight, string timestamp)
        {
            return new CustomListObjectElement
            {
                { "Status", status },
                { "Error", error },
                { "IsRunning", running },
                { "CameraSource", cameraSource },
                { "RequestedModel", requestedModel },
                { "LoadedModel", loadedModel },
                { "ClassCount", (double)classCount },
                { "DetectionCount", (double)detectionCount },
                { "FrameWidth", (double)frameWidth },
                { "FrameHeight", (double)frameHeight },
                { "Timestamp", timestamp },
            };
        }

        protected override void SetupOverride(CustomListData data) { }
        protected override void CleanupOverride(CustomListData data) { }
    }
}
