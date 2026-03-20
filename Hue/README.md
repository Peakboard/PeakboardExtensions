# Peakboard Extension: Philips Hue

This extension connects Peakboard to a [Philips Hue](https://www.philips-hue.com/) Bridge, allowing you to monitor and control smart light bulbs from your Peakboard dashboard.

## Custom Lists

### Hue Lights
Returns a list of all lights registered on the Hue Bridge with their current status (on/off, brightness, color).

### Hue Lights Storage
Provides persistent storage for light configurations.

## Features

- List all available lights on the Hue Bridge
- Switch lights on and off
- Adjust brightness levels
- Use custom functions within a custom list (see source code for examples)

## Configuration

| Parameter | Description |
|-----------|-------------|
| Bridge IP | IP address of the Philips Hue Bridge |
| API Key | Hue Bridge API key (press the bridge button to pair) |

## Installation

1. Download `PeakboardExtensionHue.zip` from the `Binaries` folder.
2. Add the extension to Peakboard Designer via **Manage Extensions**.
3. Add a new data source and select the desired Hue custom list.
