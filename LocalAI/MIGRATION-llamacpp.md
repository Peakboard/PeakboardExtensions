# Migrating LocalAI from ONNX Runtime GenAI to llama.cpp

Status: **decided, not built.** Everything below the "Decisions" heading is backed
by measurements on PB10424 on 2026-09-21; the code does not exist yet.

Full numbers: `internal/LocalAI/bench/RESULTS.md`, sections "llama.cpp on the Box"
and "The load-path spike".

## Why

Not speed alone. Three things, in the order they matter.

**1. Models people can actually get.** Today a Box owner cannot download a model.
`onnx-community` publishes ONNX GenAI builds as int4 only, and int4 is the one
precision a Box cannot use *under ONNX Runtime* — so the README has to walk them
through installing Python and running `models.builder` to convert an fp16 build
themselves. Under llama.cpp, Q4 on a Box is the **fastest** configuration measured.
The Box story collapses from "install a toolchain and convert" to "download one
file", against an ecosystem that publishes every model at every quant.

This is also why the question came up at all: a reviewer went to get a model and
what the ecosystem handed him was a GGUF.

**2. Generation is 2-3x faster and the model costs 9x less memory.**

| Qwen3-0.6B on PB10424 | prefill | generate | model in RAM |
|---|---|---|---|
| ORT CPU int4 | 15.1 | 0.3 | 1589 MB peak |
| ORT CPU fp16 | 16.4 | 3.6 | 3259 MB peak |
| llama.cpp Q4_K_M | 17.3 | **7.0 - 10.0** | **373 MiB** |

Prefill is a wash. Generation is not. Use 7 tok/s as the floor — the spread is
between runs on a 10 W part with no turbo, not within them.

The memory number may matter more than the throughput: the whole prompt-ceiling
apparatus exists because ORT leaves ~780 MB free on a 7.82 GB Box after loading.

**3. One engine, not two.** Supporting both doubles the surface — two memory
models, two error vocabularies, two format checks, two sets of bundled natives in
a package that would go from 8.5 MB to roughly 25 MB. What that buys is
compatibility with existing ONNX GenAI users, and essentially the only ones are us.

## What this does NOT buy

**A 4B model on a Box.** Measured: 2.20 tok/s prefill, 1.62 generate. A 300-token
prompt spends about two and a half minutes before the first token. 0.6B remains the
Box-sized model. This was the hypothesis that prompted the whole investigation and
it is false.

## Decisions

**Raw P/Invoke over the stock llama.cpp release, not LLamaSharp.** We already
bundle and version native DLLs (the C++ runtime work in 1.3), the API surface we
need is small, and a third-party binding brings its own native packaging that would
have to be made to agree with the extension layout. Proven working: a P/Invoke
probe loaded `ggml-base.dll`, `ggml.dll` and `llama.dll` by absolute path from a
directory other than the host executable's and enumerated the CPU backend.

**`ggml_backend_load_all_from_path()` must be called before anything else.**
llama.cpp resolves its fourteen `ggml-cpu-*.dll` variants relative to the
*executable*, which for an extension is `Peakboard.ExtensionKit.Process64.exe` in
the Peakboard install — not our folder. Without the explicit call the backend
registry is **empty** and no inference is possible, silently. Measured: 0 devices
separated, 1 device (`ggml-cpu-sse42.dll`, correct for the N5105) with the call.
`_platform/ERRATA.md` entry 131.

**Keep bundling the four CRT DLLs.** llama.cpp imports `msvcp140`, `vcruntime140`
and `vcruntime140_1` — the same dependency ORT has, and the same `0x8007007E` on a
stock Box. The 1.3 mechanism covers it unchanged.

## Work

| | |
|---|---|
| P/Invoke layer | model load, context, vocab, tokenize, chat template, decode loop, sampler, detokenize. The fiddly part is the by-value params structs (`llama_model_params`, `llama_context_params`) — get the layouts from `llama.h` of the pinned build, do not guess. |
| Pin a build | the release is `b<number>`; pin one and record it, as ORT GenAI's version is recorded now. |
| `LlmEngine` | the generation path is ~6 call sites. Streaming, cancellation, status and `FinalAnswer` all carry over unchanged. |
| Memory guard | **the real work.** `PredictPeakBytes` reads `n_head`, `n_layer`, `head_size` and context length from `genai_config.json`. GGUF carries the equivalent in its header (`llama_model_n_head` etc. are exported), so the quadratic law still applies — but the constants and the ~1,200-token Box ceiling must be **re-measured**, not ported. The 9x smaller model almost certainly moves the ceiling up; by how much is unknown. |
| `CheckModelFolder` | inverts. A GGUF file becomes the valid case; a `genai_config.json` folder becomes the one to explain. Keep the same "name what you actually found" behaviour (ERRATA 130). |
| `ModelPath` | now points at a **file**, not a folder. Breaking change for every existing board — needs a major version and a line in the README. |
| `Device` | `cpu` only, as today. llama.cpp has Vulkan and SYCL builds; out of scope, and DirectML's correctness failure is a reason for caution, not enthusiasm. |

## Still unproven

The probe did loading and backend enumeration only. Not yet shown end to end:
tokenization, the chat template, the streaming decode loop, and the same reasoning
model's `<think>` handling — which llama.cpp will produce too, so `FinalAnswer`
from 1.4 is needed regardless.

Do that as a standalone harness against the Box before touching `LlmEngine`.
