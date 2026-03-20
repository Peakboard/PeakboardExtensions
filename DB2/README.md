# Peakboard Extension: IBM DB2

This extension provides a database connector for [IBM DB2](https://www.ibm.com/db2) databases, allowing you to execute SQL queries directly from Peakboard.

## Custom List: DB2 Query

Execute SQL queries against an IBM DB2 database and use the results as a Peakboard data source.

### Configuration

| Parameter | Description |
|-----------|-------------|
| Connection String | DB2 connection string |
| SQL Statement | The SQL query to execute |

## Prerequisites

**IBM i Access Client Solutions (5733XJ1)** must be installed on the Peakboard Box for the DB2 driver to work.

## Installation

1. Download `PeakboardExtensionDB2.zip` from the `Binaries` folder.
2. Add the extension to Peakboard Designer via **Manage Extensions**.
3. Add a new data source and select **DB2** from the extensions list.

## Release Notes

2021-03-17 Initial Release
