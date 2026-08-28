using System.Diagnostics;
using System.Text;
using Microsoft.ML.OnnxRuntimeGenAI;

namespace LocalAI
{
    /// <summary>
    /// Owns the language model for the life of the process.
    ///
    /// Loading costs seconds and gigabytes, so the model is loaded once on the first
    /// Ask and kept. Generation runs on a background thread and appends to
    /// <see cref="Answer"/> as each token arrives, so the data source can be polled
    /// to watch the answer build up rather than waiting for the whole thing.
    /// </summary>
    public static class LlmEngine
    {
        private static readonly object Gate = new object();
        private static readonly StringBuilder Buffer = new StringBuilder();

        private static Model _model;
        private static Tokenizer _tokenizer;
        private static string _loadedPath;
        private static string _loadedDevice;

        private static Thread _worker;
        private static volatile bool _cancel;

        private static volatile string _answer = "";
        private static volatile string _status = "idle";
        private static volatile string _error = "";
        private static volatile bool _busy;
        private static long _promptTokens;
        private static long _generated;
        private static double _ttftMs;
        private static double _tokensPerSec;

        public static string Answer => _answer;
        public static string Status => _status;
        public static string Error => _error;
        public static bool Busy => _busy;
        public static bool Loaded => _model != null;
        public static long PromptTokens => Interlocked.Read(ref _promptTokens);
        public static long Generated => Interlocked.Read(ref _generated);
        public static double TtftMs => _ttftMs;
        public static double TokensPerSec => _tokensPerSec;

        /// <summary>
        /// Starts generating and returns immediately. Poll Answer/Status for progress.
        /// Returns "busy" if a generation is already running.
        /// </summary>
        public static string Ask(string prompt, string modelPath, string device,
                                 string systemPrompt, int maxNewTokens, bool thinking)
        {
            if (_busy) return "busy";
            if (string.IsNullOrWhiteSpace(prompt)) return "empty prompt";

            _busy = true;
            _cancel = false;
            _error = "";
            lock (Buffer) { Buffer.Clear(); }
            _answer = "";
            Interlocked.Exchange(ref _promptTokens, 0);
            Interlocked.Exchange(ref _generated, 0);
            _ttftMs = 0;
            _tokensPerSec = 0;
            _status = "thinking";

            _worker = new Thread(() =>
                Generate(prompt, modelPath, device, systemPrompt, maxNewTokens, thinking))
            {
                IsBackground = true,
                Name = "LocalAI.Generate",
            };
            _worker.Start();
            return "started";
        }

        public static void Cancel() => _cancel = true;

        public static void Reset()
        {
            _cancel = true;
            lock (Buffer) { Buffer.Clear(); }
            _answer = "";
            _error = "";
            _status = Loaded ? "ready" : "idle";
        }

        public static void Unload()
        {
            lock (Gate)
            {
                _tokenizer?.Dispose();
                _model?.Dispose();
                _tokenizer = null;
                _model = null;
                _loadedPath = null;
                _loadedDevice = null;
                _status = "idle";
            }
        }

        private static void EnsureLoaded(string modelPath, string device)
        {
            lock (Gate)
            {
                if (_model != null && _loadedPath == modelPath && _loadedDevice == device)
                    return;

                Unload();

                if (string.IsNullOrWhiteSpace(modelPath))
                    throw new InvalidOperationException(
                        "ModelPath is empty. Point it at a folder containing genai_config.json.");
                if (!Directory.Exists(modelPath))
                    throw new DirectoryNotFoundException("Model folder not found: " + modelPath);
                if (!File.Exists(Path.Combine(modelPath, "genai_config.json")))
                    throw new FileNotFoundException(
                        "No genai_config.json in " + modelPath +
                        ". This must be an ONNX Runtime GenAI model folder, not raw .safetensors.");

                _status = "loading model";
                var config = new Config(modelPath);
                config.ClearProviders();

                if (!string.Equals(device, "cpu", StringComparison.OrdinalIgnoreCase))
                {
                    // Only reachable if this was built against the DirectML flavour of
                    // the GenAI package. The shipped build is CPU-only, so this throws
                    // a clear message rather than failing somewhere deeper.
                    try
                    {
                        config.AppendProvider(device);
                        config.SetProviderOption(device, "enable_graph_capture", "0");
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException(
                            "Device '" + device + "' is not available in this build. " +
                            "The published extension is CPU-only; set Device to 'cpu'. (" +
                            ex.Message + ")");
                    }
                }

                _model = new Model(config);
                _tokenizer = new Tokenizer(_model);
                _loadedPath = modelPath;
                _loadedDevice = device;
                _status = "ready";
            }
        }

