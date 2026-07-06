# Peakboard Extension: GETT HMI

This extension integrates and controls buttons on [GETT](https://www.gett.de/) BlackLine Smart Panel PC Human Machine Interfaces (HMIs). It enables real-time interaction with GETT HMI buttons from Peakboard, including LED key coloring and full-color icon/image display on capable key bars.

The extension supports two different GETT hardware variants via two separate data sources:

- **Gett HMI Keys** – for the RGB LED capacitive key bar (USB PID `12820`). Reacts to key presses and lets you set LED colors, blink/switch/button modes and on-delays per key.
- **Gett HMI Display Keys** – for the newer display/icon key bar (USB PID `9040`). Reacts to key presses and lets you push custom images (Base64-encoded) to each key for its ON and OFF states.

Both data sources auto-detect their hardware over USB and disable themselves gracefully if the corresponding device is not connected.

## Documentation

For a step-by-step guide, see:

[Pimp my GETT - How to integrate the GETT BlackLine Smart Panel PC with Peakboard](https://how-to-dismantle-a-peakboard-box.com/Pimp-my-GETT-How-to-integrate-the-GETT-Blackline-Smart-Panel-PC-with-Peakboard.html)

## Installation

1. Download `GettHMI.zip` from the `Binaries` folder.
2. Add the extension to Peakboard Designer via **Manage Extensions**.
3. Add a new data source and select **GETT HMI** from the extensions list.
4. Choose the data source that matches your hardware: **Gett HMI Keys** (LED key bar) or **Gett HMI Display Keys** (display key bar).

## Data Sources

### Gett HMI Keys (LED key bar, PID 12820)

The list always contains exactly one row, which is overwritten on every key press. It has the following columns:

| Column      | Type   | Description                                  |
|-------------|--------|-----------------------------------------------|
| `Timestamp` | Number | Unix timestamp (ms) of the key press          |
| `KeyNumber` | Number | Number of the pressed key (1–6)               |

Only the rising edge of a key press (not-pressed → pressed) triggers an update; releasing a key is ignored. This means the list always reflects only the *most recently* pressed key, not a history of presses.

#### Functions

| Function               | Parameters                                                                                   | Returns   | Description                                                                 |
|-------------------------|-----------------------------------------------------------------------------------------------|-----------|------------------------------------------------------------------------------|
| `SetKeyColor`            | `Key Number` (Number), `HexCode` (String)                                                     | –         | Sets the LED color of a single key.                                          |
| `SetMultipleKeysColor`   | `HexCodeKey1`…`HexCodeKey6` (String, optional)                                                 | –         | Sets the LED color of up to 6 keys in one call. Empty values are skipped.     |
| `SetBlinkMode_Click`     | `Key Number` (Number), `HexOff` (String), `HexDelay` (String), `Delay` (Number)                | –         | Makes a key blink between its OFF color and a delay color at a given interval (ms). |
| `StopBlinkMode_Click`    | `Key Number` (Number)                                                                          | –         | Stops the blinking on a key.                                                  |
| `SetSwitchMode_Click`    | `Key Number` (Number), `HexOff` (String), `HexOn` (String)                                     | –         | Puts a key into toggle/switch mode with distinct ON/OFF colors.               |
| `SetButtonMode_Click`    | `Key Number` (Number)                                                                          | –         | Puts a key back into momentary button mode.                                  |
| `SetOnDelay_Click`       | `Key Number` (Number), `HexOff` (String), `HexDelay` (String), `Delay` (Number), `HexOn` (String) | –      | Configures an on-delay behavior with OFF, delay and ON colors.                |
| `RestOnDelay_Click`      | `Key Number` (Number)                                                                          | –         | Resets the on-delay configuration of a key.                                   |
| `ResetSettings_Click`    | –                                                                                              | –         | Performs a factory reset of the device.                                      |
| `IsDevicePresent`        | –                                                                                              | `Present` (Boolean) | Checks whether the LED key bar (PID 12820) is physically connected via USB, without opening a connection. |

Colors are passed as hex strings (with or without a leading `#`, e.g. `#FF0000` or `FF0000`).

### Gett HMI Display Keys (icon key bar, PID 9040)

The list always contains exactly one row, which is overwritten on every key press. It has the following columns:

| Column      | Type   | Description                                  |
|-------------|--------|-----------------------------------------------|
| `Timestamp` | Number | Unix timestamp (ms) of the key press          |
| `KeyNumber` | Number | Number of the pressed key (1–6)               |

Only the rising edge of a key press (not-pressed → pressed) triggers an update; releasing a key is ignored. This means the list always reflects only the *most recently* pressed key, not a history of presses. While the device is connected, native HID keyboard output of the bar is suppressed so key presses only surface through Peakboard.

#### Functions

| Function          | Parameters                                                     | Returns              | Description                                                                                   |
|-------------------|-----------------------------------------------------------------|-----------------------|-------------------------------------------------------------------------------------------------|
| `SetKeyImageOff`  | `KeyNumber` (Number, 1–6), `Base64Image` (String)                | –                     | Uploads the image shown when the key is in its **OFF/inactive** state. Blocks until the transfer to the device completes. |
| `SetKeyImageOn`   | `KeyNumber` (Number, 1–6), `Base64Image` (String)                | –                     | Uploads the image shown when the key is in its **ON/active** state. Blocks until the transfer to the device completes. |
| `IsDevicePresent` | –                                                                | `Present` (Boolean)   | Checks whether the display key bar (PID 9040) is physically connected via USB, without opening a connection. |

Notes on `Base64Image`:
- Accepts plain Base64 or a data URL (`data:image/png;base64,...`); any prefix before the first comma is stripped automatically.
- Images are transferred one at a time (guarded by an internal semaphore), with a short delay after each transfer to let the device finish processing.

## Example Usage in Peakboard

The following Lua snippets are taken from a real Peakboard project and show the typical wiring between UI, data sources and extension functions. In this example the two data sources were added as `GETT_Keys` (`Gett HMI Keys`) and `GETT_Displays` (`Gett HMI Display Keys`), both configured with **ReloadState = PushOnly**.

### Detecting whether the hardware is present

A timer that runs once at startup (after a short delay) polls `IsDevicePresent()` and disables the data source in the UI if the corresponding key bar is not connected:

```lua
if not data.GETT_Displays.IsDevicePresent() then
   data.GETT_Displays.isenabled = false
   peakboard.log(string.tostring(data.GETT_Displays.isenabled))
end
if not data.GETT_Keys.IsDevicePresent() then
   data.GETT_Keys.isenabled = false
   peakboard.log(string.tostring(data.GETT_Keys.isenabled))
end
```

### Reacting to a key press

Since each data source always holds exactly one row (see above), the `Refreshed` event on the data source reads `KeyNumber` from row `0` and reacts accordingly, e.g. to switch screens:

```lua
local key = 0

if data.GETT_Keys.count >= 1 then
   key = data.GETT_Keys[0].KeyNumber
   if key == 1 then
      runtime.showscreen('B')
   elseif key == 2 then
      runtime.showscreen('I')
   elseif key == 3 then
      runtime.showscreen('S')
   elseif key == 4 then
      runtime.showscreen('O')
   elseif key == 5 then
      runtime.showscreen('P')
   elseif key == 6 then
      runtime.showscreen('Q')
   end
end
```

The same pattern is used for `data.GETT_Displays`.

### Setting an LED key color (`Gett HMI Keys`)

A color picker element provides RGB values, which are converted into a hex string and passed to `SetKeyColor`:

```lua
local rgb = string.format("#%02X%02X%02X", data.RGB.first.Red, data.RGB.first.Green, data.RGB.first.Blue)

data.GETT_Keys.SetKeyColor(data.Key_selected, rgb)
```

### Uploading key images (`Gett HMI Display Keys`)

After the user picks an image for the ON and OFF state of a key, both are uploaded with a single click:

```lua
data.GETT_Displays.SetKeyImageOff(data.Key_selected, data.Image_select.first.Off)
data.GETT_Displays.SetKeyImageOn(data.Key_selected, data.Image_select.first.On)
```

(The surrounding app in this example additionally resets some UI-only state, e.g. clearing the selected key/image fields afterwards — that part is specific to that application and not related to the extension itself.)

## Requirements

- GETT BlackLine Smart Panel PC with either the RGB LED capacitive key bar (USB VID `8741` / PID `12820`) or the display/icon key bar (USB VID `8741` / PID `9040`).
- GETT manufacturer libraries `GETT_CapDeviceLib` and/or `GETT_DisplayDeviceLib`, matching whichever hardware variant is used.