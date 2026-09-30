using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Peakboard.ExtensionKit;
using PeakboardExtensionObjectDetection.Annotation;
using PeakboardExtensionObjectDetection.Inference;

namespace PeakboardExtensionObjectDetection.Extension
{
    /// <summary>
    /// Improving a trained model in daily use: detections the model was unsure about - below the
    /// confidence threshold, above MinConfidence - are offered to the operator one by one. An
    /// accepted detection goes to the Hub dataset as a box of origin "AcceptedSuggestion", a
    /// rejected one is never uploaded. A frame can instead be handed to the Annotation list and
    /// marked by hand.
    ///
    /// One row, always: the detection in question and the state of the queue. Every function
    /// returns true or false and puts the explanation into Message, like the Annotation list.
    /// It needs the Camera or Detections list on the same board - that is where frames come from.
    /// </summary>
    [Serializable]
    [CustomListIcon("PeakboardExtensionObjectDetection.ObjectDetection.png")]
    public class SuggestionCustomList : CustomListBase
    {
        private const string HubUrlProperty = "HubUrl";
        private const string UserGroupKeyProperty = "UserGroupKey";
        private const string DatasetProperty = "DatasetName";
        private const string MinConfidenceProperty = "MinConfidence";
        private const string CaptureIntervalProperty = "CaptureIntervalSeconds";
        private const string RepeatAfterProperty = "RepeatAfterSeconds";
        private const string MaxQueuedProperty = "MaxQueued";

        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "ObjectDetectionSuggestions",
                Name = "Object Detection - Suggestions",
                Description = "Offers detections the model was unsure about for acceptance. Accepted detections are added to the Hub dataset as training data.",
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
                    // Everything from here up to the Camera/Detections list's ConfidenceThreshold
                    // counts as "unsure".
                    new CustomListPropertyDefinition { Name = MinConfidenceProperty, Value = "0.15" },
                    new CustomListPropertyDefinition { Name = CaptureIntervalProperty, Value = "10" },
                    new CustomListPropertyDefinition { Name = RepeatAfterProperty, Value = "300" },
                    new CustomListPropertyDefinition { Name = MaxQueuedProperty, Value = "20" },
                },
                Functions =
                {
                    Function("Accept", "Accepts the detection in question as training data. Pass a class name to correct the model's class, or an empty text to keep it.",
                        new CustomListFunctionInputParameterDefinition
                        {
                            Name = "ClassName",
                            Description = "The correct class, or empty to keep the class the model named.",
                            Optional = true,
                            Type = CustomListFunctionParameterTypes.String,
                        }),
                    Function("Reject", "Rejects the detection in question. It is not uploaded."),
                    Function("Capture", "Offers the current frame's unsure detections right now, without waiting for the capture interval."),
                    Function("AnnotateByHand", "Hands the frame in question to the Annotation list, to mark its objects by hand, and drops it here."),
                    Function("Clear", "Drops every waiting suggestion. Nothing is uploaded."),
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

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            return new CustomListColumnCollection
            {
                new CustomListColumn("HasSuggestion", CustomListColumnTypes.Boolean),
                new CustomListColumn("SuggestionId", CustomListColumnTypes.String),
                new CustomListColumn("FrameBase64", CustomListColumnTypes.String),
                new CustomListColumn("FrameWidth", CustomListColumnTypes.Number),
                new CustomListColumn("FrameHeight", CustomListColumnTypes.Number),
                new CustomListColumn("ClassName", CustomListColumnTypes.String),
                new CustomListColumn("Confidence", CustomListColumnTypes.Number),
                new CustomListColumn("X", CustomListColumnTypes.Number),
                new CustomListColumn("Y", CustomListColumnTypes.Number),
                new CustomListColumn("Width", CustomListColumnTypes.Number),
                new CustomListColumn("Height", CustomListColumnTypes.Number),
                new CustomListColumn("CandidateNumber", CustomListColumnTypes.Number),
                new CustomListColumn("CandidateCount", CustomListColumnTypes.Number),
                new CustomListColumn("QueuedFrames", CustomListColumnTypes.Number),
                new CustomListColumn("AcceptedCount", CustomListColumnTypes.Number),
                new CustomListColumn("RejectedCount", CustomListColumnTypes.Number),
                new CustomListColumn("Message", CustomListColumnTypes.String),
                new CustomListColumn("PendingUploads", CustomListColumnTypes.Number),
                new CustomListColumn("UploadStatus", CustomListColumnTypes.String),
            };
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            var items = new CustomListObjectElementCollection();
            try
            {
                var s = SuggestionQueue.Snapshot();
                var current = s.Current;
                var candidate = current != null && s.CandidateIndex >= 0 && s.CandidateIndex < current.Candidates.Count
                    ? current.Candidates[s.CandidateIndex] : null;
                var message = s.Message ?? "";
                if (!DetectionEngine.HasStarted && current == null)
                    message = "Designer preview - suggestions are collected in the Peakboard Runtime";

                items.Add(new CustomListObjectElement
                {
                    { "HasSuggestion", candidate != null },
                    { "SuggestionId", current?.Id ?? "" },
                    { "FrameBase64", s.PreviewJpeg != null ? Convert.ToBase64String(s.PreviewJpeg) : "" },
                    { "FrameWidth", (double)(current?.FrameWidth ?? 0) },
                    { "FrameHeight", (double)(current?.FrameHeight ?? 0) },
                    { "ClassName", candidate?.ClassName ?? "" },
                    { "Confidence", candidate?.Confidence ?? 0.0 },
                    { "X", candidate?.Box.X ?? 0.0 },
                    { "Y", candidate?.Box.Y ?? 0.0 },
                    { "Width", candidate?.Box.Width ?? 0.0 },
                    { "Height", candidate?.Box.Height ?? 0.0 },
                    { "CandidateNumber", (double)(candidate != null ? s.CandidateIndex + 1 : 0) },
                    { "CandidateCount", (double)(current?.Candidates.Count ?? 0) },
                    { "QueuedFrames", (double)s.Queued },
                    { "AcceptedCount", (double)s.AcceptedTotal },
                    { "RejectedCount", (double)s.RejectedTotal },
                    { "Message", message },
                    { "PendingUploads", (double)SampleOutbox.PendingCount },
                    { "UploadStatus", SampleOutbox.UploadStatus ?? "" },
                });
            }
            catch (Exception ex)
            {
                Log?.Error($"[ObjectDetection] Suggestions list failed: {ex}");
            }
            return items;
        }

