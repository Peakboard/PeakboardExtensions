# Peakboard Extension: Object Detection

Detects objects in a camera feed **on the device itself** — a Peakboard Box, or the
PC running Peakboard Designer. No cloud service, no API key, no network call at
inference time. Nothing the camera sees leaves the machine.

It ships with a pretrained model covering the 80 everyday COCO classes, so it does
something useful the moment you add it. Point it at a camera and it works.

**Read [What it can and cannot recognise](#what-it-can-and-cannot-recognise) before
promising anyone a demo.** The shipped model knows 80 specific things, and a
screwdriver is not one of them.

## Data sources

Five, and they share one detection engine. Add the ones you need; the engine starts
when the first list starts and stops when the last one goes away.

### Object Detection - Camera

The current frame, one row.

| Property | Type | Default | Description |
|---|---|---|---|
| `CameraSource` | text | `0` | USB camera index, or an RTSP URL, or a path to a video file. See [Choosing a camera](#choosing-a-camera). |
| `ModelName` | text | `yolov9t` | Which model to run. See [Models](#models). |
| `ConfidenceThreshold` | number | `0.4` | Detections below this are discarded. |

| Column | Type | Description |
|---|---|---|
| `FrameBase64` | String | The frame with boxes and labels drawn on it, as a base64 JPEG. |
| `RawFrameBase64` | String | The same frame without the overlay. |
| `Width`, `Height` | Number | Frame size in pixels. |
| `DetectionCount` | Number | How many objects are in this frame. |
| `DetectionsJson` | String | The detections as a JSON array, matching **this** frame. |
| `Timestamp` | String | When the frame was captured. |
| `Status` | String | Engine state, or the error if there is one. |

### Object Detection - Detections

**One row per detected object, and no rows when nothing is detected.** That is what
makes `count` mean what you expect in Lua.

| Property | Type | Default |
|---|---|---|
| `CameraSource`, `ModelName`, `ConfidenceThreshold` | | as above |
| `NmsThreshold` | number | `0.45` — how aggressively overlapping boxes are merged |

| Column | Type | Description |
|---|---|---|
| `ClassName` | String | What was detected, e.g. `person`. |
| `ClassId` | Number | Its index in the model's class list. |
| `Confidence` | Number | 0 to 1. |
| `X`, `Y`, `Width`, `Height` | Number | Box in pixels, origin top-left of the frame. |
| `FrameWidth`, `FrameHeight` | Number | So you can scale the box to your screen. |
| `Timestamp`, `Status` | String | |

### Object Detection - Status

One row, always. This is where health lives, because the Detections list is empty
when there is nothing to detect and an empty table cannot tell you why.

| Column | Type | Description |
|---|---|---|
| `Status` | String | `not_started`, `idle`, `loading`, `connecting`, `ok`, `ok_model_fallback`, `reloading`, `reconnecting`, `error`, `stopped` |
| `Error` | String | Empty unless something is wrong. |
| `IsRunning` | Boolean | Whether the engine is alive. |
| `CameraSource` | String | What it is actually reading from. |
| `RequestedModel` | String | What the board asked for. |
| `LoadedModel` | String | **What is actually running.** If these two differ, read `Error`. |
| `ClassCount` | Number | How many classes the loaded model has. |
| `DetectionCount`, `FrameWidth`, `FrameHeight`, `Timestamp` | | |

### Object Detection - Cameras

The capture devices on the machine running the board.

| Column | Type | Description |
|---|---|---|
| `Index` | Number | **The number to put in `CameraSource`.** |
| `Name` | String | e.g. `Integrated Camera`, `Logitech StreamCam` |
| `DevicePath` | String | The USB/PnP path, if you need to tell two identical cameras apart. |
| `IsInUse` | Boolean | True for the one the engine is currently reading. |
| `Error` | String | Populated on the single row returned when there are no cameras at all. |

### Object Detection - Models

Every model the extension can see: `Name`, `Source` (`Pretrained` / `Resource` /
`Custom`), `SizeMB`, `OnnxPath`, `ClassesPath`.

## Choosing a camera

`CameraSource` is a **number** for USB cameras — `0`, `1`, `2` — and that is
deliberate. A board is built in Designer on one machine and deployed to a Box or a
BYOD device whose cameras nobody has seen. A name picked at design time would mean
nothing at the far end; the index is the only portable contract.

The **Cameras** list is how that number stops being a guess. Put it on a
commissioning screen and read it on the device that matters:

```lua
-- on the Cameras data source's Refreshed event
local s = ''
for i = 0, data.DS_Cameras.count - 1 do
   s = s .. '[' .. data.DS_Cameras[i].Index .. '] ' .. data.DS_Cameras[i].Name .. '\n'
end
screens['Screen1'].CameraListText.text = s
```

In Designer this lists the Designer PC's cameras, which is the wrong machine for a
deployed board — that is exactly why it exists at runtime.

**Index 0 is not always the one you want.** Laptops with an infrared Windows Hello
sensor sometimes expose it as a second capture device, and a virtual camera (OBS,
Teams) takes an index too. The Cameras list names them, so you can see which is
which instead of guessing.

For a network camera, put the URL in instead:

```
rtsp://user:password@192.168.1.50:554/stream1
```

RTSP goes through the bundled FFmpeg DLL; USB goes through DirectShow. A video file
path also works, which is the easiest way to demo without hardware.

## Models

**`yolov9t` ships with the extension** and is the default. YOLOv9-t exported by
LibreYOLO, MIT licensed, 8.3 MB, 80 COCO classes.

### Using your own model

The extension takes any YOLO-style ONNX model whose output is
`[1, 4 + numClasses, anchors]`. Two ways to supply one:

**As a Peakboard Resource** — add the `.onnx` to your board's resources. It travels
inside the `.pbmx`, so it deploys with the board and needs nothing on the device.
Add a `classes.txt` beside it, or `<modelname>_classes.txt`.

**As a folder on the device** — for models too large to embed:

```
C:\ProgramData\Peakboard\ObjectDetection\models\<your-model-name>\
    model.onnx
    classes.txt
```

Then set `ModelName` to `<your-model-name>`. It appears in the Models list as
`Source = Custom`. The folder is watched, so replacing `model.onnx` reloads it
without restarting the board.

### The class list must match the model

`classes.txt` is one class name per line, in the model's own class order. The
extension reads how many classes the model predicts out of its output tensor and
**refuses to load a class list of a different length.**

That check exists because the failure it prevents is invisible: a 3-class custom
model paired with the 80-line COCO list reports `person`, `bicycle` and `car`, with
plausible boxes and plausible confidences, and nothing anywhere says it is wrong.

If you see `Class list does not match the model` in `Error`, the model and the
class file are not a pair.

### If the model is not found

`LoadedModel` differs from `RequestedModel`, `Status` reads `ok_model_fallback`, and
`Error` names both. The extension keeps running on a bundled model rather than
showing a black screen — but it says so, every poll. There is no prefix matching:
asking for `yolov9` does not silently get you `yolov9t`.

## What it can and cannot recognise

The shipped model knows exactly these 80 things, and nothing else:

> person, bicycle, car, motorcycle, airplane, bus, train, truck, boat, traffic
> light, fire hydrant, stop sign, parking meter, bench, bird, cat, dog, horse,
> sheep, cow, elephant, bear, zebra, giraffe, backpack, umbrella, handbag, tie,
> suitcase, frisbee, skis, snowboard, sports ball, kite, baseball bat, baseball
> glove, skateboard, surfboard, tennis racket, bottle, wine glass, cup, fork, knife,
> spoon, bowl, banana, apple, sandwich, orange, broccoli, carrot, hot dog, pizza,
> donut, cake, chair, couch, potted plant, bed, dining table, toilet, tv, laptop,
> mouse, remote, keyboard, cell phone, microwave, oven, toaster, sink,
> refrigerator, book, clock, vase, scissors, teddy bear, hair drier, toothbrush

**An object that is not on that list does not come back empty. It comes back
wrong.** The model must put every detection into one of its 80 boxes, so it picks
the nearest one and reports it with real confidence.

Measured on 59 photographs of wrenches and Allen keys, at the default threshold:

| Reported | Times |
|---|---|
| scissors | 33 |
| toothbrush | 12 |
| airplane | 6 |
| person | 4 |
| baseball bat | 4 |
| knife, bird, remote | 3 each |

The boxes were in the right place. The words were not. A wrench came back as `bird`
at 0.61 confidence and as `kite` at 0.67.

**Raising `ConfidenceThreshold` does not fix this.** At 0.85 the model has gone
silent on 53 of the 59 photos and still calls a wrench an `airplane`. There is no
threshold that reports the tool correctly, because there is no correct label
available to report.

So: **industrial parts, tools, packaging, labels and defects need a model trained on
them.** This extension will run such a model — that is what `ModelName` and the
custom-model folder are for — but it does not train one, and the bundled model
cannot stand in for one. Demo it with the things it actually knows: people, chairs,
laptops, cups, phones, bottles.

## Reading detections in Lua

Draw the frame and iterate the objects:

```lua
-- Camera data source, Refreshed event
screens['Screen1'].CameraImage.imagedata = data.DS_Camera[0].FrameBase64

-- Detections data source, Refreshed event
local s = ''
for i = 0, data.DS_Detections.count - 1 do
   s = s .. string.format('%s %.0f%%\n',
                          data.DS_Detections[i].ClassName,
                          data.DS_Detections[i].Confidence * 100)
end
screens['Screen1'].DetectionText.text = s
```

Count one class, the common shop-floor case:

```lua
local people = 0
for i = 0, data.DS_Detections.count - 1 do
   if data.DS_Detections[i].ClassName == 'person' then people = people + 1 end
end
screens['Screen1'].PeopleCount.text = tostring(people)
```

Show the health, so a failure looks like a failure:

```lua
-- Status data source, Refreshed event
if data.DS_Status[0].Error ~= '' then
   screens['Screen1'].StatusText.text = data.DS_Status[0].Error
else
   screens['Screen1'].StatusText.text = data.DS_Status[0].LoadedModel ..
        ' - ' .. data.DS_Status[0].DetectionCount .. ' objects'
end
```

Set the data source refresh to **1 second** for a live-looking feed. The detection
engine runs at its own rate on a background thread; the refresh interval only
controls how often the board picks up the latest result.

## Performance

Inference is CPU-only, roughly 80 ms per frame at 640×640 on a normal PC. A
Peakboard Box is slower. Two things follow:

- **The engine is shared.** Adding the Camera and Detections lists for the same
  camera does not run inference twice.
- **Both lists must use the same `CameraSource` and `ModelName`.** They share one
  engine, so if they disagree the engine restarts on whichever set up last, and a
  warning goes to the Peakboard log.

## When something is wrong

Everything ends up in two places: the `Status` list, and the Peakboard log.

| `Status` / `Error` | What it means |
|---|---|
| `Designer preview - the engine runs in the Peakboard Runtime` | Normal in Designer. The engine only runs in the Runtime. |
| `Camera error: Failed to open camera: 1` | No camera at that index. Check the Cameras list on **that** machine. |
| `Class list does not match the model` | The `.onnx` and the `classes.txt` are not a pair. |
| `No class names for model` | The model has no `classes.txt` at all. |
| `ok_model_fallback` | `ModelName` was not found. `Error` names what loaded instead. |
| `reconnecting` | The feed dropped; it retries by itself. Normal on flaky RTSP. |

A camera that will not open reports the camera error — it is not disguised as a
Designer preview.

## Building from source

```powershell
powershell -ExecutionPolicy Bypass -File SourceCodeNew\Build.ps1
```

That builds the project and writes `Binary\ObjectDetection.zip`. It refuses to
produce a package that is missing `NOTICE.txt`, the licence text or the C++
runtime, that redistributes `Peakboard.ExtensionKit.dll`, or that contains an
Ultralytics model — the last checked by content rather than filename, so renaming
a file does not get past it.

`ObjectDetection.sln` opens the project in Visual Studio. Note that building
through the solution puts the output under `bind\...` and building the
`.csproj` directly puts it under `bin\...`; `Build.ps1` finds either.

## Installation

1. Take `ObjectDetection.zip` from the `Binary` folder.
2. In Peakboard Designer: **Data > Add data source > Manage extensions**.
3. **Add custom extension**, select the ZIP, then restart Designer.
4. Add the **Object Detection - Camera** data source and set `CameraSource`.

## Try it without building anything

`ExampleTemplate\ObjectDetectionCommissioning.pbmx` is a ready-made board that
puts all five data sources on one screen: the annotated live feed, the engine's
health, one row per detected object, and the cameras the machine can see.

It carries the extension inside it, so it can be uploaded straight to a Peakboard
Box and will run without installing anything first. Open it in Designer instead
if you want to look at how the bindings are put together.

It is also the fastest way to find out whether a camera works: if the Cameras
list is empty, or `Status` is not `ok`, the answer is on the screen rather than
in a log.

## Licences

Everything shipped is MIT or Apache 2.0, **except the FFmpeg video-I/O DLL, which is
LGPL**. See `NOTICE.txt` in the package — section 3 is the one to read before this
goes to a customer, and it explains that dropping that single 26 MB file removes the
LGPL question entirely at the cost of RTSP support.

The bundled model is MIT (LibreYOLO YOLOv9-t). It is deliberately **not** an
Ultralytics model — those are AGPL-3.0. The packaging script fails the build if an
Ultralytics artifact is staged, checking file contents rather than filenames.
