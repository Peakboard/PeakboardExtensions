// P/Invoke surface for llama.cpp, written against the header of the pinned build.
// See LlamaBuild.PinnedBuild for which one.
//
// Why hand-written rather than a binding package: the native DLLs have to sit in
// the extension folder and be loaded from there (see LlamaEngine.LoadNatives), the
// surface we need is about twenty functions, and a binding brings its own native
// packaging that would have to be made to agree with the extension layout anyway.
//
// The awkward part is that llama.cpp passes its two big parameter structs BY VALUE,
// and those structs churn between builds - llama_context_params gained n_rs_seq,
// n_outputs_max_per_seq, ctx_type, samplers and ctx_other recently. Mirroring them
// field by field would break silently on an upgrade, because a wrong offset does
// not fault, it feeds the library nonsense.
//
// So they are OPAQUE BLOBS: llama_*_default_params() fills one, only the few
// leading fields we need are patched at offsets from the header, and the blob goes
// straight back. On Win x64 a struct this size travels by hidden pointer in both
// directions, so an over-sized declaration is safe - the callee touches only the
// bytes it knows about. ParamLayout.Verify() then asserts the defaults read back as
// expected, so a layout change fails loudly at load rather than quietly at
// inference.
using System;
using System.Runtime.InteropServices;
using System.Text;

namespace LocalAI
{
    internal static class LlamaBuild
    {
        /// <summary>
        /// The llama.cpp release these declarations were written against. The
        /// parameter struct offsets below are only guaranteed for this build; if the
        /// bundled DLLs are updated, re-check them against that tag's llama.h and
        /// let ParamLayout.Verify() confirm it at run time.
        /// </summary>
        public const string PinnedBuild = "b11070";
    }

    [StructLayout(LayoutKind.Sequential, Size = 512)]
    internal struct LlamaModelParams { }

