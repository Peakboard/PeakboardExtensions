using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using Peakboard.ExtensionKit;
using PeakboardExtensionObjectDetection.Data;
using PeakboardExtensionObjectDetection.Inference;

namespace PeakboardExtensionObjectDetection.Extension
{
    [CustomListIcon("PeakboardExtensionObjectDetection.ObjectDetection.png")]
    public class CameraCustomList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "ObjectDetectionCamera",
                Name = "Object Detection - Camera",
                Description = "Returns the latest camera frame with detection bounding boxes drawn as base64 JPEG.",
                PropertyInputPossible = true,
                PropertyInputDefaults =
                {
                    new CustomListPropertyDefinition { Name = "CameraSource", Value = "0" },
                    new CustomListPropertyDefinition { Name = "ModelName", Value = "yolov9t" },
                    new CustomListPropertyDefinition { Name = "ConfidenceThreshold", Value = "0.4" },
                },
            };
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            return new CustomListColumnCollection
            {
                new CustomListColumn("FrameBase64", CustomListColumnTypes.String),
                new CustomListColumn("RawFrameBase64", CustomListColumnTypes.String),
                new CustomListColumn("Width", CustomListColumnTypes.Number),
                new CustomListColumn("Height", CustomListColumnTypes.Number),
                new CustomListColumn("DetectionCount", CustomListColumnTypes.Number),
                new CustomListColumn("DetectionsJson", CustomListColumnTypes.String),
                new CustomListColumn("Timestamp", CustomListColumnTypes.String),
                new CustomListColumn("Status", CustomListColumnTypes.String),
            };
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            var items = new CustomListObjectElementCollection();

            // SetupOverride runs in the Runtime only, so an engine that has never
            // been started means Designer preview. An engine that started and then
            // died is a different thing entirely and must fall through to the real
            // status below -- reporting a dead camera as "preview" is what hid
            // camera failures until now.
            if (!DetectionEngine.HasStarted)
            {
                items.Add(new CustomListObjectElement
                {
                    { "FrameBase64", "" },
                    { "RawFrameBase64", "" },
                    { "Width", 0.0 },
                    { "Height", 0.0 },
                    { "DetectionCount", 0.0 },
                    { "DetectionsJson", "[]" },
                    { "Timestamp", "" },
                    { "Status", "Designer preview - the engine runs in the Peakboard Runtime" },
                });
                return items;
            }

            try
            {
                var result = DetectionEngine.GetLatest();

                var json = BuildDetectionsJson(result.Detections);

                items.Add(new CustomListObjectElement
                {
                    { "FrameBase64", result.AnnotatedJpeg != null ? Convert.ToBase64String(result.AnnotatedJpeg) : "" },
                    { "RawFrameBase64", result.RawJpeg != null ? Convert.ToBase64String(result.RawJpeg) : "" },
                    { "Width", (double)result.FrameWidth },
                    { "Height", (double)result.FrameHeight },
                    { "DetectionCount", (double)(result.Detections?.Count ?? 0) },
                    { "DetectionsJson", json },
                    { "Timestamp", result.Timestamp ?? "" },
                    { "Status", string.IsNullOrEmpty(result.Error) ? result.Status ?? "idle" : result.Error },
                });
            }
            catch (Exception ex)
            {
                Log?.Error($"CameraCustomList error: {ex.Message}");
            }

            return items;
        }

        /// <summary>
        /// Serialise detections with a real JSON writer.
        /// Class names come from a user-supplied classes.txt and can contain
        /// quotes or backslashes; the previous hand-built string produced invalid
        /// JSON for those, which broke every consumer downstream at once.
        /// </summary>
        private static string BuildDetectionsJson(List<Detection> detections)
        {
            using (var buffer = new MemoryStream())
            {
                using (var w = new Utf8JsonWriter(buffer))
                {
                    w.WriteStartArray();
                    if (detections != null)
                    {
                        for (int i = 0; i < detections.Count; i++)
                        {
                            var d = detections[i];
                            w.WriteStartObject();
                            w.WriteNumber("Index", i);
                            w.WriteString("ClassName", d.ClassName ?? "");
                            w.WriteNumber("ClassId", d.ClassId);
                            w.WriteNumber("Confidence", Math.Round(d.Confidence, 4));
                            w.WriteNumber("X", Math.Round(d.X, 1));
                            w.WriteNumber("Y", Math.Round(d.Y, 1));
                            w.WriteNumber("Width", Math.Round(d.Width, 1));
                            w.WriteNumber("Height", Math.Round(d.Height, 1));
                            w.WriteEndObject();
                        }
                    }
                    w.WriteEndArray();
                }
                return Encoding.UTF8.GetString(buffer.ToArray());
            }
        }

        protected override void SetupOverride(CustomListData data)
        {
            // SetupOverride is called when the runtime starts
            var source = data.Properties["CameraSource"] ?? "0";
            var model = data.Properties["ModelName"] ?? "yolov9t";

            float conf = 0.4f;
            float.TryParse(data.Properties["ConfidenceThreshold"] ?? "0.4",
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out conf);

            DetectionEngine.SetLogger(Log);
            DetectionEngine.Start(source, model, conf);
        }

        protected override void CleanupOverride(CustomListData data)
        {
            // Release, not Stop: the engine is shared and the Detections list may
            // still be reading from it.
            DetectionEngine.Release();
        }
    }
}
