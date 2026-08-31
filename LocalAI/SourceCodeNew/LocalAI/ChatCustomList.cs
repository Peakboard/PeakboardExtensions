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
                        // genai_config.json. The README explains how to get one.
                        Name = "ModelPath",
                        Value = @"C:\LocalAI\models\qwen3-0.6b",
                    },
                    new CustomListPropertyDefinition
                    {
                        // The published build is CPU-only; "dml" only resolves in a
                        // rebuild against the DirectML flavour of the GenAI package,
                        // and LlmEngine says so rather than failing obscurely.
                        Name = "Device",
                        Value = "cpu",
                        TypeDefinition = new CustomListPropertyStringTypeDefinition
                        {
                            SelectableValues = new[] { "cpu", "dml" },
                        },
                    },
                    new CustomListPropertyDefinition
                    {
                        // Standing instruction given before every question.
                        Name = "SystemPrompt",
                        Value = "You are a helpful assistant. Answer briefly.",
                        TypeDefinition = new CustomListPropertyStringTypeDefinition
                        {
                            MultiLine = true,
                        },
                    },
                    new CustomListPropertyDefinition
                    {
                        // Upper bound on answer length. Every token costs time on a CPU.
                        Name = "MaxNewTokens",
                        Value = "128",
                        TypeDefinition = new CustomListPropertyNumberTypeDefinition
                        {
                            Integer = true, Minimum = 1, Maximum = 8192,
                        },
                    },
                    new CustomListPropertyDefinition
                    {
                        // Refuses an oversized prompt before ONNX Runtime tries to
                        // allocate a quadratic attention buffer and dies with a
                        // message nobody can act on. 0 disables the check.
                        Name = "MaxPromptTokens",
                        Value = "2048",
                        TypeDefinition = new CustomListPropertyNumberTypeDefinition
                        {
                            Integer = true, Minimum = 0, Maximum = 131072,
                        },
                    },
                    new CustomListPropertyDefinition
                    {
                        // true lets a reasoning model think out loud. Slow; off by default.
                        Name = "Thinking",
                        Value = "false",
                        TypeDefinition = new CustomListPropertyBooleanTypeDefinition(),
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
                                Name = "Started",
                                Type = CustomListFunctionParameterTypes.String,
                                Description = "\"started\", or why nothing was started: " +
                                              "\"busy\" if a generation is already running, " +
                                              "or \"empty prompt\".",
                            },
                        },
                    },
                    // Cancel and Reset declare no return value: they always succeed,
                    // so a returned constant told the caller nothing. Watch Status
                    // instead. Ask keeps its return because it genuinely varies -
                    // "busy" means the call did nothing at all.
                    new CustomListFunctionDefinition
                    {
                        Name = "Cancel",
                        Description = "Stop the generation in progress.",
                    },
                    new CustomListFunctionDefinition
                    {
                        Name = "Reset",
                        Description = "Clear the current answer.",
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
                        var maxTokens = ParseInt(data.Properties["MaxNewTokens"], 128, 1);
                        var maxPrompt = ParseInt(data.Properties["MaxPromptTokens"], 2048, 0);

                        // Generation runs on a background thread, so a failure there
                        // cannot be caught here. Route it to the log as well as the
                        // Error column - a board that does not bind Error would
                        // otherwise just sit at status "error" with nothing to go on.
                        LlmEngine.ErrorSink = m => Log?.Error("LocalAI: " + m);

                        Log?.Info($"LocalAI Ask: {prompt.Length} chars, device={device}, " +
                                  $"maxNew={maxTokens}, maxPrompt={maxPrompt}");
                        ret.Add(LlmEngine.Ask(prompt, modelPath, device, system,
                                              maxTokens, thinking, maxPrompt));
                        break;
                    }

                    // Neither declares a return parameter, so neither adds one.
                    case "Cancel":
                        LlmEngine.Cancel();
                        break;

                    case "Reset":
                        LlmEngine.Reset();
                        break;

                    default:
                        Log?.Error("LocalAI: unknown function " + context.FunctionName);
                        break;
                }
            }
            catch (Exception ex)
            {
                Log?.Error($"LocalAI {context.FunctionName} failed: {ex.Message}");
                if (context.FunctionName == "Ask") ret.Add("error: " + ex.Message);
            }

            return ret;
        }

        /// <summary>
        /// A typed property still arrives as a string, and an empty box must not turn
        /// into a zero-token answer or a zero-token prompt limit.
        /// </summary>
        private static int ParseInt(string s, int fallback, int minimum)
        {
            if (!int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n))
                return fallback;
            return n < minimum ? fallback : n;
        }

        private static bool ParseBool(string s, bool fallback)
        {
            if (string.IsNullOrWhiteSpace(s)) return fallback;
            s = s.Trim().ToLowerInvariant();
            return s == "true" || s == "1" || s == "yes";
        }
    }
}
