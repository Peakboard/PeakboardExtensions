# Peakboard Extension: Desktop Toolbox

This extension provides information about the current Windows desktop session and offers utility functions for interacting with the local desktop environment.

Please find more information on how to use the Desktop Toolbox with Peakboard here:

[Side by Side - Making Peakboard BYOD Play Nice among other Windows Apps](https://how-to-dismantle-a-peakboard-box.com/Side-by-Side-Making-Peakboard-BYOD-Play-Nice-among-other-Windows-Apps.html)

## Custom List: Desktop Information

The **Desktop Information** custom list returns a single row with details about the current desktop session. No configuration or connection properties are required.

### Columns

| Column           | Type   | Description                                      |
|------------------|--------|--------------------------------------------------|
| WindowsUserName  | String | The Windows user name of the current desktop session |
| OSVersion        | String | The OS version string (e.g. "Microsoft Windows NT 10.0.26200.0") |

### Functions

#### OpenURLInBrowser

Opens a given URL in the default browser of the operating system.

| Parameter | Type   | Required | Description                          |
|-----------|--------|----------|--------------------------------------|
| url       | String | Yes      | The URL to open (e.g. `https://www.peakboard.com`) |

**Example usage in Peakboard script:**

```lua
data.DesktopInformation.OpenURLInBrowser('https://www.peakboard.com')
```

#### WriteTextFile

Writes text content to a file on the local file system. The file is created if it does not exist and **overwritten** if it does. The content is written as UTF-8.

| Parameter | Type   | Required | Description                                              |
|-----------|--------|----------|----------------------------------------------------------|
| fileName  | String | Yes      | Full path of the file including the folder (e.g. `C:\Temp\out.txt`) |
| content   | String | Yes      | The text content to write                                |

| Return | Type | Description |
|--------|------|-------------|
| result | String | `OK` on success, or the error message (e.g. folder does not exist, access denied) on failure |

The function never throws back into Peakboard — any problem is returned as the `result` string, so check whether it equals `OK`.

> **Where does the file go?** The path is resolved to an **absolute** path before writing, and that resolved path is written to the extension log (e.g. `WriteTextFile: wrote 12 bytes to 'C:\Temp\out.txt'`). If you get `OK` but cannot find the file, check the log for the resolved path. Common reasons it differs from what you expect: a **relative path** is resolved against the Peakboard process working directory; the app runs on a **Peakboard Box** so the file is written on the Box, not your PC; or Windows redirected a write to a protected location (e.g. `C:\` root, `C:\Program Files`) into `%LOCALAPPDATA%\VirtualStore`. Always pass a full path to a writable folder such as `C:\Temp` or a user folder.

**Example usage in Peakboard script:**

```lua
local r = data.DesktopInformation.WriteTextFile('C:\\Temp\\out.txt', 'Hello world')
if r ~= 'OK' then
    -- handle error, r contains the message
end
```

#### OpenFileAsBase64Start / OpenFileAsBase64Result

Lets the user pick a file through the standard Windows file selection dialog (Explorer) and returns the file content as a **Base64** string — for example to upload the file to the Peakboard Hub or an external web service.

The operation is split into **two functions** on purpose. The Peakboard runtime enforces a timeout on extension function calls, and a file dialog can stay open for as long as the user needs. A single blocking call would therefore fail with a `TimeoutException` whenever the user does not pick a file within a few seconds. Instead:

1. **`OpenFileAsBase64Start`** opens the dialog on a background thread and returns immediately.
2. **`OpenFileAsBase64Result`** is polled (typically from a timer) and returns the outcome once the user has closed the dialog.

The dialog is always brought **to the front**, above all other windows — including full-screen/kiosk applications — so the user cannot miss it.

##### OpenFileAsBase64Start

| Parameter  | Type   | Required | Description |
|------------|--------|----------|-------------|
| extensions | String | No       | Comma-separated list of allowed file extensions **without** dots or wildcards, e.g. `pdf,jpg,png`. The dialog filter then only shows these file types (plus an "all files" fallback entry). Leave empty to allow all file types. |
| title      | String | No       | Title text shown in the dialog window. Defaults to "Datei auswählen". |

| Return | Type | Description |
|--------|------|-------------|
| result | String | JSON object `{ "status": "started" }` on success, or `{ "status": "busy" }` if a dialog from a previous call is still open (no second dialog is opened in that case). |

##### OpenFileAsBase64Result

Takes no parameters. Call it repeatedly (e.g. every 500 ms via a timer) until the status is a terminal one.

| Return | Type | Description |
|--------|------|-------------|
| result | String | JSON object, see below |

The returned JSON object has this shape:

```json
{
  "status": "done",
  "fileName": "C:\\Users\\Max\\Downloads\\report.png",
  "fileNameOnly": "report.png",
  "base64": "iVBORw0KGgoAAAANSUhEUgAA...",
  "error": ""
}
```

| Field        | Description |
|--------------|-------------|
| status       | One of: `idle` (no dialog was started, or the last result was already collected), `pending` (dialog is still open, keep polling), `done` (a file was picked), `cancelled` (the user closed the dialog without picking a file), `error` (something went wrong, see `error`). |
| fileName     | Full path of the selected file. Only populated when `status` is `done`. |
| fileNameOnly | File name without the folder path (e.g. `report.png`). Only populated when `status` is `done`. |
| base64       | Base64-encoded content of the selected file. Only populated when `status` is `done`. |
| error        | Empty on success; `CANCELLED` when cancelled; otherwise the error message. |

A terminal result (`done` / `cancelled` / `error`) is returned **exactly once**. The next call after that returns `idle` again. This makes both timer styles safe: a timer that runs permanently just sees `idle` while nothing is going on, and a result can never be processed twice.

##### Complete example: pick a file and upload it to the Peakboard Hub

This walkthrough builds a small "pick & upload" workflow. The user taps **Open** to pick a PNG file; once a file was picked, the **Upload** button becomes active and sends the file to the Peakboard Hub folder `/Uploads`.

**Building blocks:**

| Element | Type | Purpose |
|---|---|---|
| `Tools` | Data source (Desktop Information custom list) | Provides the two functions |
| `UploadVar` | Variable (String) | Buffers the result JSON between picking and uploading |
| `UploadCheck` | Timer, 500 ms, endless, initially **disabled** | Polls for the dialog result |
| `Open` | Button | Starts the dialog and the timer |
| `Upload` | Button, initially **disabled** | Uploads the buffered file to the Hub |

**1. Button `Open` — Tapped event.** Starts the dialog (filtered to PNG files, dialog title "Upload") and the polling timer:

```lua
peakboard.log(data.Tools.OpenFileAsBase64Start('png', 'Upload'))
timers.UploadCheck.start()
```

**2. Timer `UploadCheck` — script.** Polls the result. On a terminal status the timer stops itself; a successful pick is buffered in `UploadVar`:

```lua
local result = data.Tools.OpenFileAsBase64Result()
local status = json.getvaluefrompath(result, 'status', '#ERROR#')

if status ~= 'pending' and status ~= 'idle' then
   timers.UploadCheck.stop()

   if status == 'done' then
      data.UploadVar = result
   else
      peakboard.log('Dateiauswahl: ' .. status)
   end
end
```

Alternatively the timer can simply run **permanently** (enable it and drop the `start()`/`stop()` calls): while nothing is going on it sees `idle` and does nothing. Polling is extremely cheap — the extension only checks whether the background operation has finished; no dialog interaction or file access happens during a poll.

**3. Button `Upload` — enable via conditional formatting.** The button starts disabled; a conditional formatting rule on the button sets `IsEnabled = True` when `UploadVar` is not empty. This way the upload can only be triggered once a file was actually picked.

**4. Button `Upload` — Tapped event.** Extracts the file name and content from the buffered JSON, uploads it to the Hub, and clears the buffer (which disables the button again):

```lua
local DataTemp = data.UploadVar
peakboardhub.savebase64('/Uploads', json.getvaluefrompath(DataTemp, 'fileNameOnly', '#ERROR#'), json.getvaluefrompath(DataTemp, 'base64', '#ERROR#'))
data.UploadVar = ''
```

A Peakboard Hub file list data source on `/Uploads` can then be used to display the uploaded files on the dashboard.

> **Pitfall — do not shadow `data`:** never declare a local variable called `data` in your scripts (e.g. `local data = ''`). `data` is the global Peakboard object through which all data sources and variables are reached; shadowing it breaks every subsequent `data.<source>` call in that script with an *"attempt to index a nil value"* error. Use a different name such as `DataTemp` or `result` for locals.

> **Large files:** the entire file content travels through the script engine as one Base64 string (roughly 4/3 of the file size). This works fine for typical documents and images; for very large files consider whether passing the `fileName` to a different mechanism is more appropriate.

## Installation

1. Download `DesktopToolbox.zip` from the `Binary` folder.
2. Add the extension to your Peakboard Designer via *Manage Extensions*.
3. Add a new data source and select **Desktop Information** from the Desktop Toolbox extension.

## Release Notes

2026-03-11 Version 1.0 - Initial Release
2026-05-18 Version 1.1 - Added `WriteTextFile` function
2026-05-18 Version 1.2 - `WriteTextFile` now resolves and logs the absolute write path and verifies the file after writing
2026-05-18 Version 1.3 - `WriteTextFile` now detects Windows UAC file virtualization and reports the redirected location instead of a misleading "OK"
2026-07-03 Version 1.4 - Added `OpenFileAsBase64Start` / `OpenFileAsBase64Result`: file selection via the Windows Explorer dialog with optional extension filter, returning file path, name and content as Base64 in a JSON result. The dialog always opens in the foreground; the two-step start/poll design avoids the runtime's function call timeout.
