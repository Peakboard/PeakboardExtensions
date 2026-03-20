# Peakboard Extension: Databricks

This extension connects Peakboard to [Databricks](https://www.databricks.com/), enabling you to read and write data from the Databricks cloud data platform.

## Custom List: Databricks Query

Execute SQL queries against a Databricks workspace and use the results as a Peakboard data source. Supports both reading and writing data back to Databricks.

### Configuration

| Parameter | Description |
|-----------|-------------|
| Server Hostname | Databricks workspace hostname |
| HTTP Path | SQL warehouse or cluster HTTP path |
| Access Token | Databricks personal access token |
| SQL Statement | The SQL query to execute |

## Documentation

- [Brick by Brick - Connecting Databricks and Peakboard](https://how-to-dismantle-a-peakboard-box.com/Brick-by-Brick-Connecting-Databricks-and-Peakboard.html)
- [Brick by Brick - Writing data back to Databricks from a Peakboard application](https://how-to-dismantle-a-peakboard-box.com/Brick-by-Brick-Writing-data-back-to-Databricks-from-a-Peakboard-application.html)

## Installation

1. Download `Databricks.zip` from the `Binaries` folder.
2. Add the extension to Peakboard Designer via **Manage Extensions**.
3. Add a new data source and select **Databricks** from the extensions list.

## Release Notes

2021-06-18 Initial Release
