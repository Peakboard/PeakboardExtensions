// Inference for the LocalAI extension, on llama.cpp.
//
// 2.0 replaced ONNX Runtime GenAI with llama.cpp. The public surface here is
// unchanged, because the board contract - one row, a growing Answer, a Status, an
// Error column - was never the problem. What changed underneath, and why, is in
// internal/LocalAI/bench/RESULTS.md; the short version is that two rules this file
// used to enforce turned out to be properties of ONNX Runtime rather than of the
// hardware:
//
//   * "int4 is a trap" - ORT's MatMulNBits has no SSE kernel, so 4-bit collapsed to
//     0.3 tok/s on a Box. The same CPU running the same 4-bit weights through
//     llama.cpp does 7-10 tok/s, faster than the fp16 adopted to avoid it.
//
//   * "prompt length is the wall, quadratically" - ORT's CPU attention materialises
//     an n x n scores matrix. llama.cpp uses flash attention and does not, so its
//     memory is LINEAR in context: 112 KiB per token for Qwen3-0.6B, with a compute
//     buffer that stays flat from 2k to 16k.
//
// The second is why the guard below is arithmetic rather than a quadratic solved
// backwards, and why n_ctx is something this engine *chooses to fit the machine*
// instead of something it has to defend against.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace LocalAI
{
    public static class LlmEngine
    {
        private static readonly object Gate = new object();
        internal static readonly StringBuilder Buffer = new StringBuilder();

        private static IntPtr _model = IntPtr.Zero;
        private static IntPtr _ctx = IntPtr.Zero;
        private static IntPtr _vocab = IntPtr.Zero;
        private static string _loadedPath;
        private static int _ctxSize;

        // Read from the GGUF at load. Zero means the shape could not be established,
        // and the memory guard stands down rather than guessing.
        private static int _contextLength;
        private static int _numHeadKv;
        private static int _numLayers;
        private static int _kDim, _vDim;
        private static long _modelBytes;

        private static volatile string _status = "idle";
        private static volatile string _error = "";
        private static volatile string _answer = "";
        private static volatile bool _busy;
        private static volatile bool _cancel;
        private static Thread _worker;

        private static long _promptTokens;
        private static long _generated;
        private static volatile float _ttftMsF;
        private static volatile float _tokensPerSecF;

        public static string Answer => _answer;
        public static string RawAnswer { get { lock (Buffer) { return Buffer.ToString(); } } }
        public static string Status => _status;
        public static string Error => _error;
        public static bool Busy => _busy;
        public static bool Loaded => _model != IntPtr.Zero;
        public static int ContextLength => _contextLength;
        public static bool MemoryGuard = true;
        public static long PromptTokens => Interlocked.Read(ref _promptTokens);
        public static long Generated => Interlocked.Read(ref _generated);
        public static double TtftMs => _ttftMsF;
        public static double TokensPerSec => _tokensPerSecF;

        // ------------------------------------------------------------------
        // Natives
        // ------------------------------------------------------------------

        private static bool _nativesReady;
        private static string _nativeProblem;

        /// <summary>
        /// Loads llama.cpp from the extension's own folder.
        ///
        /// Two things here are not optional. The DLLs are loaded by absolute path,
        /// because the working directory of the extension host is not ours. And
        /// ggml_backend_load_all_from_path must be called with that same folder:
        /// llama.cpp resolves its dispatched CPU kernels (fourteen ggml-cpu-*.dll,
        /// one per instruction set) relative to the EXECUTABLE, which here is the
        /// Peakboard extension host in the Peakboard install. Without it the backend
        /// registry comes up empty and every generation fails with nothing useful to
        /// say. Measured: 0 devices without, 1 with. _platform ERRATA 131.
        /// </summary>
        private static void EnsureNatives()
        {
            if (_nativesReady) return;
            lock (Gate)
            {
                if (_nativesReady) return;
                if (_nativeProblem != null) throw new InvalidOperationException(_nativeProblem);

                try
                {
                    var dir = Path.GetDirectoryName(typeof(LlmEngine).Assembly.Location ?? "") ?? "";

                    foreach (var n in new[] { "ggml-base.dll", "ggml.dll", "llama.dll" })
                    {
                        var full = Path.Combine(dir, n);
                        if (!File.Exists(full))
                            throw new FileNotFoundException(
                                "Missing " + n + " beside the extension. The package is "
                                + "incomplete - reinstall the extension.", full);
                        NativeLibrary.Load(full);
                    }

                    LlamaNative.ggml_backend_load_all_from_path(dir);

                    if (LlamaNative.ggml_backend_dev_count() == 0)
                        throw new InvalidOperationException(
                            "llama.cpp loaded but found no compute backend in " + dir
                            + ". The ggml-cpu-*.dll files are missing from the package.");

                    LlamaNative.llama_backend_init();
                    _nativesReady = true;
                }
                catch (Exception ex)
                {
                    _nativeProblem = "Could not start the inference engine: " + ex.Message;
                    throw new InvalidOperationException(_nativeProblem);
                }
            }
        }

        // ------------------------------------------------------------------
        // Public entry points
        // ------------------------------------------------------------------

        public static string Ask(string prompt, string modelPath, string device,
                                 string systemPrompt, int maxNewTokens, bool thinking,
                                 int maxPromptTokens, double temperature = 0.7)
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
            _ttftMsF = 0;
            _tokensPerSecF = 0;
            _status = "thinking";

            _worker = new Thread(() =>
                Generate(prompt, modelPath, device, systemPrompt, maxNewTokens, thinking,
                         maxPromptTokens, temperature))
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

        public static void Reset()
        {
            Cancel();
            lock (Buffer) { Buffer.Clear(); }
            _answer = "";
            _error = "";
            _status = "idle";
        }

        public static void Unload()
        {
            lock (Gate)
            {
                if (_ctx != IntPtr.Zero) { LlamaNative.llama_free(_ctx); _ctx = IntPtr.Zero; }
                if (_model != IntPtr.Zero) { LlamaNative.llama_model_free(_model); _model = IntPtr.Zero; }
                _vocab = IntPtr.Zero;
                _loadedPath = null;
                _ctxSize = 0;
                _contextLength = 0;
                _numHeadKv = _numLayers = _kDim = _vDim = 0;
                _modelBytes = 0;
                _status = "idle";
            }
        }

        // ------------------------------------------------------------------
        // Model path
        // ------------------------------------------------------------------

        /// <summary>
        /// Is this a model llama.cpp can load? Returns null if it is, or a sentence
        /// describing what is wrong if it is not.
        ///
        /// 2.0 inverted this. ModelPath is now a <b>.gguf file</b>, not a folder
        /// containing genai_config.json, so the format that used to be the answer is
        /// now the thing that has to be explained.
        ///
        /// Existence checks only - nothing here loads a model, so it is cheap enough
        /// to run on a Designer preview.
        /// </summary>
        /// <param name="designTime">
        /// True when called from the Designer preview, where the model is usually
        /// NOT on this machine - it lives on the Box or the BYOD device the board
        /// will be deployed to. An absent file is then unremarkable and must not be
        /// reported as a fault. Everything else on this list is still worth saying,
        /// because it describes something that is here and is wrong.
        /// </param>
        public static string CheckModelFolder(string modelPath, bool designTime = false)
        {
            if (string.IsNullOrWhiteSpace(modelPath))
                return "ModelPath is empty. Point it at a .gguf model file.";

            if (File.Exists(modelPath))
            {
                if (modelPath.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase))
                    return null;

                return "ModelPath is a file but not a .gguf: " + Path.GetFileName(modelPath)
                     + ". This extension loads GGUF models.";
            }

            if (Directory.Exists(modelPath))
            {
                // The commonest mistake after the upgrade: a 1.x board, or a path
                // copied from the old README.
                var gguf = SafeGlob(modelPath, "*.gguf");
                if (gguf.Count == 1)
                    return "ModelPath is a folder. Since 2.0 it must be the model FILE - use "
                         + gguf[0];
                if (gguf.Count > 1)
                    return "ModelPath is a folder holding " + gguf.Count + " .gguf files. Since "
                         + "2.0 it must be the model FILE - name the one you want.";

                if (File.Exists(Path.Combine(modelPath, "genai_config.json")))
                    return "This is an ONNX Runtime GenAI model folder, which LocalAI 1.x used. "
                         + "2.0 runs GGUF instead: download a .gguf build of the same model and "
                         + "point ModelPath at the file.";

                if (SafeGlob(modelPath, "*.onnx").Count > 0)
                    return "This folder holds an ONNX model. 2.0 runs GGUF - download a .gguf "
                         + "build and point ModelPath at the file.";

                if (SafeGlob(modelPath, "*.safetensors").Count > 0)
                    return "This folder holds raw Hugging Face weights (.safetensors). Download "
                         + "a .gguf build of the model instead, or convert it with llama.cpp's "
                         + "convert_hf_to_gguf.py.";

                return "No .gguf file in " + modelPath + ". ModelPath must be the model file itself.";
            }

            return designTime
                ? "Not checked: no such file on this machine. That is normal if the model lives "
                  + "on the Box or BYOD device - this preview only sees the Designer's own disk. "
                  + "If you expected it here, the path is wrong: " + modelPath
                : "Model file not found: " + modelPath;
        }

        private static List<string> SafeGlob(string dir, string pattern)
        {
            try { return new List<string>(Directory.GetFiles(dir, pattern)); }
            catch (Exception) { return new List<string>(); }
        }

        // ------------------------------------------------------------------
        // Loading
        // ------------------------------------------------------------------

        private static void EnsureModel(string modelPath)
        {
            lock (Gate)
            {
                if (_model != IntPtr.Zero && _loadedPath == modelPath) return;

                Unload();

                var problem = CheckModelFolder(modelPath);
                if (problem != null) throw new InvalidOperationException(problem);

                _status = "loading model";
                EnsureNatives();

                var mp = LlamaNative.llama_model_default_params();
                _model = LlamaNative.llama_model_load_from_file(modelPath, mp);
                if (_model == IntPtr.Zero)
                    throw new InvalidOperationException(
                        "llama.cpp could not load " + Path.GetFileName(modelPath)
                        + ". The file may be truncated, or built for a newer llama.cpp than "
                        + "this extension bundles (" + LlamaBuild.PinnedBuild + ").");

                _vocab = LlamaNative.llama_model_get_vocab(_model);
                ReadModelShape();
                _loadedPath = modelPath;
                _status = "ready";
            }
        }

        /// <summary>
        /// Everything the memory guard needs, read from the GGUF itself.
        ///
        /// head_dim is NOT n_embd / n_head - Qwen3-0.6B has n_embd 1024 across 16
        /// heads but a head dim of 128, and deriving it the obvious way gives half
        /// the right KV size. It comes from the metadata instead.
        /// </summary>
        private static void ReadModelShape()
        {
            _contextLength = LlamaNative.llama_model_n_ctx_train(_model);
            _numHeadKv = LlamaNative.llama_model_n_head_kv(_model);
            _numLayers = LlamaNative.llama_model_n_layer(_model);
            _modelBytes = (long)LlamaNative.llama_model_size(_model);

            var arch = Meta("general.architecture");
            _kDim = MetaInt(arch + ".attention.key_length");
            _vDim = MetaInt(arch + ".attention.value_length");

            if (_kDim <= 0 || _vDim <= 0)
            {
                // Not every architecture writes those keys. n_embd / n_head is the
                // usual fallback and is right for most models; where it is wrong it
                // is wrong low, so the guard becomes optimistic rather than silent.
                var nHead = LlamaNative.llama_model_n_head(_model);
                var nEmbd = LlamaNative.llama_model_n_embd(_model);
                var d = nHead > 0 ? nEmbd / nHead : 0;
                if (_kDim <= 0) _kDim = d;
                if (_vDim <= 0) _vDim = d;
            }
        }

        private static string Meta(string key)
        {
            var buf = Marshal.AllocHGlobal(256);
            try
            {
                var n = LlamaNative.llama_model_meta_val_str(_model, key, buf, 256);
                return n > 0 ? Utf8.Read(buf, n) : "";
            }
            catch (Exception) { return ""; }
            finally { Marshal.FreeHGlobal(buf); }
        }

        private static int MetaInt(string key)
        {
            return int.TryParse(Meta(key), out var v) ? v : 0;
        }

        // ------------------------------------------------------------------
        // The memory guard - linear, because llama.cpp's memory is
        // ------------------------------------------------------------------

        /// <summary>
        /// Bytes of KV cache per token of context. Checked exactly against
        /// llama.cpp's own reported figure on Qwen3-0.6B: 28 layers x 8 kv heads x
        /// (128 + 128) x 2 bytes = 114,688 = 112 KiB, and llama.cpp reported 224 MiB
        /// for 2,048 cells. Linear in n_ctx - 224 / 896 / 1,792 MiB at 2k / 8k / 16k.
        /// </summary>
        internal static long KvBytesPerToken()
        {
            if (_numLayers <= 0 || _numHeadKv <= 0) return 0;
            return 2L * _numLayers * _numHeadKv * (_kDim + _vDim);
        }

        /// <summary>
        /// The compute buffer llama.cpp reserves. Flat with respect to context -
        /// measured 300.75 MiB at both 2k and 8k, 320.75 MiB at 16k on Qwen3-0.6B -
        /// so a constant with headroom rather than a function. Larger models reserve
        /// more; the allowance is deliberately generous, because over-reserving costs
        /// a sentence and under-reserving costs the Runtime.
        /// </summary>
        internal const long ComputeBufferBytes = 512L * 1024 * 1024;

        /// <summary>
        /// The largest context this machine can hold right now, or 0 if the model has
        /// not described itself well enough to say.
        /// </summary>
        public static int MemoryCeilingTokens(int nGenerate)
        {
            var perToken = KvBytesPerToken();
            if (perToken <= 0) return 0;

            long available = AvailablePhysicalBytes();
            if (available <= 0) return 0;

            // Once the model is loaded its bytes are already resident, so they are
            // not spent twice.
            long fixedCost = ComputeBufferBytes + (_model == IntPtr.Zero ? _modelBytes : 0);
            long spare = available - fixedCost;
            if (spare <= 0) return 0;

            long n = spare / perToken;
            if (_contextLength > 0 && n > _contextLength) n = _contextLength;
            return (int)Math.Max(0, Math.Min(int.MaxValue, n));
        }

        /// <summary>
        /// Picks a context size for this prompt and answer, and refuses when even
        /// that will not fit.
        ///
        /// Under ORT this had to defend against one enormous allocation part way
        /// through inference. llama.cpp allocates the KV cache once, at context
        /// creation, sized by n_ctx - so the honest thing is to size the context to
        /// the work and check that single number.
        /// </summary>
        private static int ChooseContextSize(int nPrompt, int nGenerate)
        {
            int want = nPrompt + nGenerate + 8;           // slack for the template
            if (_contextLength > 0 && want > _contextLength) want = _contextLength;
            if (want < 256) want = 256;

            if (!MemoryGuard || KvBytesPerToken() <= 0) return want;

            long available = AvailablePhysicalBytes();
            if (available <= 0) return want;              // cannot tell - do not block on a guess

            long need = ComputeBufferBytes + KvBytesPerToken() * (long)want
                      + (_model == IntPtr.Zero ? _modelBytes : 0);
            if (need <= available) return want;

            int fits = MemoryCeilingTokens(nGenerate);
            throw new InvalidOperationException(string.Format(
                "This prompt needs a {0:N0}-token context, about {1}; only {2} is free on "
                + "this machine. {3} Send fewer rows, or summarise them before asking.",
                want, Bytes(need), Bytes(available),
                fits > 256
                    ? string.Format("About {0:N0} tokens fit here right now.", fits)
                    : "Not even a short context fits - free some memory, or use a smaller model."));
        }

        private static void EnsureContext(int nCtx)
        {
            lock (Gate)
            {
                if (_ctx != IntPtr.Zero && _ctxSize >= nCtx) return;

                if (_ctx != IntPtr.Zero) { LlamaNative.llama_free(_ctx); _ctx = IntPtr.Zero; }

                var cp = LlamaNative.llama_context_default_params();
                if (!ParamLayout.Verify(ref cp, out var detail))
                    throw new InvalidOperationException(
                        "llama.cpp's context parameters are not laid out as this build of the "
                        + "extension expects (" + detail + "). The bundled llama.cpp is not "
                        + LlamaBuild.PinnedBuild + " - reinstall the extension.");

                ParamLayout.Set(ref cp, ParamLayout.OffNCtx, nCtx);

                // The Box has four cores and no SMT; more threads than cores costs
                // more than it buys on a 10 W part.
                int threads = Math.Max(1, Math.Min(Environment.ProcessorCount, 8));
                ParamLayout.Set(ref cp, ParamLayout.OffNThreads, threads);
                ParamLayout.Set(ref cp, ParamLayout.OffNThreadsBatch, threads);

                _ctx = LlamaNative.llama_init_from_model(_model, cp);
                if (_ctx == IntPtr.Zero)
                    throw new InvalidOperationException(
                        "Could not create a " + nCtx.ToString("N0") + "-token context. There "
                        + "was not enough memory, or the model rejected the size.");
                _ctxSize = nCtx;
            }
        }

        // ------------------------------------------------------------------
        // Generation
        // ------------------------------------------------------------------

        private static void Generate(string prompt, string modelPath, string device,
                                     string systemPrompt, int maxNewTokens, bool thinking,
                                     int maxPromptTokens, double temperature)
        {
            IntPtr pTokens = IntPtr.Zero, pieceBuf = IntPtr.Zero, pOne = IntPtr.Zero;
            IntPtr smpl = IntPtr.Zero;

            try
            {
                if (!string.IsNullOrWhiteSpace(device)
                        && !string.Equals(device, "cpu", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException(
                        "Device '" + device + "' is not available in this build. The published "
                        + "extension is CPU-only; set Device to 'cpu'.");

                EnsureModel(modelPath);
                _status = "thinking";

                var text = BuildPrompt(prompt, systemPrompt, thinking);
                int nPrompt = Tokenize(text, ref pTokens);
                Interlocked.Exchange(ref _promptTokens, nPrompt);

                if (_contextLength > 0 && nPrompt >= _contextLength)
                    throw new InvalidOperationException(string.Format(
                        "Prompt is {0:N0} tokens; this model's context is {1:N0}. Nothing can "
                        + "be generated. Send fewer rows.", nPrompt, _contextLength));

                int budget = _contextLength > 0
                    ? Math.Min(maxNewTokens, _contextLength - nPrompt)
                    : maxNewTokens;

                if (maxPromptTokens > 0 && nPrompt > maxPromptTokens)
                {
                    int room = MemoryCeilingTokens(budget);
                    throw new InvalidOperationException(string.Format(
                        "Prompt is {0:N0} tokens; MaxPromptTokens is {1:N0}.{2} Send fewer rows, "
                        + "summarise them first, or raise MaxPromptTokens.",
                        nPrompt, maxPromptTokens,
                        room > 0
                            ? string.Format(" On this machine about {0:N0} tokens would fit, so "
                                            + "MaxPromptTokens can safely go that high.", room)
                            : ""));
                }

                EnsureContext(ChooseContextSize(nPrompt, budget));
                smpl = BuildSampler(temperature);

                var sw = Stopwatch.StartNew();
                if (LlamaNative.llama_decode(_ctx, LlamaNative.llama_batch_get_one(pTokens, nPrompt)) != 0)
                    throw new InvalidOperationException(
                        "The model could not process the prompt. It may be longer than the "
                        + "context that could be allocated.");

                pieceBuf = Marshal.AllocHGlobal(512);
                pOne = Marshal.AllocHGlobal(sizeof(int));

                var genClock = new Stopwatch();
                bool first = true;
                int n = 0;

                while (!_cancel && n < budget)
                {
                    int tok = LlamaNative.llama_sampler_sample(smpl, _ctx, -1);
                    if (LlamaNative.llama_vocab_is_eog(_vocab, tok)) break;

                    if (first)
                    {
                        _ttftMsF = (float)sw.Elapsed.TotalMilliseconds;
                        _status = "writing";
                        first = false;
                        genClock.Start();
                    }

                    int len = LlamaNative.llama_token_to_piece(_vocab, tok, pieceBuf, 512, 0, true);
                    if (len > 0)
                    {
                        var piece = Utf8.Read(pieceBuf, len);
                        lock (Buffer)
                        {
                            Buffer.Append(piece);
                            _answer = StripThinking(Buffer.ToString());
                        }
                    }

                    n++;
                    Interlocked.Exchange(ref _generated, n);
                    if (genClock.Elapsed.TotalSeconds > 0)
                        _tokensPerSecF = (float)(n / genClock.Elapsed.TotalSeconds);

                    Marshal.WriteInt32(pOne, tok);
                    if (LlamaNative.llama_decode(_ctx, LlamaNative.llama_batch_get_one(pOne, 1)) != 0)
                        break;
                }

                // Resolve what the board will actually show. Up to here _answer has
                // been the streaming view, which deliberately hides an unclosed
                // <think>; now that nothing more is coming, an empty view means the
                // user gets a blank panel after a generation that worked.
                if (!_cancel)
                {
                    lock (Buffer) { _answer = FinalAnswer(Buffer.ToString(), n >= budget, budget); }
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
                if (smpl != IntPtr.Zero) LlamaNative.llama_sampler_free(smpl);
                if (pTokens != IntPtr.Zero) Marshal.FreeHGlobal(pTokens);
                if (pieceBuf != IntPtr.Zero) Marshal.FreeHGlobal(pieceBuf);
                if (pOne != IntPtr.Zero) Marshal.FreeHGlobal(pOne);
                _busy = false;
            }
        }

        /// <summary>
        /// Tokenizes into a buffer the caller owns. A negative return from
        /// llama_tokenize means the buffer was too small and its magnitude is the
        /// size actually needed, so the second attempt always fits.
        /// </summary>
        private static int Tokenize(string text, ref IntPtr pTokens)
        {
            var pText = Utf8.Alloc(text, out var textLen);
            int cap = textLen + 64;
            pTokens = Marshal.AllocHGlobal(cap * sizeof(int));
            int n = LlamaNative.llama_tokenize(_vocab, pText, textLen, pTokens, cap, true, true);
            Marshal.FreeHGlobal(pText);

            if (n < 0)
            {
                cap = -n + 16;
                Marshal.FreeHGlobal(pTokens);
                pTokens = Marshal.AllocHGlobal(cap * sizeof(int));
                pText = Utf8.Alloc(text, out textLen);
                n = LlamaNative.llama_tokenize(_vocab, pText, textLen, pTokens, cap, true, true);
                Marshal.FreeHGlobal(pText);
            }

            if (n <= 0) throw new InvalidOperationException("The prompt could not be tokenized.");
            return n;
        }

        /// <summary>
        /// Temperature 0 is greedy and reproducible; anything above it samples.
        ///
        /// 1.x hard-coded 0.7 with no way to change it, which is why the blank-answer
        /// bug was intermittent and why the same board could give two different
        /// answers to the same data. It is a property now.
        /// </summary>
        private static IntPtr BuildSampler(double temperature)
        {
            var chain = LlamaNative.llama_sampler_chain_init(
                LlamaNative.llama_sampler_chain_default_params());

            if (temperature <= 0.0)
            {
                LlamaNative.llama_sampler_chain_add(chain, LlamaNative.llama_sampler_init_greedy());
            }
            else
            {
                LlamaNative.llama_sampler_chain_add(chain, LlamaNative.llama_sampler_init_top_p(0.9f, 1));
                LlamaNative.llama_sampler_chain_add(chain, LlamaNative.llama_sampler_init_temp((float)temperature));
                LlamaNative.llama_sampler_chain_add(chain,
                    LlamaNative.llama_sampler_init_dist(unchecked((uint)Environment.TickCount)));
            }
            return chain;
        }

        /// <summary>
        /// Applies the model's own chat template, so the prompt is shaped the way the
        /// model was trained to expect.
        /// </summary>
        private static string BuildPrompt(string prompt, string systemPrompt, bool thinking)
        {
            // Reasoning models emit their scratchpad as ordinary tokens. On a slow
            // device that is minutes the user watches happen, so it is off unless
            // asked for. Qwen honours the /no_think marker; other models ignore it
            // harmlessly.
            var user = thinking ? prompt : prompt + " /no_think";
            var plain = string.IsNullOrWhiteSpace(systemPrompt) ? user : systemPrompt + "\n\n" + user;

            var tmpl = LlamaNative.llama_model_chat_template(_model, null);
            if (tmpl == IntPtr.Zero) return plain;

            var pins = new List<IntPtr>();
            var buf = IntPtr.Zero;
            try
            {
                var msgs = new List<LlamaChatMessage>();
                if (!string.IsNullOrWhiteSpace(systemPrompt))
                {
                    var r = Utf8.Alloc("system", out _);
                    var c = Utf8.Alloc(systemPrompt, out _);
                    pins.Add(r); pins.Add(c);
                    msgs.Add(new LlamaChatMessage { Role = r, Content = c });
                }
                var ur = Utf8.Alloc("user", out _);
                var uc = Utf8.Alloc(user, out _);
                pins.Add(ur); pins.Add(uc);
                msgs.Add(new LlamaChatMessage { Role = ur, Content = uc });

                int size = Math.Max(8192, (plain.Length * 4) + 2048);
                buf = Marshal.AllocHGlobal(size);
                int n = LlamaNative.llama_chat_apply_template(
                    tmpl, msgs.ToArray(), (nuint)msgs.Count, true, buf, size);

                return (n > 0 && n <= size) ? Utf8.Read(buf, n) : plain;
            }
            catch (Exception)
            {
                return plain;
            }
            finally
            {
                if (buf != IntPtr.Zero) Marshal.FreeHGlobal(buf);
                foreach (var p in pins) Marshal.FreeHGlobal(p);
            }
        }

        // ------------------------------------------------------------------
        // Answer shaping - unchanged from 1.4, and still needed: llama.cpp emits
        // <think> for a reasoning model exactly as ORT did.
        // ------------------------------------------------------------------

        /// <summary>
        /// What to show once generation has finished.
        ///
        /// Normally the answer with any reasoning block removed. If that leaves
        /// nothing - a reasoning model that never closed its &lt;think&gt;, or spent
        /// the whole token budget inside one - show the reasoning instead, and say
        /// so. It is not the answer that was asked for, but it is what the model
        /// produced, and a blank panel tells the operator nothing at all.
        /// </summary>
        internal static string FinalAnswer(string raw, bool hitTokenLimit, int budget = 0)
        {
            var visible = StripThinking(raw);
            if (!string.IsNullOrWhiteSpace(visible))
            {
                // An answer that stopped because it ran out of budget looks exactly
                // like one that finished, and the reader has no way to tell - the
                // only clue is TokensGenerated equalling MaxNewTokens, which is on a
                // metrics line most boards do not show. Observed on a real board: a
                // three-part analysis stopped one token short of naming its own
                // conclusion, and read as a complete answer.
                //
                // Same failure as the blank panel this method was written for:
                // silence about something the operator needs to know.
                if (hitTokenLimit)
                    return visible.TrimEnd() + "\n\n[cut off at MaxNewTokens"
                         + (budget > 0 ? " = " + budget.ToString("N0") : "")
                         + " - raise it for a longer answer]";

                return visible;
            }

            var inner = raw.Replace("<think>", "").Replace("</think>", "").Trim();
            if (inner.Length == 0) return visible;

            // Cut off mid-thought: the budget really is the problem, and the text is
            // a fragment rather than an answer.
            if (hitTokenLimit)
                return "The model ran out of tokens before finishing - raise "
                     + "MaxNewTokens to give it room. What it had written:\n\n" + inner;

            // Finished on its own with the tag left open. The text is the answer; the
            // model was simply sloppy about closing a marker, and saying anything
            // about token limits here would be wrong and would send the reader off to
            // change a setting that is not involved.
            return inner;
        }

        /// <summary>
        /// Removes a reasoning block from the visible answer.
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
        // Free physical memory
        // ------------------------------------------------------------------

        private static string Bytes(long b)
        {
            const double G = 1024.0 * 1024.0 * 1024.0;
            double g = b / G;
            if (g >= 10) return g.ToString("N0") + " GB";
            if (g >= 1) return g.ToString("N1") + " GB";
            return (b / (1024.0 * 1024.0)).ToString("N0") + " MB";
        }

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
    }
}
