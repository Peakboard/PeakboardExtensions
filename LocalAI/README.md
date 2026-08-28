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

| Property | Default | Description |
|---|---|---|
| `ModelPath` | `C:\LocalAI\models\qwen3-0.6b` | Folder holding an ONNX Runtime GenAI model. Must contain `genai_config.json`. |
| `Device` | `cpu` | The published build is CPU-only. See [DirectML](#why-cpu-only). |
| `SystemPrompt` | `You are a helpful assistant. Answer briefly.` | Standing instruction sent before every question. |
| `MaxNewTokens` | `128` | Upper bound on answer length. Every token costs time. |
| `Thinking` | `false` | `true` lets a reasoning model think out loud. Slow — see below. |

### Columns

| Column | Type | Description |
|---|---|---|
| `Answer` | String | The answer so far. Grows while the model writes. |
| `Status` | String | `idle` / `loading model` / `thinking` / `writing` / `done` / `error` |
| `Busy` | Boolean | True while generating. |
| `PromptTokens` | Number | Size of the prompt actually sent. |
| `TokensGenerated` | Number | Tokens produced so far. |
| `TokensPerSecond` | Number | Current writing speed. |
| `TimeToFirstTokenMs` | Number | How long the model spent reading the prompt. |
| `Error` | String | Empty unless something failed. |

### Functions

| Function | Parameters | Description |
|---|---|---|
| `Ask` | `Prompt` (String) | Sends a prompt. Returns immediately — poll the list for the answer. |
| `Cancel` | — | Stops the generation in progress. |
| `Reset` | — | Clears the current answer. |

## Getting a model

**The extension ships without one.** Models are 0.5–3 GB; they do not belong in a
git repository, and which one you want depends on your hardware. You point
`ModelPath` at a folder you create once.

The model must be in **ONNX Runtime GenAI** format — a folder containing
`genai_config.json`, `model.onnx`, `model.onnx.data` and a tokenizer. Raw
`.safetensors` from Hugging Face will not work; they have to be converted.

To build one (Python 3.10+, roughly 10 minutes and a few GB of disk):

```bash
pip install onnxruntime-genai transformers torch
```

**Which model and which precision depends on the CPU**, and the answer is close to
opposite at the two ends. Pick your row:

| Hardware | Model | Precision | On disk |
|---|---|---|---|
| Peakboard Box (Celeron N5105, no AVX) | Qwen3-0.6B | **fp16** | ~1.2 GB |
| PC or laptop, 16 GB RAM | Qwen3-4B | **int4** | ~2.5 GB |
| PC with 8 GB RAM, or you want snappier answers | Qwen3-1.7B | **int4** | ~1.1 GB |

```bash
# Peakboard Box - fp16. int4 is ~12x SLOWER here; see Performance.
python -m onnxruntime_genai.models.builder \
    -m Qwen/Qwen3-0.6B -o C:\LocalAI\models\qwen3-0.6b -p fp16 -e cpu

# PC - int4. Smaller, faster, and lets a far better model fit.
python -m onnxruntime_genai.models.builder \
    -m Qwen/Qwen3-4B   -o C:\LocalAI\models\qwen3-4b   -p int4 -e cpu
```

Then point `ModelPath` at the folder you built.

**Why the reversal.** int4 matrix multiply (`MatMulNBits`) has hand-written AVX2 and
AVX-512 kernels in ONNX Runtime, and no SSE-only one. Every x86 CPU since roughly
2013 has AVX2, so on a PC int4 is the fast path — a quarter of the memory traffic
for nearly the same answers. The Box's Celeron is one of the few current CPUs
*without* AVX, so it falls back to a reference implementation and int4 collapses.
Do not carry the Box's fp16 recommendation onto a PC: you would be leaving a much
better model on the table for no reason.

Any model the ONNX Runtime GenAI builder supports will work — Qwen, Llama, Phi,
Gemma, Mistral. Check the model's own licence before shipping it in a product:
Qwen3 is Apache 2.0, but Gemma and Llama have their own terms.

> If `transformers` asks for a Hugging Face token on a public model, download the
> weights first with `huggingface_hub.snapshot_download(..., token=False)` and
> point the builder at the local folder with `-i`.

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

## Performance

Measured on a **Peakboard Box** (Intel Celeron N5105, 8 GB RAM) with
Qwen3-0.6B at fp16:

| | |
|---|---|
| Model load (once, on first Ask) | 8–18 s |
| Short prompt (~50 tokens), warm | first token after **2.4 s**, then **5.4 tokens/s** |
| Long prompt (~500 tokens) | first token after **~31 s**, then ~3.6 tokens/s |
| Peak memory | ~4.6 GB |

Three things follow from this, and they are worth knowing before you design a
board around it:

- **Keep prompts short.** Reading the prompt dominates. A 500-token prompt costs
  half a minute before the first word appears.
- **Keep the model loaded.** The first `Ask` pays the load cost; later ones do not.
- **Memory is tight on a Box.** A 0.6B model at fp16 peaks near 4.6 GB against
  roughly 4.6 GB free. A larger model will not fit alongside a real dashboard.

Anything bigger than about 1B parameters is not usable on Box hardware. On a
normal PC with more memory and a faster CPU, larger models are fine.

**Avoid int4 on a Peakboard Box.** It is a third of the size and looks like the
obvious choice, but the N5105 has no AVX instructions and ONNX Runtime has no
SSE-only kernel for int4 matrix multiply. Measured: **0.26 tokens/s**, against 3.6
for fp16 — about twelve times slower, for the same answers.

### Reasoning models

Models like Qwen3 reason out loud by default, and that reasoning is generated
token by token like everything else. On slow hardware a short answer can hide a
300-token internal monologue, turning ten seconds into two minutes. `Thinking` is
therefore `false` by default; the extension appends `/no_think` and strips any
`<think>` block from `Answer`.

## Why CPU only

There is a DirectML (GPU) flavour of ONNX Runtime GenAI, and this extension is
**not** built with it, for two reasons:

1. **It returns wrong answers on Intel integrated graphics.** On a Peakboard Box's
   Gen11 iGPU the same model and prompt that produce *"The sky on a clear day is
   blue."* on CPU produce a stream of nonsense tokens through DirectML. This is a
   [known ONNX Runtime issue](https://github.com/microsoft/onnxruntime/issues/19837)
   in the DirectML/Intel metacommand path, not specific to this hardware, and
   disabling metacommands does not fix it.
2. **Licensing.** DirectML is not MIT — it carries a Microsoft EULA restricting
   use to Windows and Xbox. Leaving it out keeps the entire runtime MIT.

If you have a machine with a GPU DirectML handles correctly, build with
`-p:Backend=dml`, but verify the output is *correct* before trusting it, and have
the DirectML licence reviewed.

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

## Licences

Everything in the shipped package is **MIT** (ONNX Runtime, ONNX Runtime GenAI,
and the .NET libraries). See `NOTICE.txt` in the ZIP.

The **model is licensed separately and is not shipped here.** Qwen3-0.6B is Apache
2.0, which permits commercial use but requires the licence text and a statement of
modifications to travel with the weights — converting to ONNX counts as a
modification. `NOTICE.txt` and `Qwen3-0.6B-LICENSE.txt` are included as a starting
point; adjust them if you ship a different model.
