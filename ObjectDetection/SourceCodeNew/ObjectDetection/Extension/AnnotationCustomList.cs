using System;
using System.IO;
using System.Text;
using System.Text.Json;
using Peakboard.ExtensionKit;
using PeakboardExtensionObjectDetection.Annotation;

namespace PeakboardExtensionObjectDetection.Extension
{
    /// <summary>
    /// Teaching a new object on the device: freeze a camera frame, draw around each object
    /// in a Drawing Area laid over the frame, confirm the box derived from the drawing, give
    /// it a class, save. Saved frames go to the Hub dataset named in DatasetName.
    ///
    /// One row, always: the state of the annotation session. Every function returns true or
    /// false and puts the explanation into Message, so a board needs no error handling of its
    /// own - bind a text to Message.
    /// </summary>
    [Serializable]
    [CustomListIcon("PeakboardExtensionObjectDetection.ObjectDetection.png")]
    public class AnnotationCustomList : CustomListBase
    {
        private const string HubUrlProperty = "HubUrl";
        private const string UserGroupKeyProperty = "UserGroupKey";
        private const string DatasetProperty = "DatasetName";
        private const string StretchProperty = "ImageStretch";

        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "ObjectDetectionAnnotation",
                Name = "Object Detection - Annotation",
                Description = "Freeze a camera frame, mark objects on it by drawing around them, and save the frame with its boxes to a Hub dataset.",
                PropertyInputPossible = true,
                PropertyInputDefaults =
                {
                    new CustomListPropertyDefinition { Name = HubUrlProperty, Value = "" },
                    new CustomListPropertyDefinition
                    {
                        Name = UserGroupKeyProperty,
                        Value = "",
                        TypeDefinition = new CustomListPropertyStringTypeDefinition { Masked = true },
                    },
                    new CustomListPropertyDefinition { Name = DatasetProperty, Value = "" },
                    new CustomListPropertyDefinition
                    {
                        // How the image control under the Drawing Area shows the frozen frame.
                        // The box can only be mapped back onto the frame if this matches it.
                        Name = StretchProperty,
                        Value = "Uniform",
                        TypeDefinition = new CustomListPropertyStringTypeDefinition
                        {
                            SelectableValues = new[] { "Uniform", "Fill" },
                        },
                    },
                },
                Functions =
                {
                    Function("Freeze", "Freezes the current camera frame for annotation. Refused while the frozen frame has unsaved objects - Save or Cancel first."),
                    Function("ProposeBox", "Turns a drawing around an object into a box and shows it for confirmation. The box is the rectangle through the topmost, leftmost, rightmost and bottommost point of the drawing.",
                        Input("Drawing", "The drawing, as the Drawing Area's getstrokes() returns it."),
                        Input("ClassName", "The class of the object.")),
                    Function("ConfirmBox", "Adds the box waiting for confirmation to the frame."),
                    Function("DiscardBox", "Drops the box waiting for confirmation."),
                    Function("RemoveLastBox", "Removes the object added last."),
                    Function("Save", "Saves the frozen frame with its objects and uploads it to the Hub dataset."),
                    Function("Cancel", "Drops the frozen frame and everything marked on it."),
                },
            };
        }

        private static CustomListFunctionDefinition Function(string name, string description,
            params CustomListFunctionInputParameterDefinition[] inputs)
        {
            var function = new CustomListFunctionDefinition
            {
                Name = name,
                Description = description,
                ReturnParameters =
                {
                    new CustomListFunctionReturnParameterDefinition
                    {
                        Name = "Ok",
                        Type = CustomListFunctionParameterTypes.Boolean,
                        Description = "True when it worked. Otherwise Message says why.",
                    },
                },
            };
            foreach (var input in inputs) function.InputParameters.Add(input);
            return function;
        }

        private static CustomListFunctionInputParameterDefinition Input(string name, string description)
        {
            return new CustomListFunctionInputParameterDefinition
            {
                Name = name,
                Description = description,
                Optional = false,
                Type = CustomListFunctionParameterTypes.String,
            };
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            return new CustomListColumnCollection
            {
                new CustomListColumn("IsFrozen", CustomListColumnTypes.Boolean),
                new CustomListColumn("FrameBase64", CustomListColumnTypes.String),
                new CustomListColumn("FrameWidth", CustomListColumnTypes.Number),
                new CustomListColumn("FrameHeight", CustomListColumnTypes.Number),
                new CustomListColumn("ObjectCount", CustomListColumnTypes.Number),
                new CustomListColumn("HasPendingBox", CustomListColumnTypes.Boolean),
                new CustomListColumn("PendingClass", CustomListColumnTypes.String),
                new CustomListColumn("PendingX", CustomListColumnTypes.Number),
                new CustomListColumn("PendingY", CustomListColumnTypes.Number),
                new CustomListColumn("PendingWidth", CustomListColumnTypes.Number),
                new CustomListColumn("PendingHeight", CustomListColumnTypes.Number),
                new CustomListColumn("AnnotationsJson", CustomListColumnTypes.String),
                new CustomListColumn("Message", CustomListColumnTypes.String),
                new CustomListColumn("LastSampleId", CustomListColumnTypes.String),
                new CustomListColumn("PendingUploads", CustomListColumnTypes.Number),
                new CustomListColumn("UploadStatus", CustomListColumnTypes.String),
            };
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            var items = new CustomListObjectElementCollection();
            try
            {
                var s = AnnotationSession.Snapshot();
                var pending = s.Pending;
                items.Add(new CustomListObjectElement
                {
                    { "IsFrozen", s.IsFrozen },
                    { "FrameBase64", s.PreviewJpeg != null ? Convert.ToBase64String(s.PreviewJpeg) : "" },
                    { "FrameWidth", (double)s.FrameWidth },
                    { "FrameHeight", (double)s.FrameHeight },
                    { "ObjectCount", (double)s.Objects.Count },
                    { "HasPendingBox", pending != null },
                    { "PendingClass", pending?.ClassName ?? "" },
                    { "PendingX", pending?.Box.X ?? 0.0 },
                    { "PendingY", pending?.Box.Y ?? 0.0 },
                    { "PendingWidth", pending?.Box.Width ?? 0.0 },
                    { "PendingHeight", pending?.Box.Height ?? 0.0 },
                    { "AnnotationsJson", AnnotationsJson(s) },
                    { "Message", s.Message ?? "" },
                    { "LastSampleId", s.LastSampleId ?? "" },
                    { "PendingUploads", (double)SampleOutbox.PendingCount },
                    { "UploadStatus", SampleOutbox.UploadStatus ?? "" },
                });
            }
            catch (Exception ex)
            {
                Log?.Error($"[ObjectDetection] Annotation list failed: {ex}");
            }
            return items;
        }

        protected override CustomListExecuteReturnContext ExecuteFunctionOverride(
            CustomListData data, CustomListExecuteParameterContext context)
        {
            var ret = new CustomListExecuteReturnContext { ReloadAfterExecution = true };
            try
            {
                Configure(data);
                bool ok;
                switch (context.FunctionName)
                {
                    case "Freeze":
                        ok = AnnotationSession.Freeze();
                        break;
                    case "ProposeBox":
                        ok = AnnotationSession.ProposeBox(
                            context.Values.GetByIndexOrDefault(0)?.StringValue ?? "",
                            context.Values.GetByIndexOrDefault(1)?.StringValue ?? "",
                            data.Properties[StretchProperty] ?? "Uniform");
                        break;
                    case "ConfirmBox":
                        ok = AnnotationSession.ConfirmBox();
                        break;
                    case "DiscardBox":
                        ok = AnnotationSession.DiscardBox();
                        break;
                    case "RemoveLastBox":
                        ok = AnnotationSession.RemoveLastBox();
                        break;
                    case "Save":
                        ok = Save();
                        break;
                    case "Cancel":
                        AnnotationSession.Cancel("Discarded. Freeze a frame to start.");
                        ok = true;
                        break;
                    default:
                        Log?.Error($"[ObjectDetection] Unknown function {context.FunctionName}.");
                        ok = false;
                        break;
                }
                ret.Add(ok);
            }
            catch (Exception ex)
            {
                Log?.Error($"[ObjectDetection] {context.FunctionName} failed: {ex}");
                AnnotationSession.SetMessage($"{context.FunctionName} failed: {ex.Message}");
                ret = new CustomListExecuteReturnContext { ReloadAfterExecution = true };
                ret.Add(false);
            }
            return ret;
        }

        private static bool Save()
        {
            var s = AnnotationSession.Snapshot();
            if (!s.IsFrozen)
            {
                // A second tap on Save: the frame is already saved - keep saying so.
                if (!s.JustSaved) AnnotationSession.SetMessage("Freeze a frame first.");
                return false;
            }
            if (s.Pending != null)
            {
                AnnotationSession.SetMessage("Confirm or discard the box first.");
                return false;
            }
            if (s.Objects.Count == 0)
            {
                AnnotationSession.SetMessage("Mark at least one object before saving.");
                return false;
            }
            if (SampleOutbox.Dataset.Length == 0)
            {
                // Without it the sample would have no dataset to go to. The frame stays frozen.
                AnnotationSession.SetMessage("DatasetName is not set on the Annotation data source. The frame stays frozen - nothing is lost.");
                return false;
            }

            string sampleId;
            try { sampleId = SampleOutbox.Save(s); }
            catch (IOException ex)
            {
                AnnotationSession.SetMessage($"Could not save on this device: {ex.Message}");
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                AnnotationSession.SetMessage($"Could not save on this device: {ex.Message}");
                return false;
            }

            AnnotationSession.Saved(sampleId, s.Objects.Count);
            return true;
        }

        private void Configure(CustomListData data)
        {
            SampleOutbox.Configure(
                data.Properties[HubUrlProperty],
                data.Properties[UserGroupKeyProperty],
                data.Properties[DatasetProperty],
                Log);
        }

        private static string AnnotationsJson(AnnotationSnapshot s)
        {
            using (var buffer = new MemoryStream())
            {
                using (var w = new Utf8JsonWriter(buffer))
                {
                    w.WriteStartArray();
                    foreach (var o in s.Objects)
                    {
                        w.WriteStartObject();
                        w.WriteString("ClassName", o.ClassName);
                        w.WriteNumber("X", o.Box.X);
                        w.WriteNumber("Y", o.Box.Y);
                        w.WriteNumber("Width", o.Box.Width);
                        w.WriteNumber("Height", o.Box.Height);
                        w.WriteEndObject();
                    }
                    w.WriteEndArray();
                }
                return Encoding.UTF8.GetString(buffer.ToArray());
            }
        }

        protected override void SetupOverride(CustomListData data)
        {
            try
            {
                // Starts the uploader, so samples left over from an earlier run - the Hub was
                // down, the device was switched off - go out without waiting for the next save.
                Configure(data);
            }
            catch (Exception ex)
            {
                Log?.Error($"[ObjectDetection] Annotation setup failed: {ex}");
            }
        }

        protected override void CleanupOverride(CustomListData data) { }
    }
}
