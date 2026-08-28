using System.Globalization;
using Peakboard.ExtensionKit;

namespace LocalAI
{
    /// <summary>
    /// One-row data source carrying the current answer and generation state.
    ///
    /// Set the refresh interval to about a second. Each refresh returns a slightly
    /// longer Answer while the model is writing, which is what makes the text appear
    /// to type itself instead of the screen sitting blank until the answer is
    /// complete.
    /// </summary>
    [Serializable]
    [CustomListIcon("LocalAI.LocalAI.png")]
    public class ChatCustomList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "LocalAiChat",
                Name = "Local AI",
                Description = "Ask a locally running language model a question and stream the answer back.",
                PropertyInputPossible = true,
                PropertyInputDefaults =
                {
                    new CustomListPropertyDefinition
                    {
                        // Folder holding an ONNX Runtime GenAI model; must contain
                        // genai_config.json. The README explains how to build one.
                        Name = "ModelPath",
                        Value = @"C:\LocalAI\models\qwen3-0.6b",
                    },
                    new CustomListPropertyDefinition
                    {
                        // The published build is CPU-only.
                        Name = "Device",
                        Value = "cpu",
                    },
                    new CustomListPropertyDefinition
                    {
                        // Standing instruction given before every question.
                        Name = "SystemPrompt",
                        Value = "You are a helpful assistant. Answer briefly.",
                    },
                    new CustomListPropertyDefinition
                    {
                        // Upper bound on answer length. Every token costs time on a CPU.
                        Name = "MaxNewTokens",
                        Value = "128",
                    },
                    new CustomListPropertyDefinition
                    {
                        // true lets a reasoning model think out loud. Slow; off by default.
                        Name = "Thinking",
                        Value = "false",
                    },
                },
                Functions =
                {
                    new CustomListFunctionDefinition
                    {
                        Name = "Ask",
                        Description = "Send a prompt to the model. Returns at once; poll the list for the answer.",
                        InputParameters =
                        {
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "Prompt",
                                Type = CustomListFunctionParameterTypes.String,
                                Description = "The question, or a prompt built from board data.",
                            },
                        },
                        ReturnParameters =
                        {
                            new CustomListFunctionReturnParameterDefinition
                            {
                                Name = "Status",
                                Type = CustomListFunctionParameterTypes.String,
                            },
                        },
                    },
                    new CustomListFunctionDefinition
                    {
                        Name = "Cancel",
                        Description = "Stop the generation in progress.",
                        ReturnParameters =
                        {
                            new CustomListFunctionReturnParameterDefinition
                            {
                                Name = "Status",
                                Type = CustomListFunctionParameterTypes.String,
                            },
                        },
                    },
                    new CustomListFunctionDefinition
                    {
                        Name = "Reset",
                        Description = "Clear the current answer.",
                        ReturnParameters =
                        {
                            new CustomListFunctionReturnParameterDefinition
                            {
                                Name = "Status",
                                Type = CustomListFunctionParameterTypes.String,
                            },
                        },
                    },
                },
            };
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            return new CustomListColumnCollection
            {
                new CustomListColumn("Answer", CustomListColumnTypes.String),
                new CustomListColumn("Status", CustomListColumnTypes.String),
                new CustomListColumn("Busy", CustomListColumnTypes.Boolean),
                new CustomListColumn("PromptTokens", CustomListColumnTypes.Number),
                new CustomListColumn("TokensGenerated", CustomListColumnTypes.Number),
                new CustomListColumn("TokensPerSecond", CustomListColumnTypes.Number),
                new CustomListColumn("TimeToFirstTokenMs", CustomListColumnTypes.Number),
                new CustomListColumn("Error", CustomListColumnTypes.String),
            };
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            var items = new CustomListObjectElementCollection();

            // Never load a multi-gigabyte model just to draw a Designer preview.
            // Until something calls Ask, this stays a placeholder row.
            if (!LlmEngine.Loaded && !LlmEngine.Busy)
            {
                items.Add(Row(
                    answer: LlmEngine.Answer ?? "",
                    status: string.IsNullOrEmpty(LlmEngine.Error)
                        ? "idle - call Ask() to load the model"
                        : LlmEngine.Status));
                return items;
            }

            items.Add(Row(LlmEngine.Answer ?? "", LlmEngine.Status ?? "", live: true));
            return items;
        }

        private static CustomListObjectElement Row(string answer, string status, bool live = false)
        {
            return new CustomListObjectElement
            {
                { "Answer", answer },
                { "Status", status },
                { "Busy", live && LlmEngine.Busy },
                { "PromptTokens", live ? (double)LlmEngine.PromptTokens : 0.0 },
                { "TokensGenerated", live ? (double)LlmEngine.Generated : 0.0 },
                { "TokensPerSecond", live ? Math.Round(LlmEngine.TokensPerSec, 2) : 0.0 },
                { "TimeToFirstTokenMs", live ? Math.Round(LlmEngine.TtftMs, 0) : 0.0 },
                { "Error", LlmEngine.Error ?? "" },
            };
        }

        protected override CustomListExecuteReturnContext ExecuteFunctionOverride(
            CustomListData data, CustomListExecuteParameterContext context)
        {
            var ret = new CustomListExecuteReturnContext();

            try
            {
                switch (context.FunctionName)
                {
                    case "Ask":
                    {
                        var prompt = context.Values.GetByIndexOrDefault(0)?.StringValue ?? "";
                        var modelPath = data.Properties["ModelPath"] ?? "";
                        var device = (data.Properties["Device"] ?? "cpu").Trim();
                        var system = data.Properties["SystemPrompt"] ?? "";
                        var thinking = ParseBool(data.Properties["Thinking"], false);

                        if (!int.TryParse(data.Properties["MaxNewTokens"], NumberStyles.Integer,
                                          CultureInfo.InvariantCulture, out var maxTokens) || maxTokens <= 0)
                            maxTokens = 128;

                        Log?.Info($"LocalAI Ask: {prompt.Length} chars, device={device}, max={maxTokens}");
                        ret.Add(LlmEngine.Ask(prompt, modelPath, device, system, maxTokens, thinking));
                        break;
                    }

                    case "Cancel":
                        LlmEngine.Cancel();
                        ret.Add("cancelled");
                        break;

                    case "Reset":
                        LlmEngine.Reset();
                        ret.Add("reset");
                        break;

                    default:
                        ret.Add("unknown function: " + context.FunctionName);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log?.Error($"LocalAI {context.FunctionName} failed: {ex.Message}");
                ret.Add("error: " + ex.Message);
            }

            return ret;
        }

        private static bool ParseBool(string s, bool fallback)
        {
            if (string.IsNullOrWhiteSpace(s)) return fallback;
            s = s.Trim().ToLowerInvariant();
            return s == "true" || s == "1" || s == "yes";
        }
    }
}
