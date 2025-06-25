# Peakboard Extension for Serial Barcode Scanners

This extension connects a barcode scanner via a serial port (COM port) to a Peakboard application. It ensures that scanned data is sent directly to the application's logic, bypassing the need for keyboard emulation or focused text boxes.

Additionally, if your scanner supports serial commands (e.g., for triggering a scan), you can send these commands directly from your Peakboard application.

## Features

* Directly listen for data from any serial COM port.
* Access scanned data within your Peakboard application logic.
* Send trigger commands or configuration strings to the scanner.
* Full configuration of serial port parameters (Baudrate, Parity, etc.).

## Configuration

The following parameters can be set in the extension's settings panel.

| Parameter    | Description                                       | Example Value |
| :----------- | :------------------------------------------------ | :------------ |
| `SerialPort` | The COM port the scanner is connected to.         | `COM8`        |
| `Baudrate`   | The baud rate required by your scanner.           | `9600`        |
| `Parity`     | The parity scheme. Options: `None`, `Odd`, `Even`.  | `None`        |
| `StopBits`   | The stop bits. Options: `One`, `Two`, `OnePointFive`. | `One`         |
| `DataBits`   | The data bits. Options: `8`, `7`, `6`, `5`.         | `8`           |

### Sending a Command to the Scanner

You can send a command string to the scanner using the `SendSerialCommand` function. This is useful for triggering the scanner remotely.

**Sample for sending a Scan Trigger command to a Honeywell N4680 Scanner:**

The command `<SYN>T<CR>` triggers a scan. `<SYN>` is ASCII character 22 and `<CR>` (Carriage Return) is ASCII character 13.

```lua
-- Sends the trigger command to the scanner
data.Scanner.SendSerialCommand('<22>T<13>')
```