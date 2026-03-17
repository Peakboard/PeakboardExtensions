# Peakboard Extension: Desktop Toolbox

This extension provides information about the current Windows desktop session and offers utility functions for interacting with the local desktop environment.

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

## Installation

1. Download `DesktopToolbox.zip` from the `Binary` folder.
2. Add the extension to your Peakboard Designer via *Manage Extensions*.
3. Add a new data source and select **Desktop Information** from the Desktop Toolbox extension.

## Release Notes

2026-03-11 Version 1.0 - Initial Release
