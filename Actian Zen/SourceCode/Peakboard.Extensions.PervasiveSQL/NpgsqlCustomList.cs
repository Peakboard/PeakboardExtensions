
using System.Data;
using System.Data.Common;
using Pervasive.Data.SqlClient;
using Peakboard.ExtensionKit;
using System;
using System.Data;

namespace Peakboard.Extensions.Npgsql
{
    /// <summary>
    /// Provides a custom list that contains data from a PervasiveSQL Database.
    /// The list can be obtained using the following parameters:
    /// <list type="bullet">
    ///     <item>
    ///         <term>Host</term>
    ///         <description>The PervasiveSQL Server.</description>
    ///     </item>
    ///     <item>
    ///         <term>Port</term>
    ///         <description>The PervasiveSQL Server's port.</description>
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
    public class NpgsqlCustomList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "NpgsqlCustomList",
                Name = "SQL Statement",
                Description = "Returns data from a PervasiveSQL Database",
                PropertyInputPossible = true,
                PropertyInputDefaults = {
                    new CustomListPropertyDefinition() { Name = "Host", Value = "localhost" },
                    new CustomListPropertyDefinition() { Name = "Port", Value = "5432" },
                    new CustomListPropertyDefinition() { Name = "Database", Value = "" },
                    new CustomListPropertyDefinition() { Name = "Username", Value = "Pervasives" },
                    new CustomListPropertyDefinition() { Name = "Password", Masked = true, Value = "" },
                    new CustomListPropertyDefinition() { Name = "SQLStatement", Value = "SELECT * FROM my_table", EvalParameters = true, MultiLine = true  }
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
                var name = (string) column["ColumnName"];
                var dataType = (Type)column["DataType"];

                CustomListColumnTypes listColumnType;

                if (dataType == typeof(string))
                    listColumnType = CustomListColumnTypes.String;
                else if (dataType == typeof(bool))
                    listColumnType = CustomListColumnTypes.Boolean;
                else
                    listColumnType = DataTypeHelper.IsNumericType(dataType) ? CustomListColumnTypes.Number : CustomListColumnTypes.String;

                CustomListColumn customListColumn = new CustomListColumn(name, listColumnType);
                columns.Add(customListColumn);
            }

            connection.Close();
            reader.Close();

            return columns;
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            DataTable dataTable = new DataTable();
            
            PsqlConnection connection = OpenConnection(data);
            PsqlDataReader dataReader = ReadStatement(data, connection);
            dataTable.Load(dataReader);

            var items = new CustomListObjectElementCollection();

            foreach (DataRow sqlrow in dataTable.Rows)
            {
                CustomListObjectElement newitem = new CustomListObjectElement();
                foreach (DataColumn sqlcol in dataTable.Columns)
                    newitem.Add(sqlcol.ColumnName, DataTypeHelper.GetOrConvertNumericTypeToDouble(sqlcol.DataType, sqlrow[sqlcol.ColumnName]));
                items.Add(newitem);
            }

            this.Log?.Info(string.Format("Ingres extension fetched {0} rows.", items.Count));

            connection.Close();
            dataTable.Dispose();
            dataReader.Close();

            return items;
        }

        private PsqlConnection OpenConnection(CustomListData data)
        {
            PsqlConnectionStringBuilder builder = new PsqlConnectionStringBuilder();
            builder.Host = data.Properties["Host"];
            builder.Port = int.Parse(data.Properties["Port"]);
            builder.UserID = data.Properties["Username"];
            builder.Password = data.Properties["Password"];
            builder.DatabaseName = data.Properties["Database"];

            var connection = new PsqlConnection(builder.ToString());
            connection.Open();

            return connection;
        }

        private PsqlDataReader ReadStatement(CustomListData data, PsqlConnection connection)
        {
            PsqlCommand command = connection.CreateCommand();
            command.CommandText = data.Properties["SQLStatement"];

            return command.ExecuteReader();
        }

        protected override void CheckDataOverride(CustomListData data)
        {
            bool validData =
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
            {
                throw new InvalidOperationException("Invalid or no data provided. Make sure to fill out all properties. Port must be a number.");
            }

            base.CheckDataOverride(data);
        }
    }
}