        private static void Generate(string prompt, string modelPath, string device,
                                     string systemPrompt, int maxNewTokens, bool thinking)
        {
            try
            {
                EnsureLoaded(modelPath, device);
                _status = "thinking";

                var text = BuildPrompt(prompt, systemPrompt, thinking);

                using var seq = _tokenizer.Encode(text);
                int nPrompt = seq[0].Length;
                Interlocked.Exchange(ref _promptTokens, nPrompt);

                using var gp = new GeneratorParams(_model);
                gp.SetSearchOption("max_length", nPrompt + maxNewTokens);
                gp.SetSearchOption("do_sample", true);
                gp.SetSearchOption("temperature", 0.7);
                gp.SetSearchOption("top_p", 0.9);

                using var gen = new Generator(_model, gp);
                using var stream = _tokenizer.CreateStream();

                var sw = Stopwatch.StartNew();
                gen.AppendTokenSequences(seq);

                var genClock = new Stopwatch();
                bool first = true;
                int n = 0;

                while (!gen.IsDone() && !_cancel && n < maxNewTokens)
                {
                    gen.GenerateNextToken();

                    if (first)
                    {
                        // The first token only returns once the prompt has been
                        // processed, so this is time-to-first-token.
                        _ttftMs = sw.Elapsed.TotalMilliseconds;
                        _status = "writing";
                        first = false;
                        genClock.Start();
                    }

                    var full = gen.GetSequence(0);
                    var piece = stream.Decode(full[full.Length - 1]);
                    if (!string.IsNullOrEmpty(piece))
                    {
                        lock (Buffer)
                        {
                            Buffer.Append(piece);
                            _answer = StripThinking(Buffer.ToString());
                        }
                    }

                    n++;
                    Interlocked.Exchange(ref _generated, n);
                    if (genClock.Elapsed.TotalSeconds > 0)
                        _tokensPerSec = n / genClock.Elapsed.TotalSeconds;
                }

                _status = _cancel ? "cancelled" : "done";
            }
            catch (Exception ex)
            {
                _error = ex.GetType().Name + ": " + ex.Message;
                _status = "error";
            }
            finally
            {
                _busy = false;
            }
        }

        private static string BuildPrompt(string prompt, string systemPrompt, bool thinking)
        {
            // Reasoning models emit their scratchpad as ordinary tokens. On a slow
            // device that is minutes the user watches happen, so it is off unless
            // asked for. Qwen honours the /no_think marker; other models ignore it
            // harmlessly.
            var user = thinking ? prompt : prompt + " /no_think";

            var messages = new StringBuilder("[");
            if (!string.IsNullOrWhiteSpace(systemPrompt))
                messages.Append("{\"role\":\"system\",\"content\":")
                        .Append(JsonString(systemPrompt)).Append("},");
            messages.Append("{\"role\":\"user\",\"content\":")
                    .Append(JsonString(user)).Append("}]");

            try
            {
                return _tokenizer.ApplyChatTemplate(null, messages.ToString(), null, true);
            }
            catch
            {
                // Model folder has no chat template - fall back to plain text.
                return string.IsNullOrWhiteSpace(systemPrompt) ? user : systemPrompt + "\n\n" + user;
            }
        }

        /// <summary>
        /// Removes a reasoning block from the visible answer. Qwen still emits an
        /// empty &lt;think&gt;&lt;/think&gt; pair when reasoning is suppressed, and a
        /// dashboard should not show the scaffolding. While a block is still open,
        /// shows nothing rather than a half-written tag.
        /// </summary>
        internal static string StripThinking(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;

            int open = s.IndexOf("<think>", StringComparison.Ordinal);
            while (open >= 0)
            {
                int close = s.IndexOf("</think>", open, StringComparison.Ordinal);
                if (close < 0) return s.Substring(0, open).TrimStart();
                s = s.Remove(open, (close + "</think>".Length) - open);
                open = s.IndexOf("<think>", StringComparison.Ordinal);
            }
            return s.TrimStart();
        }

        private static string JsonString(string s)
        {
            var sb = new StringBuilder("\"");
            foreach (var ch in s)
            {
                switch (ch)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (ch < 0x20) sb.Append("\\u").Append(((int)ch).ToString("x4"));
                        else sb.Append(ch);
                        break;
                }
            }
            return sb.Append('"').ToString();
        }
    }
}
