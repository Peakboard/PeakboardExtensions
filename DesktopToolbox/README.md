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

## Installation

1. Download `DesktopToolbox.zip` from the `Binary` folder.
2. Add the extension to your Peakboard Designer via *Manage Extensions*.
3. Add a new data source and select **Desktop Information** from the Desktop Toolbox extension.

## Release Notes

2026-03-11 Version 1.0 - Initial Release
2026-05-18 Version 1.1 - Added `WriteTextFile` function
2026-05-18 Version 1.2 - `WriteTextFile` now resolves and logs the absolute write path and verifies the file after writing
2026-05-18 Version 1.3 - `WriteTextFile` now detects Windows UAC file virtualization and reports the redirected location instead of a misleading "OK"