        protected override CustomListExecuteReturnContext ExecuteFunctionOverride(
            CustomListData data, CustomListExecuteParameterContext context)
        {
            var ret = new CustomListExecuteReturnContext { ReloadAfterExecution = true };
            try
            {
                SampleOutbox.ConfigureHub(Property(data, HubUrlProperty, ""), Property(data, UserGroupKeyProperty, ""), Log);
                var dataset = Property(data, DatasetProperty, "").Trim();
                Func<Suggestion, string> save = s => SaveFrame(s, dataset);

                bool ok;
                switch (context.FunctionName)
                {
                    case "Accept":
                        if (dataset.Length == 0)
                        {
                            // Without it an accepted detection would have nowhere to go.
                            SuggestionQueue.SetMessage("DatasetName is not set on the Suggestions data source. Nothing was accepted.");
                            ok = false;
                            break;
                        }
                        ok = SuggestionQueue.Accept(context.Values.GetByIndexOrDefault(0)?.StringValue ?? "", save);
                        break;
                    case "Reject":
                        ok = SuggestionQueue.Reject(save);
                        break;
                    case "Capture":
                        ok = SuggestionQueue.CaptureNow();
                        break;
                    case "AnnotateByHand":
                        ok = SuggestionQueue.AnnotateByHand();
                        break;
                    case "Clear":
                        SuggestionQueue.Clear();
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
                SuggestionQueue.SetMessage($"{context.FunctionName} failed: {ex.Message}");
                ret = new CustomListExecuteReturnContext { ReloadAfterExecution = true };
                ret.Add(false);
            }
            return ret;
        }

        /// <summary>The decided frame, with its accepted detections, into the outbox. Null when saved.</summary>
        private static string SaveFrame(Suggestion s, string dataset)
        {
            if (dataset.Length == 0)
                return "DatasetName is not set on the Suggestions data source. The frame stays here - nothing is lost.";

            var now = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            var snapshot = new AnnotationSnapshot
            {
                IsFrozen = true,
                FrameJpeg = s.FrameJpeg,
                FrameWidth = s.FrameWidth,
                FrameHeight = s.FrameHeight,
                CapturedAt = s.CapturedAt,
                Objects = s.Candidates
                    .Where(c => c.Decision == SuggestionDecision.Accepted)
                    .Select(c => new AnnotatedObject
                    {
                        ClassName = c.AcceptedClass,
                        Box = c.Box,
                        DrawingJson = "",
                        CreatedAt = now,
                        Origin = AnnotatedObject.OriginAcceptedSuggestion,
                        Confidence = c.Confidence,
                    })
                    .ToList(),
            };

            try
            {
                SampleOutbox.Save(snapshot, dataset);
                return null;
            }
            catch (IOException ex) { return $"Could not save on this device: {ex.Message}"; }
            catch (UnauthorizedAccessException ex) { return $"Could not save on this device: {ex.Message}"; }
        }

        protected override void SetupOverride(CustomListData data)
        {
            try
            {
                SampleOutbox.ConfigureHub(Property(data, HubUrlProperty, ""), Property(data, UserGroupKeyProperty, ""), Log);

                var min = Number(data, MinConfidenceProperty, 0.15);
                DetectionEngine.SetSuggestionThreshold((float)Math.Max(0.01, Math.Min(0.99, min)));
                SuggestionQueue.Start(
                    Number(data, CaptureIntervalProperty, 10),
                    Number(data, RepeatAfterProperty, 300),
                    (int)Number(data, MaxQueuedProperty, 20));
            }
            catch (Exception ex)
            {
                Log?.Error($"[ObjectDetection] Suggestions setup failed: {ex}");
            }
        }

        protected override void CleanupOverride(CustomListData data)
        {
            try
            {
                SuggestionQueue.Stop();
                DetectionEngine.SetSuggestionThreshold(0);
            }
            catch (Exception ex)
            {
                Log?.Error($"[ObjectDetection] Suggestions cleanup failed: {ex}");
            }
        }

        private static string Property(CustomListData data, string name, string fallback)
        {
            return data.Properties.TryGetValue(name, out var value) && value != null ? value : fallback;
        }

        private static double Number(CustomListData data, string name, double fallback)
        {
            return double.TryParse(Property(data, name, ""), NumberStyles.Float, CultureInfo.InvariantCulture, out var v)
                && !double.IsNaN(v) && !double.IsInfinity(v) ? v : fallback;
        }
    }
}
