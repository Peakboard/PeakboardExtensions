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

### Function: ExecuteStatement

The data source also exposes a function for statements that return no rows — `INSERT`, `UPDATE`, `DELETE`, DDL.

```lua
local rows = data.MyMySqlList.ExecuteStatement(
    "INSERT INTO readings (scale_id, weight) VALUES (3, 1420)")
```

| | |
|---|---|
| Input | `ExecuteStatement` (String) — the SQL statement to run |
| Returns | `RowsAffected` (Number) — rows the statement changed |

`RowsAffected` is worth checking. `0` is a legitimate answer, not an error: an
idempotent `INSERT ... ON DUPLICATE KEY UPDATE` that matched an existing row
returns `0`. A writer that never inspects it cannot tell "wrote a new row" from
"matched an existing one", and cannot report on itself at all.

Calling any other function name throws rather than silently doing nothing.

### How column types are mapped

Peakboard has three column types, so everything is mapped onto String, Number or Boolean:

| SQL / .NET type | Peakboard column | Value |
|---|---|---|
| `CHAR`, `VARCHAR`, `TEXT` → `string` | String | as-is |
| `BIT(1)` → `bool` | Boolean | as-is |
| `DATE`, `DATETIME`, `TIMESTAMP`, `TIME` | **String** | formatted text |
| `INT`, `BIGINT`, `DECIMAL`, `FLOAT`, … | Number | converted to double |
| anything else | String | `.ToString()` |

Two limits worth knowing before you design a query:

- **Binary columns are not usable.** `BLOB`, `BINARY` and `VARBINARY` arrive as
  `byte[]`, which is neither string, bool nor numeric, so they render as the
  literal text `System.Byte[]`. Convert or encode them in SQL if you need them.
- **Very large integers and high-precision decimals lose precision**, because
  Number is a double. `BIGINT` above 2^53 and `DECIMAL` beyond ~16 significant
  digits are affected. `CAST(... AS CHAR)` in the query keeps them exact.

A conversion that fails does not take the extension down: the value degrades to
the type's default (empty string, `false`, or `0`) instead of throwing.

## Installation

1. Download `MySQLExtensionNew.zip` from the `Binary` folder.
2. Add the extension to Peakboard Designer via **Manage Extensions**.
3. Add a new data source and select **MySql** from the extensions list.

> Two builds are shipped in the `Binary` folder: `MySQLExtensionNew.zip` (.NET 8, recommended) and `MySQLExtension.zip` (.NET Framework, legacy). Use the build that matches your Peakboard runtime.

## Building

**Both ZIPs in `Binary/` must be rebuilt whenever either source tree changes.**
They are the only thing customers install; a source-only change reaches nobody.
For three years `MySQLExtension.zip` shipped a 2023 assembly whose source had
moved on, because the binary was never rebuilt after the commit that changed it.

Both trees build with no SDK installs beyond Visual Studio / the .NET 8 SDK. The
Framework tree gets its 4.6.2 reference assemblies from the
`Microsoft.NETFramework.ReferenceAssemblies` NuGet package rather than a
system-wide Developer Pack, so it also builds on a clean machine and in CI.

```
# .NET 8 -> MySQLExtensionNew.zip
cd SourceCodeNew/MySQL
dotnet build -c Release

# .NET Framework 4.6.2 -> MySQLExtension.zip
cd SourceCode/PeakboardExtensionMySql
msbuild -t:restore
msbuild -t:Rebuild -p:Configuration=Release -p:Platform=x64
```

Then pack each build output flat — managed assemblies plus `Extension.xml`, no
folders. Excluded on purpose: `*.pdb`, `*.xml` IntelliSense docs,
`deps.json` / `runtimeconfig.json`, `runtimes/`, and `Peakboard.ExtensionKit.dll`
(the host provides it, and a second copy in the plugin load context is a
different type identity).

### Keeping the two trees in step

`SourceCode/` (.NET Framework) and `SourceCodeNew/` (.NET 8) are separate copies
of the same extension, and they have drifted apart twice — once when the write
function was added to one and not the other, and once when a build was made from
a tree that predated a fix already committed here. **A change to one belongs in
the other in the same commit**, and the version in `MySqlExtension.cs` and
`Extension.xml` must match across both.

The trees are not identical by construction: `SourceCode/` is C# 7.3, so it uses
classic `using (...)` blocks where `SourceCodeNew/` uses `using var`.

## Release Notes

- 2026-09-04 v1.5 — Write path restored and hardened, in both trees.
  - **`ExecuteStatement` now returns `RowsAffected`.** Previously the .NET
    Framework build computed the value and discarded it, and the .NET 8 build
    had no write function at all — so no board could verify its own writes.
  - **Connections are no longer leaked on failure.** Every path closed its
    connection only on success, so each failed read or write leaked one. Against
    a server that had started refusing connections this turned a transient fault
    into a permanent one — measured at ~357 leaked connections in 90 minutes at
    one site, against a default `max_connections` of 151.
  - **Connect and command timeouts are now set** (5 s / 15 s). With none set, a
    server that accepts the TCP connection but never completes the handshake
    parked the calling thread for the driver default; one site lost 13 hours of
    writes to this before anything was logged.
  - **The connection string is built with `MySqlConnectionStringBuilder`.** The
    old `string.Format($"...")` form interpolated first and then parsed the
    result for `{0}` placeholders, so a password containing a brace threw
    `FormatException` and one containing a semicolon silently truncated the
    connection string.
  - **Write outcomes are logged** as `SQL Command executed: {n} rows affected.`
    The Framework build previously used `Console.WriteLine`, which goes nowhere
    in an extension host. `GetDefinitionOverride` and `GetCustomListsOverride`
    are logged too — the latter is where the custom list publishes its function
    collection, so its absence from a box log is the signature of a data source
    that reads fine and silently cannot write.
  - Calling an unknown function name now throws instead of returning quietly.
  - `Extension.xml` in the .NET 8 build said `1.0` while the assembly said
    `1.1`; both now say `1.5`.
  - The Framework project builds without a system-wide targeting pack, and both
    binaries were rebuilt from source for this release.
- 2026-06-13 v1.1 — Fixed a crash ("Failed to serialize PipeMessage value") that occurred when a query returned date/time columns or NULL dates.
- 2020-10-12 Initial Release
