using System;
using System.Data;
using Firebird;
using FirebirdSql.Data.FirebirdClient;
using Peakboard.ExtensionKit;

namespace Firebird;

[CustomListIcon("Firebird.Firebird.png")]
public class FirebirdCustomList : CustomListBase
{
    protected override CustomListDefinition GetDefinitionOverride()
    {
        return new CustomListDefinition
        {
            ID = "FirebirdCustomList",
            Name = "SQL Statement",
            Description = "Returns data from a Firebird Database",
            PropertyInputPossible = true,
            PropertyInputDefaults = {
                new CustomListPropertyDefinition() { Name = "DataSource", Value = "" },
                new CustomListPropertyDefinition() { Name = "Port", Value = "3050" },
                new CustomListPropertyDefinition() { Name = "Database", Value = "" },
                new CustomListPropertyDefinition() { Name = "UserID", Value = "" },
                new CustomListPropertyDefinition() { Name = "Password", Masked = true, Value = "" },
                new CustomListPropertyDefinition() { Name = "SQLStatement", Value = "SELECT * FROM my_table", MultiLine = true  }
            },
        };
    }

    protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
    {
        var columns = new CustomListColumnCollection();
        var connection = OpenConnection(data);
        var reader = ReadStatement(data, connection);
        var schema = reader.GetSchemaTable();

        foreach (DataRow column in schema.Rows)
        {
            var name = (string)column["ColumnName"];
            var dataType = (Type)column["DataType"];

            CustomListColumnTypes listColumnType;

            if (dataType == typeof(string))
                listColumnType = CustomListColumnTypes.String;
            else if (dataType == typeof(bool))
                listColumnType = CustomListColumnTypes.Boolean;
            else
                listColumnType = DataTypeHelper.IsNumericType(dataType) ? CustomListColumnTypes.Number : CustomListColumnTypes.String;

            var customListColumn = new CustomListColumn(name, listColumnType);
            columns.Add(customListColumn);
        }

        connection.Close();
        reader.Close();

        return columns;
    }

    protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
    {
        var dataTable = new DataTable();

        var connection = OpenConnection(data);
        var dataReader = ReadStatement(data, connection);
        dataTable.Load(dataReader);

        var items = new CustomListObjectElementCollection();

        foreach (DataRow sqlrow in dataTable.Rows)
        {
            var newitem = new CustomListObjectElement();
            foreach (DataColumn sqlcol in dataTable.Columns)
                newitem.Add(sqlcol.ColumnName, DataTypeHelper.GetOrConvertNumericTypeToDouble(sqlcol.DataType, sqlrow[sqlcol.ColumnName]));
            items.Add(newitem);
        }

        Log?.Info(string.Format("Firebird extension fetched {0} rows.", items.Count));

        connection.Close();
        dataTable.Dispose();
        dataReader.Close();

        return items;
    }

    private FbConnection OpenConnection(CustomListData data)
    {
        var builder = new FbConnectionStringBuilder();
        builder.DataSource = data.Properties["DataSource"];
        builder.Port = int.Parse(data.Properties["Port"]);
        builder.UserID = data.Properties["UserID"];
        builder.Password = data.Properties["Password"];
        builder.Database = data.Properties["Database"];

        var connection = new FbConnection(builder.ToString());
        connection.Open();

        return connection;
    }

    private FbDataReader ReadStatement(CustomListData data, FbConnection connection)
    {
        var command = connection.CreateCommand();
        command.CommandText = data.Properties["SQLStatement"];

        return command.ExecuteReader();
    }

    protected override void CheckDataOverride(CustomListData data)
    {
        var validData =
            data.Properties.TryGetValue("DataSource", out var DataSource) &&
            data.Properties.TryGetValue("Port", out var portString) &&
            data.Properties.TryGetValue("UserID", out var UserID) &&
            data.Properties.TryGetValue("Password", out var password) &&
            data.Properties.TryGetValue("Database", out var database) &&
            data.Properties.TryGetValue("SQLStatement", out var statement) &&
            !string.IsNullOrEmpty(DataSource) &&
            int.TryParse(portString, out _) &&
            !string.IsNullOrEmpty(database) &&
            !string.IsNullOrEmpty(statement);

        if (!validData)
        {
            throw new InvalidOperationException("Invalid or no data provided. Make sure to fill out all properties. Port must be a number.");
        }

        base.CheckDataOverride(data);
    }
}