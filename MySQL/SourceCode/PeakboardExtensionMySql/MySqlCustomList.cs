﻿using System;
using System.Data;
using MySql.Data.MySqlClient;
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
                    new CustomListPropertyDefinition() { Name = "Database", Value = "sys" },
                    new CustomListPropertyDefinition() { Name = "Username", Value = "peakboard" },
                    new CustomListPropertyDefinition() { Name = "Password", Masked = true, Value="" },
                    new CustomListPropertyDefinition() { Name = "SQLStatement", Value = "select * from testtable", EvalParameters = true, MultiLine = true  },
                },
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
            var con = GetConnection(data);
            var command = new MySqlCommand(SQLStatement, con);
            var reader = command.ExecuteReader();
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
                else
                    listColumnType = DataTypeHelper.IsNumericType(dataType) ? CustomListColumnTypes.Number : CustomListColumnTypes.String;

                cols.Add(new CustomListColumn(columnName, listColumnType));
            }

            con.Close();

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
                    newitem.Add(sqlcol.ColumnName, DataTypeHelper.GetOrConvertNumericTypeToDouble(sqlcol.DataType, sqlrow[sqlcol.ColumnName]));
                items.Add(newitem);
            }

            this.Log?.Info(string.Format("MySql extension fetched {0} rows.", items.Count));

            return items;
        }

        private DataTable GetSQLTable(CustomListData data)
        {
            MySqlConnection con = GetConnection(data);
            data.Properties.TryGetValue("SQLStatement", StringComparison.OrdinalIgnoreCase, out var SQLStatement);

            MySqlDataAdapter da = new MySqlDataAdapter(new MySqlCommand(SQLStatement, con));
            DataTable sqlresult = new DataTable();
            da.Fill(sqlresult);
            con.Close();
            da.Dispose();
            return sqlresult;
        }

        private void CheckProperties(CustomListData data)
        {
            data.Properties.TryGetValue("Host", StringComparison.OrdinalIgnoreCase, out var DBServer);
            data.Properties.TryGetValue("Database", StringComparison.OrdinalIgnoreCase, out var DBName);
            data.Properties.TryGetValue("Username", StringComparison.OrdinalIgnoreCase, out var Username);
            data.Properties.TryGetValue("Password", StringComparison.OrdinalIgnoreCase, out var Password);

            if (string.IsNullOrWhiteSpace(DBServer) || string.IsNullOrWhiteSpace(DBName) || string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                throw new InvalidOperationException("Invalid properties. Please check carefully!");
            }
        }

        private MySqlConnection GetConnection(CustomListData data)
        {
            data.Properties.TryGetValue("Host", StringComparison.OrdinalIgnoreCase, out var Host);
            data.Properties.TryGetValue("Database", StringComparison.OrdinalIgnoreCase, out var Database);
            data.Properties.TryGetValue("Username", StringComparison.OrdinalIgnoreCase, out var Username);
            data.Properties.TryGetValue("Password", StringComparison.OrdinalIgnoreCase, out var Password);

            MySqlConnection con = new MySqlConnection(string.Format("server={0};userid={2};password={3};database={1}", Host, Database, Username, Password));
            con.Open();

            return con;
        }
    }
}
