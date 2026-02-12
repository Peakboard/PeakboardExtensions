using System;
using System.Data;
using Npgsql;
using Peakboard.ExtensionKit;

namespace PostgreSQL;

/// <summary>
/// Provides a custom list that contains data from a PostgreSQL Database.
/// The list can be obtained using the following parameters:
/// <list type="bullet">
///     <item>
///         <term>Host</term>
///         <description>The PostgreSQL Server.</description>
///     </item>
///     <item>
///         <term>Port</term>
///         <description>The PostgreSQL Server's port.</description>
///     </item>
///     <item>
///         <term>Database</term>
///         <description>The Database name.</description>
///     </item>
///     <item>
///         <term>Username</term>
///         <description>The name of the user that has access to the database.</description>
///     </item>
///     <item>
///         <term>Password</term>
///         <description>The user's password.</description>
///     </item>
///     <item>
///         <term>SQLStatement</term>
///         <description>The SQL statement that returns the list contents. This property defaults to <c>SELECT * FROM my_table</c>.</description>
///     </item>
/// </list>
/// </summary>
[Serializable]
[CustomListIcon("PostgreSQL.Elephant64.png")]
public class PostgreSQLCustomList : CustomListBase
{
    protected override CustomListDefinition GetDefinitionOverride()
    {
        return new CustomListDefinition
        {
            ID = PostgreSQLExtension.ExtensionId,
            Name = "SQL Statement",
            Description = "Returns data from a PostgreSQL Database",
            PropertyInputPossible = true,
            PropertyInputDefaults =
            {
                new CustomListPropertyDefinition() { Name = "Host", Value = "localhost" },
                new CustomListPropertyDefinition() { Name = "Port", Value = "5432" },
                new CustomListPropertyDefinition() { Name = "Database", Value = "" },
                new CustomListPropertyDefinition() { Name = "Username", Value = "postgres" },
                new CustomListPropertyDefinition() { Name = "Password", Masked = true, Value = "" },
                new CustomListPropertyDefinition() { Name = "SQLStatement", Value = "SELECT * FROM my_table", EvalParameters = true, MultiLine = true }
            },
        };
    }

    protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
    {
        try
        {
            var columns = new CustomListColumnCollection();

            using var connection = OpenConnection(data);
            using var reader = ReadStatement(data, connection);

            var schema = reader.GetSchemaTable();
            if (schema == null)
                return columns;

            foreach (DataRow column in schema.Rows)
            {
                var name = column["ColumnName"]?.ToString() ?? string.Empty;
                var dataType = column["DataType"] as Type;

                var listColumnType = CustomListColumnTypes.String;
                if (dataType == typeof(string))
                    listColumnType = CustomListColumnTypes.String;
                else if (dataType == typeof(bool))
                    listColumnType = CustomListColumnTypes.Boolean;
                else if (dataType != null)
                    listColumnType = DataTypeHelper.IsNumericType(dataType) ? CustomListColumnTypes.Number : CustomListColumnTypes.String;

                columns.Add(new CustomListColumn(name, listColumnType));
            }

            return columns;
        }
        catch (Exception e)
        {
            Log?.Error("Error while fetching columns from PostgreSQL Database.", e);
            return new CustomListColumnCollection();
        }
    }

    protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
    {
        try
        {
            using var connection = OpenConnection(data);
            using var dataReader = ReadStatement(data, connection);

            var dataTable = new DataTable();
            dataTable.Load(dataReader);

            var items = new CustomListObjectElementCollection();

            foreach (DataRow sqlrow in dataTable.Rows)
            {
                var newitem = new CustomListObjectElement();

                foreach (DataColumn sqlcol in dataTable.Columns)
                {
                    var value = DataTypeHelper.GetOrConvertNumericTypeToDouble(sqlcol.DataType, sqlrow[sqlcol.ColumnName]);

                    // Avoid nulls in string columns
                    if (value == null)
                        value = string.Empty;

                    newitem.Add(sqlcol.ColumnName, value);
                }

                items.Add(newitem);
            }

            Log?.Info(string.Format("PostgreSQL extension fetched {0} rows.", items.Count));
            return items;
        }
        catch (Exception e)
        {
            Log?.Error("Error while fetching items from PostgreSQL Database.", e);
            return new CustomListObjectElementCollection();
        }
    }

    protected override void SetupOverride(CustomListData data)
    {
        try
        {
            // no-op
        }
        catch (Exception e)
        {
            Log?.Error("Error in SetupOverride (PostgreSQL)", e);
        }
    }

    protected override CustomListExecuteReturnContext ExecuteFunctionOverride(CustomListData data, CustomListExecuteParameterContext context)
    {
        try
        {
            return base.ExecuteFunctionOverride(data, context);
        }
        catch (Exception e)
        {
            Log?.Error("Error in ExecuteFunctionOverride (PostgreSQL)", e);
            return new CustomListExecuteReturnContext();
        }
    }

    private static NpgsqlConnection OpenConnection(CustomListData data)
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = data.Properties["Host"],
            Port = int.Parse(data.Properties["Port"]),
            Username = data.Properties["Username"],
            Password = data.Properties["Password"],
            Database = data.Properties["Database"],
            Pooling = false
        };

        var connection = new NpgsqlConnection(builder.ToString());
        connection.Open();
        return connection;
    }

    private static NpgsqlDataReader ReadStatement(CustomListData data, NpgsqlConnection connection)
    {
        var command = connection.CreateCommand();
        command.CommandText = data.Properties["SQLStatement"];
        return command.ExecuteReader();
    }

    protected override void CheckDataOverride(CustomListData data)
    {
        var validData =
            data.Properties.TryGetValue("Host", out var host) &&
            data.Properties.TryGetValue("Port", out var portString) &&
            data.Properties.TryGetValue("Username", out var username) &&
            data.Properties.TryGetValue("Password", out var password) &&
            data.Properties.TryGetValue("Database", out var database) &&
            data.Properties.TryGetValue("SQLStatement", out var statement) &&
            !string.IsNullOrEmpty(host) &&
            int.TryParse(portString, out _) &&
            !string.IsNullOrEmpty(database) &&
            !string.IsNullOrEmpty(statement);

        if (!validData)
            throw new InvalidOperationException("Invalid or no data provided. Make sure to fill out all properties. Port must be a number.");

        base.CheckDataOverride(data);
    }
}
