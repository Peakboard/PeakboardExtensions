using System;
using System.Data;
using MySql.Data.MySqlClient;
using MySqlX.XDevAPI.Relational;
using Peakboard.ExtensionKit;

namespace PeakboardExtensionMySql
{
    [Serializable]
    class MySqlCustomList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = $"MySqlCustomList",
                Name = "MySql List",
                Description = "Returns data from MySql database",
                PropertyInputPossible = true,
                PropertyInputDefaults = {
                    new CustomListPropertyDefinition() { Name = "Host", Value = "xxx.compute.amazonaws.com" },
                    new CustomListPropertyDefinition() { Name = "Port", Value = "3306" },
                    new CustomListPropertyDefinition() { Name = "Database", Value = "sys" },
                    new CustomListPropertyDefinition() { Name = "Username", Value = "peakboard" },
                    new CustomListPropertyDefinition() { Name = "Password", Masked = true, Value="" },
                    new CustomListPropertyDefinition() { Name = "SQLStatement", Value = "select * from testtable", EvalParameters = true, MultiLine = true  },
                },
                Functions = new CustomListFunctionDefinitionCollection
                {
                    new CustomListFunctionDefinition()
                    {
                        Name = "ExecuteStatement",
                        Description = "Executes a SQL statement that returns no rows (INSERT, UPDATE, DELETE, DDL).",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "ExecuteStatement",
                                Description = "Enter your SQL Statement",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.String
                            }
                        },
                        ReturnParameters = new CustomListFunctionReturnParameterDefinitionCollection
                        {
                            new CustomListFunctionReturnParameterDefinition
                            {
                                Name = "RowsAffected",
                                Description = "Rows the statement changed. 0 is a legitimate answer - an idempotent INSERT that matched an existing row returns 0.",
                                Type = CustomListFunctionParameterTypes.Number
                            }
                        }
                    }
                }
            };
        }

        protected override void CheckDataOverride(CustomListData data)
        {
            CheckProperties(data);
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            data.Properties.TryGetValue("SQLStatement", StringComparison.OrdinalIgnoreCase, out var SQLStatement);

            var cols = new CustomListColumnCollection();

            // using on all three. The previous shape reached con.Close() only when
            // nothing threw, so every failed schema read leaked a connection, its
            // socket and an undisposed reader.
            using (var con = GetConnection(data))
            using (var command = new MySqlCommand(SQLStatement, con))
            using (var reader = command.ExecuteReader())
            {
            var schemaTable = reader.GetSchemaTable();

            foreach (DataRow sqlcol in schemaTable.Rows)
            {
                var columnName = (string)sqlcol["ColumnName"];
                var dataType = (Type)sqlcol["DataType"];
                var listColumnType = CustomListColumnTypes.String;

                // We convert the types to one of the three Peakboard types (string, number or boolean)
                if (dataType == typeof(string))
                    listColumnType = CustomListColumnTypes.String;
                else if (dataType == typeof(bool))
                    listColumnType = CustomListColumnTypes.Boolean;
                else if (dataType == typeof(DateTime))
                    listColumnType = CustomListColumnTypes.String; // Store dates as strings for consistency
                else
                    listColumnType = DataTypeHelper.IsNumericType(dataType) ? CustomListColumnTypes.Number : CustomListColumnTypes.String;

                cols.Add(new CustomListColumn(columnName, listColumnType));
            }
            }

            return cols;
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            DataTable sqlresult = GetSQLTable(data);

            var items = new CustomListObjectElementCollection();

            // We simply transfer the Datatable object to a CustomListObjectCollection
            foreach (DataRow sqlrow in sqlresult.Rows)
            {
                CustomListObjectElement newitem = new CustomListObjectElement();
                foreach (DataColumn sqlcol in sqlresult.Columns)
                {
                    object value = sqlrow[sqlcol] == DBNull.Value ? DataTypeHelper.GetDefaultValue(sqlcol.DataType) : sqlrow[sqlcol];
                    newitem.Add(sqlcol.ColumnName, DataTypeHelper.GetOrConvertNumericTypeToDouble(sqlcol.DataType, value));
                }
                items.Add(newitem);
            }

            this.Log?.Info(string.Format("MySql extension fetched {0} rows.", items.Count));

            return items;
        }

        protected override CustomListExecuteReturnContext ExecuteFunctionOverride(CustomListData data, CustomListExecuteParameterContext context)
        {
            if (!context.FunctionName.Equals("ExecuteStatement", StringComparison.InvariantCultureIgnoreCase))
            {
                // Loud rather than silent. The previous shape returned an empty
                // context for any unknown name, so a board could call a function
                // that does not exist and see nothing happen.
                throw new DataErrorException($"Function '{context.FunctionName}' is not supported by MySqlCustomList.");
            }

            var ret = new CustomListExecuteReturnContext();

            // using, not con.Close() - the old implementation closed the connection
            // on the success path only, so every failed statement leaked one.
            using (var con = GetConnection(data))
            using (var command = new MySqlCommand(context.Values[0].StringValue, con))
            {
                int rowsAffected = command.ExecuteNonQuery();

                // Console.WriteLine went nowhere: an extension host has no console,
                // so the only record of a write was lost entirely. Log?.Info puts it
                // in the box log, and the text matches the net8.0 build so one grep
                // works against either.
                this.Log?.Info(string.Format("SQL Command executed: {0} rows affected.", rowsAffected));

                // Returned so a script can verify its own write. Without this a
                // board can only learn the outcome by reading the box log, which
                // means an idempotent INSERT cannot tell "wrote a new row" from
                // "matched an existing one".
                ret.Add((double)rowsAffected);
            }

            return ret;
        }

        private DataTable GetSQLTable(CustomListData data)
        {
            data.Properties.TryGetValue("SQLStatement", StringComparison.OrdinalIgnoreCase, out var SQLStatement);

            // The hot path: three datasources on a 10 s reload is 18 calls a
            // minute. The previous shape reached con.Close() only when Fill()
            // succeeded, so once a server started refusing connections every
            // failure leaked one. Measured at a customer site on 2026-08-26:
            // ~357 leaked connections in 90 minutes against a server whose
            // default max_connections is 151.
            DataTable sqlresult = new DataTable();

            using (var con = GetConnection(data))
            using (var command = new MySqlCommand(SQLStatement, con))
            using (var da = new MySqlDataAdapter(command))
            {
                da.Fill(sqlresult);
            }

            return sqlresult;
        }

        private void CheckProperties(CustomListData data)
        {
            data.Properties.TryGetValue("Host", StringComparison.OrdinalIgnoreCase, out var DBServer);
            data.Properties.TryGetValue("Port", StringComparison.OrdinalIgnoreCase, out var Port);
            data.Properties.TryGetValue("Database", StringComparison.OrdinalIgnoreCase, out var DBName);
            data.Properties.TryGetValue("Username", StringComparison.OrdinalIgnoreCase, out var Username);
            data.Properties.TryGetValue("Password", StringComparison.OrdinalIgnoreCase, out var Password);

            if (string.IsNullOrWhiteSpace(DBServer) || string.IsNullOrWhiteSpace(Port) || string.IsNullOrWhiteSpace(DBName) || string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                throw new InvalidOperationException("Invalid properties. Please check carefully!");
            }
        }

        // Timeouts are explicit and short on purpose. With none set, a database
        // that accepts the TCP connection but never finishes the handshake parks
        // the calling thread for the driver default. Observed at a customer site
        // on 2026-08-26: requests hung ~30 s each from 13:38, the host logged
        //   Authentication to host '...' failed. (I/O error occurred.)
        // at 15:08:29 and never spoke again. The Runtime did not report a dead
        // pipe until 04:23 the next morning - 13 hours in which every write was
        // lost. Failing fast turns that into an error the board can see.
        private const uint ConnectTimeoutSeconds = 5;
        private const uint CommandTimeoutSeconds = 15;

        private MySqlConnection GetConnection(CustomListData data)
        {
            data.Properties.TryGetValue("Host", StringComparison.OrdinalIgnoreCase, out var Host);
            data.Properties.TryGetValue("Port", StringComparison.OrdinalIgnoreCase, out var Port);
            data.Properties.TryGetValue("Database", StringComparison.OrdinalIgnoreCase, out var Database);
            data.Properties.TryGetValue("Username", StringComparison.OrdinalIgnoreCase, out var Username);
            data.Properties.TryGetValue("Password", StringComparison.OrdinalIgnoreCase, out var Password);

            // Built through the builder rather than by concatenation. The old
            // form was string.Format($"...") - the interpolation ran first and the
            // result was then parsed for {0} placeholders, so a password
            // containing a brace threw FormatException, and one containing a
            // semicolon silently truncated the connection string. The builder
            // escapes both.
            var builder = new MySqlConnectionStringBuilder
            {
                Server = Host,
                Port = uint.TryParse(Port, out var port) ? port : 3306u,
                UserID = Username,
                Password = Password,
                Database = Database,
                ConnectionTimeout = ConnectTimeoutSeconds,
                DefaultCommandTimeout = CommandTimeoutSeconds,
            };

            MySqlConnection con = new MySqlConnection(builder.ConnectionString);
            con.Open();

            return con;
        }
    }
}
