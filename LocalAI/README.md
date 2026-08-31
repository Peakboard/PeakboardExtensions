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
| `ModelPath` | text | `C:\LocalAI\models\qwen3-0.6b` | Folder holding an ONNX Runtime GenAI model. Must contain `genai_config.json`. |
| `Device` | list | `cpu` | `cpu` or `dml`. The published build is CPU-only and `dml` will refuse with a message saying so. See [DirectML](#why-cpu-only). |
| `SystemPrompt` | text, multi-line | `You are a helpful assistant. Answer briefly.` | Standing instruction sent before every question. |
| `MaxNewTokens` | number | `128` | Upper bound on answer length. Every token costs time. Trimmed automatically if the prompt leaves less room than this in the context window. |
| `MaxPromptTokens` | number | `2048` | Refuses a prompt longer than this. **Read [Prompt length](#prompt-length-is-the-real-limit) before raising it** — this guard is what stands between a large data source and a 160 GB memory request. `0` disables it. |
| `Thinking` | checkbox | `false` | `true` lets a reasoning model think out loud. Slow — see below. |

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

**The extension ships without one.** Models are 0.5–3 GB; they do not belong in a
git repository, and which one you want depends on your hardware. You point
`ModelPath` at a folder you create once.

The model must be in **ONNX Runtime GenAI** format — a folder containing
`genai_config.json`, `model.onnx`, `model.onnx.data` and a tokenizer. Neither raw
`.safetensors` nor the `onnx/` folder published for transformers.js will work;
if there is no `genai_config.json`, it is the wrong format.

There are two ways to get one, and **which one is open to you depends on the
precision you need**:

| | Download a prebuilt one | Convert one yourself |
|---|---|---|
| effort | a browser, a few minutes | Python, ~10 min, a few GB of scratch |
| available as | **int4 only** | any precision |
| use it for | a PC or laptop | a Peakboard Box, or anything unusual |

### Which model

**Precision is not a detail here, and the right answer inverts between a Box and
a PC.** Pick your row:

| Hardware | Model | Precision | On disk | How |
|---|---|---|---|---|
| Peakboard Box (Celeron N5105, no AVX) | Qwen3-0.6B | **fp16** | ~1.2 GB | convert |
| PC or laptop, 16 GB RAM | Qwen3-4B | **int4** | ~2.8 GB | download |
| PC with 8 GB RAM, or you want snappier answers | Qwen3-1.7B | **int4** | ~1.1 GB | download |

**Why the reversal.** int4 matrix multiply (`MatMulNBits`) has hand-written AVX2
and AVX-512 kernels in ONNX Runtime, and no SSE-only one. Every x86 CPU since
roughly 2013 has AVX2, so on a PC int4 is the fast path — a quarter of the memory
traffic for nearly the same answers. The Box's Celeron is one of the few current
CPUs *without* AVX, so it falls back to a reference implementation and int4
collapses: **0.26 tokens/s, against 3.6 for fp16 of the same model.** Do not carry
the Box's fp16 recommendation onto a PC, and do not carry a PC's int4 onto a Box.

This is also why there is no download link for the Box. The prebuilt ONNX Runtime
GenAI models published on Hugging Face are int4 — the one precision a Box cannot
use — so a Box model has to be converted.

### Downloading a prebuilt model (PC)

`onnx-community` publishes ready-made ONNX Runtime GenAI builds. In a browser, open

- **Qwen3-4B** — <https://huggingface.co/onnx-community/Qwen3-4B-ONNX/tree/main/onnxruntime/cpu_and_mobile/cpu-int4-kld-block-128>
- **Qwen3-1.7B** — <https://huggingface.co/onnx-community/Qwen3-1.7B-ONNX/tree/main/onnxruntime/cpu_and_mobile/cpu-int4-kld-block-128>

and download **every file in that folder** into one local folder, e.g.
`C:\LocalAI\models\qwen3-4b`. It is a handful of files; `model.onnx.data` is the
big one. Then point `ModelPath` at that folder.

With a shell, the same thing in one line:

```bash
pip install huggingface_hub
hf download onnx-community/Qwen3-4B-ONNX \
    --include "onnxruntime/cpu_and_mobile/cpu-int4-kld-block-128/*" \
    --local-dir C:\LocalAI\models\qwen3-4b
```

**`ModelPath` must be the folder that holds `genai_config.json`** — with the
command above that is
`C:\LocalAI\models\qwen3-4b\onnxruntime\cpu_and_mobile\cpu-int4-kld-block-128`,
not the download root. This is the single most common way to get a "No
genai_config.json in ..." error.

### Converting one yourself (Box, or any other precision)

```bash
pip install onnxruntime-genai transformers torch
```

```bash
# Peakboard Box - fp16. int4 is ~12x SLOWER here; see Performance.
python -m onnxruntime_genai.models.builder \
    -m Qwen/Qwen3-0.6B -o C:\LocalAI\models\qwen3-0.6b -p fp16 -e dml

# PC - int4, if you would rather build than download.
python -m onnxruntime_genai.models.builder \
    -m Qwen/Qwen3-4B   -o C:\LocalAI\models\qwen3-4b   -p int4 -e cpu
```

`-e` names the **export flavour, not the runtime provider**. The fp16 model above
is built with `-e dml` and runs perfectly well on the CPU — that is the exact
model behind every Box number in [Performance](#performance). `-p fp16 -e cpu` is
accepted by the builder too, but it is not the combination that was tested here.

Any model the builder supports will work — Qwen, Llama, Phi, Gemma, Mistral.
Check the model's own licence before shipping it in a product: Qwen3 is Apache
2.0, but Gemma and Llama have their own terms.

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

## Prompt length is the real limit

The obvious way to use this on a dashboard is to walk a data source and put every
row in the prompt. That works, and then stops working — abruptly, and with an
error message that does not explain itself. **This is the thing to understand
before building anything on it.**

A model advertises a context window: Qwen3-4B says 40,960 tokens. That number is
a hard ceiling, not a budget you can spend. Two separate walls arrive well before
it:

**1. Memory.** ONNX Runtime's CPU attention kernel materialises the whole
attention score matrix, so that one allocation grows with the *square* of the
prompt. It is the allocation that kills you long before the context limit does.

**2. Time.** Reading the prompt dominates everything else; the answer's length
barely matters by comparison. Measured, it costs roughly 2.5x per doubling of the
prompt.

Measured end to end on a Ryzen 7 PRO 7840U (8 cores, 28 GB) with Qwen3-4B int4 —
a fast machine, far above a Peakboard Box:

| Prompt | Peak RAM | Wall clock |
|---|---|---|
| ~1,000 tokens | 4.4 GB | 37 s |
| ~2,000 tokens | 5.9 GB | 52 s |
| ~4,100 tokens | 9.8 GB | 130 s |
| ~8,200 tokens | 17.0 GB | 332 s |
| ~36,900 tokens | **fails** — asks for a single 163 GB buffer | — |

(Roughly 3.4 GB of that peak is the loaded model, which is paid once regardless
of prompt length.)

The last row is still *inside* the model's 40,960-token context. It fails anyway,
with `BFCArena::AllocateRawInternal Failed to allocate memory for requested buffer
of size 174720360960` naming a node in layer 0 — which tells you nothing about
the prompt being too long. Go past 40,960 instead and you get
`max_length (55160) cannot be greater than model context_length (40960)`, which at
least names the problem.

**`MaxPromptTokens` (default 2048) refuses the prompt before either of those
happens**, with a message that says what to do about it. Raise it if you have the
memory and the patience; the numbers above are what you are buying. Set it to `0`
only if you want the runtime's errors instead.

If you have more data than fits, the answer is not a bigger limit. A 4,000-token
prompt is over two minutes of staring at a dashboard and an 8,000-token one is
five and a half, on a laptop far quicker than a Box. Filter or aggregate the rows
first and send the model a summary — a hundred rows of readings become "3 lines
faulted, worst is Line 3 at E-17". It is being asked to judge, not to read.

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

If you have a machine with a GPU DirectML handles correctly, swap the package
reference in `LocalAI.csproj` from `Microsoft.ML.OnnxRuntimeGenAI` to
`Microsoft.ML.OnnxRuntimeGenAI.DirectML`, rebuild, and set `Device` to `dml`.
Verify the output is *correct* before trusting it — see above, this is exactly
where it goes wrong quietly — and have the DirectML licence reviewed.

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
