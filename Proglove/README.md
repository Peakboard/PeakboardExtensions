# Peakboard Extension: ProGlove

This extension integrates [ProGlove](https://www.proglove.com/) wearable barcode scanners with Peakboard. It connects to ProGlove gateways to receive scan events, track connected scanners, and generate reports.

## Custom Lists

### ProGlove Events
Receives real-time scan events from connected ProGlove scanners. Each event contains the scanned barcode data and scanner metadata.

### ProGlove Gateways
Lists all discovered ProGlove gateways on the network with their connection status.

### ProGlove Reports
Generates reports about scanner usage and scan statistics.

## Installation

1. Download `Proglove.zip` from the `Binaries` folder.
2. Add the extension to Peakboard Designer via **Manage Extensions**.
3. Add a new data source and select the desired ProGlove custom list.
