# Peakboard Extension: InfluxDB

This extension connects Peakboard to [InfluxDB](https://www.influxdata.com/) time-series databases, supporting both reading and writing data.

## Custom Lists

### InfluxDB Query
Execute InfluxQL or Flux queries against an InfluxDB instance and display the results as a Peakboard data source.

### InfluxDB Write
Write data points to an InfluxDB measurement from Peakboard scripts.

### Configuration

| Parameter | Description |
|-----------|-------------|
| Server URL | URL of the InfluxDB instance |
| Database / Bucket | Target database or bucket name |
| Credentials | Username/password or token for authentication |
| Query | InfluxQL or Flux query string |

## Installation

1. Download `InfluxDB.zip` from the `Binaries` folder.
2. Add the extension to Peakboard Designer via **Manage Extensions**.
3. Add a new data source and select the desired InfluxDB custom list.
