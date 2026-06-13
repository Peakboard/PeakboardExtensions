# Peakboard Extension: MySQL

This extension provides a database connector for [MySQL](https://www.mysql.com/) databases, allowing you to execute SQL queries directly from Peakboard.

## Custom List: MySql List

Execute SQL queries against a MySQL database and use the results as a Peakboard data source.

### Configuration

| Parameter | Description |
|-----------|-------------|
| Host | Hostname or IP address of the MySQL server |
| Port | TCP port of the MySQL server (default `3306`) |
| Database | Name of the database to connect to |
| Username | User name used to authenticate |
| Password | Password used to authenticate (stored masked) |
| SQLStatement | The SQL query to execute. Supports multi-line input and Peakboard parameters |

Date and time columns are returned as text. Numeric columns are returned as numbers and `BIT`/boolean columns as booleans.

## Installation

1. Download `MySQLExtensionNew.zip` from the `Binary` folder.
2. Add the extension to Peakboard Designer via **Manage Extensions**.
3. Add a new data source and select **MySql** from the extensions list.

> Two builds are shipped in the `Binary` folder: `MySQLExtensionNew.zip` (.NET 8, recommended) and `MySQLExtension.zip` (.NET Framework, legacy). Use the build that matches your Peakboard runtime.

## Release Notes

- 2026-06-13 v1.1 — Fixed a crash ("Failed to serialize PipeMessage value") that occurred when a query returned date/time columns or NULL dates.
- 2020-10-12 Initial Release
