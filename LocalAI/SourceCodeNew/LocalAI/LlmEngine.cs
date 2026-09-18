using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
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

        // Read from genai_config.json at load. All zero means the shape could not be
        // read, and every check that depends on it is skipped rather than guessed.
        private static int _contextLength;
        private static int _numHeads;
        private static int _numKvHeads;
        private static int _headSize;
        private static int _numLayers;

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

        /// <summary>
        /// The model's output before <see cref="StripThinking"/> runs. Diagnostic:
        /// when Answer is empty but tokens were generated, this is what it produced.
        /// </summary>
        public static string RawAnswer { get { lock (Buffer) { return Buffer.ToString(); } } }
        public static string Status => _status;
        public static string Error => _error;
        public static bool Busy => _busy;
        public static bool Loaded => _model != null;
        public static int ContextLength => _contextLength;

        /// <summary>
        /// The memory guard refuses a prompt the machine cannot hold. Off only for
        /// measurement - tools/PromptLimitProbe turns it off to observe the raw
        /// runtime failure it exists to prevent. Never turn it off in a board.
        /// </summary>
        public static bool MemoryGuard = true;
        public static long PromptTokens => Interlocked.Read(ref _promptTokens);
        public static long Generated => Interlocked.Read(ref _generated);
        public static double TtftMs => _ttftMs;
        public static double TokensPerSec => _tokensPerSec;

        /// <summary>
        /// Starts generating and returns immediately. Poll Answer/Status for progress.
        /// Returns "busy" if a generation is already running.
        /// </summary>
        public static string Ask(string prompt, string modelPath, string device,
                                 string systemPrompt, int maxNewTokens, bool thinking,
                                 int maxPromptTokens)
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
                Generate(prompt, modelPath, device, systemPrompt, maxNewTokens, thinking,
                         maxPromptTokens))
            {
                IsBackground = true,
                Name = "LocalAI.Generate",
            };
            _worker.Start();
            return "started";
        }

        public static void Cancel() => _cancel = true;

        /// <summary>
        /// Records a failure that happened outside the generation thread - a
        /// malformed property, an unknown function - so that it reaches the board
        /// through the same Error column as every other failure.
        ///
        /// Deliberately does not touch Status or Busy: this can be called while a
        /// generation is running, and reporting a property error must not make the
        /// board think the model stopped.
        /// </summary>
        public static void ReportError(string message) => _error = message ?? "";

        /// <summary>
        /// Is this folder an ONNX Runtime GenAI model? Returns null if it is, or a
        /// sentence describing what is wrong if it is not.
        ///
        /// Existence checks only - nothing here loads a model, so it is cheap enough
        /// to run on every Designer preview. That matters: without it the first
        /// report of a mistyped path came from a Box, out of a board that was
        /// already deployed.
        ///
        /// The message names what the folder actually holds. A previous version
        /// always said "not raw .safetensors", which to someone holding a GGUF model
        /// reads as an answer to a question they did not ask.
        /// </summary>
        /// <param name="designTime">
        /// True when called from the Designer preview, where the model is usually
        /// NOT on this machine - it lives on the Box or the BYOD device the board
        /// will be deployed to. An absent folder is then unremarkable and must not
        /// be reported as a fault. Everything else on this list is still worth
        /// saying, because it describes a folder that is here and is wrong.
        /// </param>
        public static string CheckModelFolder(string modelPath, bool designTime = false)
        {
            if (string.IsNullOrWhiteSpace(modelPath))
                return "ModelPath is empty. Point it at a folder containing genai_config.json.";

            if (!Directory.Exists(modelPath))
                return designTime
                    ? "Not checked: no such folder on this machine. That is normal if the "
                      + "model lives on the Box or BYOD device - this preview only sees the "
                      + "Designer's own disk. If you expected it here, the path is wrong: "
                      + modelPath
                    : "Model folder not found: " + modelPath;

            if (File.Exists(Path.Combine(modelPath, "genai_config.json")))
                return null;

            // Prebuilt repositories nest the model several levels down, and pointing
            // at the download root is the commonest mistake with this extension. If
            // the real folder is in there somewhere, name it rather than making
            // someone go hunting.
            try
            {
                var nested = Directory.GetFiles(modelPath, "genai_config.json",
                                                SearchOption.AllDirectories);
                if (nested.Length > 0)
                    return "ModelPath points one level too high. genai_config.json is in "
                         + Path.GetDirectoryName(nested[0])
                         + " - set ModelPath to that folder.";
            }
            catch (Exception)
            {
                // Unreadable subfolder: fall through to the format checks below.
            }

            if (Directory.GetFiles(modelPath, "*.gguf").Length > 0)
                return "This is a GGUF model, which is llama.cpp's format. ONNX Runtime "
                     + "GenAI cannot load it, and there is no setting that makes it. Use an "
                     + "ONNX GenAI build instead - onnx-community publishes them on Hugging "
                     + "Face - or convert one with onnxruntime_genai.models.builder.";

            if (Directory.GetFiles(modelPath, "*.safetensors").Length > 0)
                return "This folder holds raw Hugging Face weights (.safetensors), not an "
                     + "ONNX Runtime GenAI model. Convert it with "
                     + "onnxruntime_genai.models.builder, or download a prebuilt ONNX build.";

            if (Directory.GetFiles(modelPath, "*.onnx").Length > 0)
                return "This folder has an .onnx file but no genai_config.json, so ONNX "
                     + "Runtime GenAI cannot tell how to run it. A GenAI model folder carries "
                     + "genai_config.json and tokenizer.json beside the weights.";

            if (Directory.GetFiles(modelPath).Length == 0
                    && Directory.GetDirectories(modelPath).Length == 0)
                return "Model folder is empty: " + modelPath;

            return "No genai_config.json in " + modelPath
                 + ". This must be an ONNX Runtime GenAI model folder.";
        }

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
                _contextLength = 0;
                _numHeads = _numKvHeads = _headSize = _numLayers = 0;
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

                var problem = CheckModelFolder(modelPath);
                if (problem != null)
                    throw new InvalidOperationException(problem);

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
                ReadModelShape(modelPath);
                _loadedPath = modelPath;
                _loadedDevice = device;
                _status = "ready";
            }
        }

        private static void Generate(string prompt, string modelPath, string device,
                                     string systemPrompt, int maxNewTokens, bool thinking,
                                     int maxPromptTokens)
        {
            try
            {
                EnsureLoaded(modelPath, device);
                _status = "thinking";

                var text = BuildPrompt(prompt, systemPrompt, thinking);

                using var seq = _tokenizer.Encode(text);
                int nPrompt = seq[0].Length;
                Interlocked.Exchange(ref _promptTokens, nPrompt);

                // Three walls stand between a prompt and an answer, and ONNX
                // Runtime's own message for two of them is unreadable on a
                // dashboard. Check all three here, while the numbers still mean
                // something, and in the order that gives the most actionable
                // sentence when more than one applies.
                //
                //   1. the model's context window - a hard ceiling, cheap to explain
                //   2. MaxPromptTokens - the board author's own policy
                //   3. memory - physics, and the only one that cannot be configured
                //      away. MaxPromptTokens alone protected nothing but its default:
                //      raise it to 40,000 and a 36,882-token prompt sailed straight
                //      through into a 163 GB allocation failure inside layer 0.

                if (_contextLength > 0 && nPrompt >= _contextLength)
                    throw new InvalidOperationException(string.Format(
                        "Prompt is {0:N0} tokens; this model's context is {1:N0}. " +
                        "Nothing can be generated. Send fewer rows.",
                        nPrompt, _contextLength));

                // Trim the answer rather than fail: the prompt fits, only the
                // requested answer length does not.
                int budget = _contextLength > 0
                    ? Math.Min(maxNewTokens, _contextLength - nPrompt)
                    : maxNewTokens;

                if (maxPromptTokens > 0 && nPrompt > maxPromptTokens)
                {
                    // Name the machine ceiling too. Raising MaxPromptTokens is the
                    // obvious next move and this is the number that says how far it
                    // can go - the alternative is finding out by crashing.
                    int room = MemoryCeilingTokens(budget);
                    throw new InvalidOperationException(string.Format(
                        "Prompt is {0:N0} tokens; MaxPromptTokens is {1:N0}. Attention " +
                        "memory grows with the square of the prompt and the time not far " +
                        "behind, so large prompts stop being usable well before the " +
                        "model's context limit.{2} Send fewer rows, or summarise them " +
                        "before asking.",
                        nPrompt, maxPromptTokens,
                        room > 0
                            ? string.Format(" On this machine about {0:N0} tokens would " +
                                            "fit, so MaxPromptTokens can safely go that " +
                                            "high and no higher.", room)
                            : ""));
                }

                ThrowIfPromptWillNotFit(nPrompt, budget);

                using var gp = new GeneratorParams(_model);
                gp.SetSearchOption("max_length", nPrompt + budget);
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

                while (!gen.IsDone() && !_cancel && n < budget)
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

                // Resolve what the board will actually show. Up to here _answer has
                // been the streaming view, which deliberately hides an unclosed
                // <think>; now that nothing more is coming, an empty view means the
                // user gets a blank panel after a generation that worked.
                if (!_cancel)
                {
                    lock (Buffer) { _answer = FinalAnswer(Buffer.ToString(), n >= budget); }
                }

                _status = _cancel ? "cancelled" : "done";
            }
            catch (Exception ex)
            {
                // An InvalidOperationException here is one of our own guards above and
                // already reads as a sentence; anything else is the runtime's and
                // needs its type to be identifiable.
                _error = ex is InvalidOperationException
                    ? ex.Message
                    : ex.GetType().Name + ": " + ex.Message;
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
        /// What to show once generation has finished.
        ///
        /// Normally the answer with any reasoning block removed. If that leaves
        /// nothing - a reasoning model that never closed its &lt;think&gt;, or spent
        /// the whole token budget inside one - show the reasoning instead, and say
        /// so. It is not the answer that was asked for, but it is what the model
        /// produced, and a blank panel tells the operator nothing at all.
        /// </summary>
        internal static string FinalAnswer(string raw, bool hitTokenLimit)
        {
            var visible = StripThinking(raw);
            if (!string.IsNullOrWhiteSpace(visible)) return visible;

            var inner = raw.Replace("<think>", "").Replace("</think>", "").Trim();
            if (inner.Length == 0) return visible;

            // Cut off mid-thought: the budget really is the problem, and the text is
            // a fragment rather than an answer.
            if (hitTokenLimit)
                return "The model ran out of tokens before finishing - raise "
                     + "MaxNewTokens to give it room. What it had written:\n\n" + inner;

            // Finished on its own with the tag left open. The text is the answer; the
            // model was simply sloppy about closing a marker, and saying anything
            // about token limits here would be wrong and would send the reader off
            // to change a setting that is not involved.
            return inner;
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

        // ------------------------------------------------------------------
        // Will it fit?
        //
        // ONNX Runtime's CPU attention kernel materialises the whole score matrix
        // in one allocation, so the memory a prompt costs goes as the SQUARE of
        // its length:
        //
        //     scores = num_attention_heads x tokens^2 x 4 bytes
        //
        // That is not a model of the cost, it is the allocation the runtime
        // actually asks for. At 36,882 tokens against Qwen3-4B's 32 heads it
        // predicts 174,116,086,272 bytes; the arena reported 174,720,360,960.
        // 0.35% out.
        //
        // The KV cache is the other big one, and it is linear. genai_config sets
        // past_present_share_buffer, so it is allocated once for the whole
        // max_length rather than growing as tokens arrive:
        //
        //     kv = 2 x layers x kv_heads x head_size x (prompt + answer) x 4 bytes
        //
        // Peak working set runs above the sum of those two, because the arena
        // holds transients besides. Measured on a Ryzen 7 PRO 7840U with Qwen3-4B
        // int4 (bench/RESULTS.md), peak-above-model against the computed sum:
        //
        //      1,055 tokens    1.0 GB /  0.43 GB   2.3x
        //      2,078 tokens    2.5 GB /  1.09 GB   2.3x
        //      4,124 tokens    6.4 GB /  3.17 GB   2.0x
        //      8,216 tokens   13.6 GB / 10.31 GB   1.3x
        //
        // Hence 2.0. It is the flat part of that column, and it is conservative at
        // the top end - which is the right bias here. Over-refusing costs one
        // sentence naming exactly where the line is; under-refusing costs the
        // runtime.
        // ------------------------------------------------------------------

        internal const double PeakToAllocationRatio = 2.0;

        private static long KvBytesPerToken() =>
            2L * _numLayers * _numKvHeads * _headSize * 4;

        /// <summary>Bytes this prompt is expected to cost on top of the loaded model.</summary>
        internal static long PredictPeakBytes(int nPrompt, int nGenerate)
        {
            long n = nPrompt;
            long scores = (long)_numHeads * n * n * 4;
            long kv = KvBytesPerToken() * (n + nGenerate);
            return (long)((scores + kv) * PeakToAllocationRatio);
        }

        /// <summary>
        /// Inverts <see cref="PredictPeakBytes"/>: the longest prompt that fits in a
        /// given number of bytes. Solves r(an^2 + b(n+g)) = budget for n.
        /// </summary>
        internal static int LargestPromptThatFits(long availableBytes, int nGenerate)
        {
            if (_numHeads <= 0) return 0;

            double a = PeakToAllocationRatio * _numHeads * 4.0;
            double b = PeakToAllocationRatio * KvBytesPerToken();
            double c = b * nGenerate - availableBytes;

            double disc = b * b - 4 * a * c;
            if (disc < 0) return 0;

            double n = (-b + Math.Sqrt(disc)) / (2 * a);
            if (n < 0) n = 0;
            if (_contextLength > 0 && n > _contextLength - 1) n = _contextLength - 1;
            return (int)n;
        }

        /// <summary>
        /// The longest prompt this machine can take right now, or 0 if the model has
        /// not described itself well enough to say.
        /// </summary>
        public static int MemoryCeilingTokens(int nGenerate)
        {
            if (_numHeads <= 0) return 0;
            long available = AvailablePhysicalBytes();
            return available <= 0 ? 0 : LargestPromptThatFits(available, nGenerate);
        }

        /// <summary>
        /// Refuses a prompt this machine cannot hold, before ONNX Runtime is asked
        /// for the allocation. Its own failure reads
        /// "BFCArena::AllocateRawInternal Failed to allocate memory for requested
        /// buffer of size 174720360960" against a node in layer 0, which names
        /// neither the prompt nor anything a board author can act on.
        /// </summary>
        private static void ThrowIfPromptWillNotFit(int nPrompt, int nGenerate)
        {
            if (!MemoryGuard || _numHeads <= 0) return;

            long available = AvailablePhysicalBytes();
            if (available <= 0) return;   // cannot tell - do not block on a guess

            long needed = PredictPeakBytes(nPrompt, nGenerate);
            if (needed <= available) return;

            int fits = LargestPromptThatFits(available, nGenerate);

            throw new InvalidOperationException(string.Format(
                "Prompt is {0:N0} tokens, which needs about {1}; only {2} is free on this " +
                "machine. Attention memory grows with the SQUARE of the prompt, so the real " +
                "ceiling sits far below the model's {3:N0}-token context window. {4} Send " +
                "fewer rows, or summarise them before asking.",
                nPrompt, Bytes(needed), Bytes(available), _contextLength,
                fits > 0
                    ? string.Format("About {0:N0} tokens fit here right now.", fits)
                    : "Not even a short prompt fits - free some memory, or use a smaller model."));
        }

        private static string Bytes(long b)
        {
            const double G = 1024.0 * 1024.0 * 1024.0;
            double g = b / G;
            if (g >= 10) return g.ToString("N0") + " GB";
            if (g >= 1) return g.ToString("N1") + " GB";
            return (b / (1024.0 * 1024.0)).ToString("N0") + " MB";
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MemoryStatusEx
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx buffer);

        /// <summary>
        /// Free PHYSICAL memory. Deliberately not virtual: paging a ten-gigabyte
        /// attention buffer is indistinguishable from a hang. The model is already
        /// loaded by the time this is called, so its own gigabytes are already gone
        /// from this number - which is what makes it the right one to compare against.
        /// Returns 0 if it cannot be read.
        /// </summary>
        internal static long AvailablePhysicalBytes()
        {
            try
            {
                var st = new MemoryStatusEx();
                st.dwLength = (uint)Marshal.SizeOf<MemoryStatusEx>();
                if (GlobalMemoryStatusEx(ref st)) return (long)st.ullAvailPhys;
            }
            catch { /* fall through */ }
            return 0;
        }

        /// <summary>
        /// The model's context window and attention shape, from genai_config.json.
        /// Anything it cannot read stays 0 and the check that needs it is skipped -
        /// a model that will not describe itself still gets to run.
        /// </summary>
        private static void ReadModelShape(string modelPath)
        {
            _contextLength = _numHeads = _numKvHeads = _headSize = _numLayers = 0;
            try
            {
                using var doc = JsonDocument.Parse(
                    File.ReadAllText(Path.Combine(modelPath, "genai_config.json")));
                if (!doc.RootElement.TryGetProperty("model", out var m)) return;

                _contextLength = ReadInt(m, "context_length");
                if (!m.TryGetProperty("decoder", out var d)) return;

                _numHeads = ReadInt(d, "num_attention_heads");
                _numKvHeads = ReadInt(d, "num_key_value_heads");
                _headSize = ReadInt(d, "head_size");
                _numLayers = ReadInt(d, "num_hidden_layers");

                // A plain multi-head model omits num_key_value_heads entirely; there
                // it is the same as the attention head count.
                if (_numKvHeads <= 0) _numKvHeads = _numHeads;
            }
            catch { /* fall through */ }
        }

        private static int ReadInt(JsonElement e, string name) =>
            e.TryGetProperty(name, out var v) && v.TryGetInt32(out var n) ? n : 0;

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