    [StructLayout(LayoutKind.Sequential, Size = 512)]
    internal struct LlamaContextParams { }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LlamaSamplerChainParams
    {
        [MarshalAs(UnmanagedType.I1)] public bool NoPerf;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LlamaBatch
    {
        public int NTokens;
        public IntPtr Token;
        public IntPtr Embd;
        public IntPtr Pos;
        public IntPtr NSeqId;
        public IntPtr SeqId;
        public IntPtr Logits;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct LlamaChatMessage
    {
        public IntPtr Role;
        public IntPtr Content;
    }

    internal static class LlamaNative
    {
        const string GGML = "ggml.dll";
        const string LLAMA = "llama.dll";
        const CallingConvention CC = CallingConvention.Cdecl;

        // Mandatory before anything else. llama.cpp looks for its dispatched CPU
        // kernels beside the EXECUTABLE - which for an extension is the Peakboard
        // extension host, not our folder - so without this the backend registry is
        // empty and no inference is possible, silently. _platform ERRATA 131.
        [DllImport(GGML, CallingConvention = CC, CharSet = CharSet.Ansi)]
        internal static extern void ggml_backend_load_all_from_path(string dir);

        [DllImport(GGML, CallingConvention = CC)]
        internal static extern nuint ggml_backend_dev_count();

        [DllImport(LLAMA, CallingConvention = CC)]
        internal static extern void llama_backend_init();

        [DllImport(LLAMA, CallingConvention = CC)]
        internal static extern void llama_backend_free();

        [DllImport(LLAMA, CallingConvention = CC)]
        internal static extern LlamaModelParams llama_model_default_params();

        [DllImport(LLAMA, CallingConvention = CC)]
        internal static extern LlamaContextParams llama_context_default_params();

        [DllImport(LLAMA, CallingConvention = CC)]
        internal static extern LlamaSamplerChainParams llama_sampler_chain_default_params();

        [DllImport(LLAMA, CallingConvention = CC, CharSet = CharSet.Ansi)]
        internal static extern IntPtr llama_model_load_from_file(string path, LlamaModelParams p);

        [DllImport(LLAMA, CallingConvention = CC)]
        internal static extern void llama_model_free(IntPtr model);

        [DllImport(LLAMA, CallingConvention = CC)]
        internal static extern IntPtr llama_init_from_model(IntPtr model, LlamaContextParams p);

        [DllImport(LLAMA, CallingConvention = CC)]
        internal static extern void llama_free(IntPtr ctx);

        [DllImport(LLAMA, CallingConvention = CC)]
        internal static extern IntPtr llama_model_get_vocab(IntPtr model);

        // Everything the memory guard needs, straight out of the GGUF.
        [DllImport(LLAMA, CallingConvention = CC)]
        internal static extern int llama_model_n_ctx_train(IntPtr model);

        [DllImport(LLAMA, CallingConvention = CC)]
        internal static extern int llama_model_n_head(IntPtr model);

        [DllImport(LLAMA, CallingConvention = CC)]
        internal static extern int llama_model_n_head_kv(IntPtr model);

        [DllImport(LLAMA, CallingConvention = CC)]
        internal static extern int llama_model_n_layer(IntPtr model);

        [DllImport(LLAMA, CallingConvention = CC)]
        internal static extern int llama_model_n_embd(IntPtr model);

        [DllImport(LLAMA, CallingConvention = CC)]
        internal static extern ulong llama_model_size(IntPtr model);

        [DllImport(LLAMA, CallingConvention = CC, CharSet = CharSet.Ansi)]
        internal static extern int llama_model_meta_val_str(
            IntPtr model, string key, IntPtr buf, nuint bufSize);

        [DllImport(LLAMA, CallingConvention = CC, CharSet = CharSet.Ansi)]
        internal static extern IntPtr llama_model_chat_template(IntPtr model, string name);

        [DllImport(LLAMA, CallingConvention = CC)]
        internal static extern int llama_chat_apply_template(
            IntPtr tmpl, [In] LlamaChatMessage[] chat, nuint nMsg,
            [MarshalAs(UnmanagedType.I1)] bool addAss, IntPtr buf, int length);

        [DllImport(LLAMA, CallingConvention = CC)]
        internal static extern int llama_tokenize(
            IntPtr vocab, IntPtr text, int textLen, IntPtr tokens, int nTokensMax,
            [MarshalAs(UnmanagedType.I1)] bool addSpecial,
            [MarshalAs(UnmanagedType.I1)] bool parseSpecial);

        [DllImport(LLAMA, CallingConvention = CC)]
        internal static extern int llama_token_to_piece(
            IntPtr vocab, int token, IntPtr buf, int length, int lstrip,
            [MarshalAs(UnmanagedType.I1)] bool special);

        [DllImport(LLAMA, CallingConvention = CC)]
        [return: MarshalAs(UnmanagedType.I1)]
        internal static extern bool llama_vocab_is_eog(IntPtr vocab, int token);

        [DllImport(LLAMA, CallingConvention = CC)]
        internal static extern LlamaBatch llama_batch_get_one(IntPtr tokens, int nTokens);

        [DllImport(LLAMA, CallingConvention = CC)]
        internal static extern int llama_decode(IntPtr ctx, LlamaBatch batch);

        [DllImport(LLAMA, CallingConvention = CC)]
        internal static extern IntPtr llama_sampler_chain_init(LlamaSamplerChainParams p);

        [DllImport(LLAMA, CallingConvention = CC)]
        internal static extern void llama_sampler_chain_add(IntPtr chain, IntPtr smpl);

        [DllImport(LLAMA, CallingConvention = CC)]
        internal static extern IntPtr llama_sampler_init_greedy();

        [DllImport(LLAMA, CallingConvention = CC)]
        internal static extern IntPtr llama_sampler_init_temp(float t);

        [DllImport(LLAMA, CallingConvention = CC)]
        internal static extern IntPtr llama_sampler_init_top_p(float p, nuint minKeep);

        [DllImport(LLAMA, CallingConvention = CC)]
        internal static extern IntPtr llama_sampler_init_dist(uint seed);

        [DllImport(LLAMA, CallingConvention = CC)]
        internal static extern int llama_sampler_sample(IntPtr smpl, IntPtr ctx, int idx);

        [DllImport(LLAMA, CallingConvention = CC)]
        internal static extern void llama_sampler_free(IntPtr smpl);
    }

    /// <summary>
    /// The only fields poked into the opaque LlamaContextParams blob, with their
    /// offsets from the pinned build's llama.h, plus a check that the library really
    /// lays them out that way.
    /// </summary>
    internal static class ParamLayout
    {
        // uint32 n_ctx, n_batch, n_ubatch, n_seq_max, n_rs_seq, n_outputs_max,
        // n_outputs_max_per_seq; then int32 n_threads, n_threads_batch.
        public const int OffNCtx = 0;
        public const int OffNBatch = 4;
        public const int OffNThreads = 28;
        public const int OffNThreadsBatch = 32;

        public static unsafe void Set(ref LlamaContextParams p, int off, int value)
        {
            fixed (LlamaContextParams* q = &p) *(int*)((byte*)q + off) = value;
        }

        public static unsafe int Get(ref LlamaContextParams p, int off)
        {
            fixed (LlamaContextParams* q = &p) return *(int*)((byte*)q + off);
        }

        /// <summary>
        /// llama.cpp's documented defaults are n_ctx 512 and n_batch 2048. Reading
        /// those back at the offsets above means the leading layout is what we think
        /// it is. If a field is ever inserted at the front, this fails here rather
        /// than handing the library a garbage thread count and a context size taken
        /// from the middle of another number.
        /// </summary>
        public static bool Verify(ref LlamaContextParams p, out string detail)
        {
            var nCtx = Get(ref p, OffNCtx);
            var nBatch = Get(ref p, OffNBatch);
            var nThreads = Get(ref p, OffNThreads);
            var ok = nCtx == 512 && nBatch == 2048 && nThreads > 0 && nThreads <= 4096;
            detail = string.Format("n_ctx={0} n_batch={1} n_threads={2}", nCtx, nBatch, nThreads);
            return ok;
        }
    }

    internal static class Utf8
    {
        public static IntPtr Alloc(string s, out int byteLen)
        {
            var b = Encoding.UTF8.GetBytes(s ?? "");
            byteLen = b.Length;
            var p = Marshal.AllocHGlobal(b.Length + 1);
            Marshal.Copy(b, 0, p, b.Length);
            Marshal.WriteByte(p, b.Length, 0);
            return p;
        }

        public static string Read(IntPtr p, int len)
        {
            if (p == IntPtr.Zero || len <= 0) return string.Empty;
            var b = new byte[len];
            Marshal.Copy(p, b, 0, len);
            return Encoding.UTF8.GetString(b);
        }
    }
}
