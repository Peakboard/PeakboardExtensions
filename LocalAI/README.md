# Peakboard Extension: Local AI

Runs a small language model **on the device itself** — a Peakboard Box, or the PC
running Peakboard Designer. No cloud service, no API key, no network call at
inference time. Nothing you send it leaves the machine.

This is the offline counterpart to the [GPT](../GPT/) extension. GPT is far more
capable and needs an OpenAI account; this one is weaker and needs neither.

## What it is good for

Short, structured work: classify a fault code, pull fields out of a machine
message, rewrite a status into a fixed sentence, summarise a handful of readings.
Give it a shop-floor prompt and a small model answers in a few seconds.

**It is not a chatbot replacement.** Read [Performance](#performance) before
promising anyone a conversation — on Peakboard Box hardware a two-sentence answer
takes about ten seconds, and a long prompt takes considerably longer.

## Custom List: Local AI

One row, carrying the current answer and how the generation is going.

### Properties

| Property | Type | Default | Description |
|---|---|---|---|
| `ModelPath` | text | `C:\LocalAI\models\Qwen3-0.6B-Q4_K_M.gguf` | The **.gguf model file**. Changed in 2.0 - 1.x wanted a folder. |
| `Device` | list | `cpu` | `cpu` only. llama.cpp publishes GPU builds, but no GPU path has been shown correct on a Box. See [Why CPU only](#why-cpu-only). |
| `SystemPrompt` | text, multi-line | `You are a helpful assistant. Answer briefly.` | Standing instruction sent before every question. |
| `MaxNewTokens` | number | `128` | Upper bound on answer length. Every token costs time. Trimmed automatically if the prompt leaves less room than this in the context window. |
| `MaxPromptTokens` | number | `4096` | Refuses a prompt longer than this. `0` disables it — but not the memory check below it, which cannot be turned off from a board. See [Prompt length](#prompt-length-is-the-real-limit). |
| `Thinking` | checkbox | `false` | `true` lets a reasoning model think out loud. Slow — see below. |
| `Temperature` | number | `0.7` | 0 is greedy - same prompt, same answer, every time, which is usually what a dashboard wants. New in 2.0; 1.x hard-coded 0.7. |

### Columns

| Column | Type | Description |
|---|---|---|
| `Answer` | String | The answer so far. Grows while the model writes. |
| `Status` | String | `idle` / `loading model` / `ready` / `thinking` / `writing` / `done` / `cancelled` / `error` |
| `Busy` | Boolean | True while generating. |
| `PromptTokens` | Number | Size of the prompt actually sent. |
| `TokensGenerated` | Number | Tokens produced so far. |
| `TokensPerSecond` | Number | Current writing speed. |
| `TimeToFirstTokenMs` | Number | How long the model spent reading the prompt. |
| `Error` | String | Empty unless something failed. |

### Functions

| Function | Parameters | Returns | Description |
|---|---|---|---|
| `Ask` | `Prompt` (String) | `Started` (String) | Sends a prompt. Returns immediately — poll the list for the answer. |
| `Cancel` | — | — | Stops the generation in progress. |
| `Reset` | — | — | Clears the current answer. |

`Ask` returns **`"started"`**, or the reason nothing was started: **`"busy"`** if a
generation is already running (the new prompt was dropped — this call does not
queue), or **`"empty prompt"`**. It cannot report whether the answer was any good,
because it returns before the model has read the prompt; everything after that
point arrives through the `Status` and `Error` columns.

Ignoring the return is fine for a single Ask button. It is worth checking if
several things can trigger a question:

```lua
local started = data.LocalAI.Ask(screens['Screen1'].PromptInput.text)
if started ~= 'started' then
   screens['Screen1'].StatusText.text = 'Not sent: ' .. started
end
```

`Cancel` and `Reset` return nothing. They always succeed, so a return value
would have carried no information — watch `Status` instead.

## Getting a model

**The extension ships without one.** Models are 0.4-3 GB; they do not belong in a
git repository, and which one you want depends on your hardware. You point
`ModelPath` at a file you download once.

Since 2.0 the model is a **GGUF file** - the format llama.cpp reads, and the one
the model ecosystem actually publishes. `ModelPath` is that file, not a folder:

```
C:\LocalAI\models\Qwen3-0.6B-Q4_K_M.gguf
```

No conversion, no Python, no toolchain. Download one file from Hugging Face and
point at it. If you get this wrong the data source tells you what it found instead
- a folder, an ONNX model, raw `.safetensors`, or a `.gguf` sitting one level down.

### Which model

Pick on **speed**, because on a CPU that is what constrains you, and a Box is slow.
All measured on a Peakboard Box (Celeron N5105, 4 cores, no AVX):

| model | file | prefill | generate | verdict on a Box |
|---|---|---|---|---|
| **Qwen3-0.6B Q4_K_M** | 373 MB | 16.5 tok/s | 7-10 tok/s | **use this one** |
| Qwen3-1.7B Q4_K_M | 1.0 GB | 5.7 tok/s | 3.9 tok/s | better answers, ~1 min per question |
| Qwen3-4B Q4_K_M | 2.4 GB | 2.2 tok/s | 1.6 tok/s | too slow - PC only |

On a PC the picture changes completely: the same 4B file does **43 tok/s** prefill
on a Ryzen 7 PRO 7840U. Bigger models are fine there.

Quantisation: **Q4_K_M** unless you have a reason. Q8_0 is about twice the size for
a difference you will struggle to see on a 0.6B.

### Where to get one

Straight from Hugging Face, one file:

- **Qwen3-0.6B** - <https://huggingface.co/unsloth/Qwen3-0.6B-GGUF> (`Qwen3-0.6B-Q4_K_M.gguf`)
- **Qwen3-1.7B** - <https://huggingface.co/unsloth/Qwen3-1.7B-GGUF>
- **Qwen3-4B** - <https://huggingface.co/Qwen/Qwen3-4B-GGUF>

Or with the CLI:

```bash
hf download unsloth/Qwen3-0.6B-GGUF Qwen3-0.6B-Q4_K_M.gguf --local-dir C:\LocalAI\models
```

Any GGUF llama.cpp can read will work - Qwen, Llama, Phi, Gemma, Mistral. Check the
model's own licence before shipping it; they differ.

**Deploying to a Box.** The model is not in the `.pbmx`, so copy the file to the
device once and leave it there - any path works as long as `ModelPath` matches:

```powershell
Copy-Item .\Qwen3-0.6B-Q4_K_M.gguf \<box>\c$\LocalAI\models```

A board then deploys in seconds rather than carrying a gigabyte each time.

## Showing the answer as it is written

`Ask` returns immediately and the model writes in the background, so the answer
arrives a word at a time. Set the data source refresh to **1 second** and copy the
column onto a label:

```lua
-- on the data source's Refreshed event
screens['Screen1'].AnswerText.text = data.LocalAI[0].Answer
screens['Screen1'].StatusText.text = data.LocalAI[0].Status
```

Each refresh returns a slightly longer string, so the text types itself. This
matters more than it sounds: at 5 tokens a second, streaming reads as someone
typing, while waiting for the finished answer reads as a frozen screen.

To ask a question from a button:

```lua
data.LocalAI.Ask(screens['Screen1'].PromptInput.text)
```

To send board data instead of typed text, build the prompt from your own lists —
this is the same call, and the reason the extension is useful on a dashboard:

```lua
local p = 'Machine readings:'
for i = 0, data.DS_Readings.count - 1 do
   p = table.concat({p, ' ', data.DS_Readings[i].Line,
                     ' status ', data.DS_Readings[i].Status,
                     ', temperature ', string.tostring(data.DS_Readings[i].TempC), ' C.'})
end
data.LocalAI.Ask(table.concat({p, ' Which line needs attention first?'}))
```

## Prompt length is the real limit - but the limit is now TIME, not memory

Up to 1.4 this section warned that ONNX Runtime's attention buffer grew with the
*square* of the prompt, so a prompt well inside the model's context could ask for
tens of gigabytes and kill the runtime. **That was a property of ONNX Runtime, not
of the hardware.** llama.cpp uses flash attention and never builds that matrix, so
2.0's memory is linear in context: about 112 KiB per token for Qwen3-0.6B, with a
compute buffer that stays flat from 2k to 16k.

Measured on a Box:

| context | KV cache | compute buffer |
|---|---|---|
| 2,048 | 224 MiB | 301 MiB |
| 8,192 | 896 MiB | 301 MiB |
| 16,384 | 1,792 MiB | 321 MiB |

So memory has stopped being the wall. **Time took its place**, and prefill runs at
a flat ~16.5 tok/s on a Box regardless of prompt length:

| prompt | first token | plus an 80-token answer |
|---|---|---|
| 150 | 9 s | ~19 s |
| 300 | 18 s | ~28 s |
| 500 | 30 s | ~40 s |
| 1,000 | 61 s | ~71 s |
| 2,000 | 121 s | ~131 s |

**A Box is comfortable to roughly 300-500 prompt tokens.** Past about 1,000 it
stops feeling like pressing a button. On a PC, multiply by three or more.

### What the extension does about it

`MaxPromptTokens` is a policy cap you set. The engine additionally sizes the
context to the actual work and refuses when that will not fit in free memory,
naming what would:

> This prompt needs a 12,400-token context, about 1.8 GB; only 900 MB is free on
> this machine. About 6,200 tokens fit here right now. Send fewer rows, or
> summarise them before asking.

That refusal is now rare, because memory is rarely the binding constraint. Treat
`MaxPromptTokens` as your patience budget rather than a safety device - roughly
1,000 on a Box, more on a PC.

### Give the model facts, not rows

The sharpest limitation is not speed, and it is worth knowing before you design a
board. **A small model reads a prepared summary well and searches raw data badly.**

Measured on a Box, same model, same day: given three stations summarised into three
lines with one flagged in alarm, Qwen3-0.6B correctly named the faulted station and
what to check. Given twenty rows of raw sensor readings with one anomalous row, it
named a timestamp that **did not exist in the data**, quoted ordinary values as the
deviations, and still concluded confidently.

So reduce first - minimum, maximum, trend, delta, which one is in alarm - in Lua,
where a `for` loop gets it right every time, and hand the model a dozen prepared
lines. That is cheaper in tokens, faster, and far more reliable than hoping it
finds the needle. A 4B on a PC *can* do the search; a Box-sized model cannot.

## Performance

Measured on a **Peakboard Box** (Intel Celeron N5105, 4 cores, 8 GB RAM) with
Qwen3-0.6B Q4_K_M under 2.0:

| | |
|---|---|
| Model load (once, on first Ask) | ~0.7 s |
| Model resident | **373 MiB** |
| Prefill | **16.5 tokens/s**, flat from 128 to 2,048 tokens |
| Generation | **7-13 tokens/s** depending on context |
| 300-token prompt -> 40-token answer | ~20 s end to end |

For comparison, 1.4 on the same Box with the same model at fp16 managed **5.8
tokens/s** generating and peaked near **3.3 GB**. 2.0 is roughly twice as fast and
uses a ninth of the memory, because llama.cpp's 4-bit kernels have an SSE path and
ONNX Runtime's do not.

On a PC the same engine with Qwen3-4B Q4_K_M does **43 tok/s** prefill and ~12
tok/s generating (Ryzen 7 PRO 7840U). The 2x advantage is a **weak-CPU** result: a
machine with AVX-512 takes ONNX Runtime's fast path too, and the two come out
level.

Three things follow, and they are worth knowing before designing a board:

- **Keep prompts short.** Reading the prompt dominates, and it is linear: every 16
  tokens costs about a second on a Box. 300-500 tokens is comfortable; 2,000 is two
  minutes.
- **Keep the model loaded.** The first `Ask` pays the load; later ones do not.
- **Summarise before asking.** Not only for speed - a Box-sized model reads
  prepared facts reliably and searches raw rows badly. See
  [Give the model facts, not rows](#give-the-model-facts-not-rows).

Memory is no longer the constraint it was: a 0.6B leaves several gigabytes spare on
a Box, and a 1.7B fits comfortably if you can wait about a minute per question.

### Reasoning models

Models like Qwen3 reason out loud by default, and that reasoning is generated
token by token like everything else. On slow hardware a short answer can hide a
300-token internal monologue, turning ten seconds into two minutes. `Thinking` is
therefore `false` by default; the extension appends `/no_think` and strips any
`<think>` block from `Answer`.

## Why CPU only

llama.cpp publishes Vulkan, SYCL, CUDA and ROCm builds, all MIT, and any could be
bundled. None is, for a measured reason rather than a licensing one.

**GPU on a Peakboard Box has been shown to return wrong answers.** Under the ONNX
Runtime + DirectML stack that 1.x used, the Box's Gen11 integrated GPU produced a
stream of nonsense tokens for the same prompt that gave *"The sky on a clear day is
blue."* on CPU - a [known ONNX Runtime
issue](https://github.com/microsoft/onnxruntime/issues/19837) in the DirectML/Intel
metacommand path. That specific bug does not apply to llama.cpp's Vulkan backend,
but nothing has demonstrated that path is correct on this hardware either, and a
GPU backend that is fast and wrong is worse than a CPU one that is slow and right.

Measure correctness before speed. The first benchmark round on this project did the
opposite and recommended a configuration that returned garbage.

If you want to try a GPU build, swap the DLLs in `llamacpp/` for those from a
Vulkan or SYCL release of the same llama.cpp build and check the answers against
CPU before trusting anything.

## Building from source

```bash
cd SourceCodeNew/LocalAI
dotnet build -c Release

powershell -ExecutionPolicy Bypass -File ../../../tools/pack-extension.ps1 `
    -BinDir      "...\SourceCodeNew\LocalAI\bin\Release\net8.0\win-x64" `
    -Destination "...\LocalAI\Binary\LocalAI.zip"
```

## Installation

1. Download `LocalAI.zip` from the `Binary` folder.
2. In Peakboard Designer: **Data > Add data source > Manage extensions**.
3. **Add custom extension**, select the ZIP, then restart Designer.
4. Create a model folder (see [Getting a model](#getting-a-model)).
5. Add the **Local AI** data source and set `ModelPath`.

The model folder must exist on whichever machine runs the board — the Designer PC
for a preview, the Box for a deployed dashboard. It is not carried inside the
`.pbmx`.

Nothing else has to be installed. In particular you do **not** need to install a
Visual C++ redistributable — see below.

## Why the package contains Microsoft C++ DLLs

`msvcp140.dll`, `msvcp140_1.dll`, `vcruntime140.dll` and `vcruntime140_1.dll` ship
inside the ZIP, and they are there on purpose.

ONNX Runtime is C++ and links to the Microsoft Visual C++ runtime dynamically. That
runtime is normally installed once per machine — and **a Peakboard Box does not have
it.** A developer PC almost always does, because Visual Studio and many ordinary
applications install it, which is exactly why this went unnoticed through two
releases: it worked for everyone who tested it and failed on the appliance.

Without these files a Box fails on the first `Ask` with

```
DllNotFoundException: Unable to load DLL '...\Extensions\LocalAI\llama.dll'
or one of its dependencies: The specified module could not be found. (0x8007007E)
```

and on a machine with an out-of-date runtime with `0x8007045A`, *"DLL initialization
routine failed"* — neither of which names the cause.

A copy of the runtime beside the native DLLs takes precedence over the machine's own,
so shipping a current one fixes both cases and asks nothing of the user. It costs
about 900 KB. These four files are the only dependencies that do not ship with
Windows.

**If you rebuild this extension, keep them.** Refresh them from a current Visual C++
redistributable rather than deleting them; the runtime is backward compatible, so a
newer copy is safe and an older one is not.

## Licences

Everything in the shipped package is **MIT** (llama.cpp and ggml), apart from
libomp.dll, which is Apache 2.0 with the LLVM exception. See `NOTICE.txt` in the
ZIP.

The **model is licensed separately and is not shipped here.** Qwen3-0.6B is Apache
2.0, which permits commercial use but requires the licence text and a statement of
modifications to travel with the weights — quantising to GGUF counts as a
modification. `NOTICE.txt` and `Qwen3-0.6B-LICENSE.txt` are included as a starting
point; adjust them if you ship a different model.
