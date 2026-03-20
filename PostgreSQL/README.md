# Peakboard Extension: PostgreSQL

This extension provides a database connector for [PostgreSQL](https://www.postgresql.org/) databases, allowing you to execute SQL queries directly from Peakboard.

## Custom List: PostgreSQL Query

Execute SQL queries against a PostgreSQL database and use the results as a Peakboard data source.

### Configuration

| Parameter | Description |
|-----------|-------------|
| Connection String | Npgsql connection string to the PostgreSQL database |
| SQL Statement | The SQL query to execute |

## Installation

1. Download `Peakboard.Extensions.Npgsql.zip` from the `Binaries` folder.
2. Add the extension to Peakboard Designer via **Manage Extensions**.
3. Add a new data source and select **PostgreSQL** from the extensions list.

## Release Notes

2020-10-12 Initial Release
