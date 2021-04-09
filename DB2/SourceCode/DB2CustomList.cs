using System;
using Peakboard.ExtensionKit;
using System.Data;
using IBM.Data.DB2;

namespace PeakboardExtensionDB2
{
    [Serializable]
    [CustomListIcon("PeakboardExtensionDB2.DB2.png")]
    class DB2CustomList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = $"DB2CustomList",
                Name = "DB2 List",
                Description = "Returns DB2 data",
                PropertyInputPossible = true,
                PropertyInputDefaults = {
                    new CustomListPropertyDefinition() { Name = "Host", Value = "xxx.compute.amazonaws.com" },
                    new CustomListPropertyDefinition() { Name = "Database", Value = "sys" },
                    new CustomListPropertyDefinition() { Name = "Username", Value = "peakboard" },
                    new CustomListPropertyDefinition() { Name = "Password", Value = "", Masked = true},
                    new CustomListPropertyDefinition() { Name = "SQL Statement", Value = "select * from testtable", EvalParameters = true, MultiLine = true },
                },
            };
        }

        protected override void CheckDataOverride(CustomListData data)
        {
            CheckProperties(data);
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            data.Properties.TryGetValue("SQL Statement", StringComparison.OrdinalIgnoreCase, out var SQLStatement);

            var cols = new CustomListColumnCollection();
            var con = GetConnection(data);
            var command = new DB2Command(SQLStatement, con);
            var reader = command.ExecuteReader();
            var schemaTable = reader.GetSchemaTable();

            foreach (DataRow db2col in schemaTable.Rows)
            {
                var columnName = (string)db2col["ColumnName"];
                var dataType = (Type)db2col["DataType"];
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
            DataTable db2result = GetDB2Table(data);

            var items = new CustomListObjectElementCollection();

            // Simply transfer the Datatable object to a CustomListObjectCollection
            foreach (DataRow db2row in db2result.Rows)
            {
                CustomListObjectElement newitem = new CustomListObjectElement();
                foreach (DataColumn db2col in db2result.Columns)
                {
                    newitem.Add(db2col.ColumnName, db2row[db2col.ColumnName]);
                }
                items.Add(newitem);
            }

            Log?.Info(string.Format("DB2 extension fetched {0} rows.", items.Count));

            return items;
        }

        private DataTable GetDB2Table(CustomListData data)
        {
            DB2Connection con = GetConnection(data);
            data.Properties.TryGetValue("SQL Statement", StringComparison.OrdinalIgnoreCase, out var SQLStatement);

            DB2DataAdapter da = new DB2DataAdapter(new DB2Command(SQLStatement, con));
            DataTable db2result = new DataTable();
            da.Fill(db2result);
            con.Close();
            da.Dispose();
            return db2result;
        }

        private void CheckProperties(CustomListData data)
        {
            data.Properties.TryGetValue("Host", StringComparison.OrdinalIgnoreCase, out var Host);
            data.Properties.TryGetValue("Database", StringComparison.OrdinalIgnoreCase, out var Database);
            data.Properties.TryGetValue("Username", StringComparison.OrdinalIgnoreCase, out var Username);
            data.Properties.TryGetValue("Password", StringComparison.OrdinalIgnoreCase, out var Password);

            if (string.IsNullOrWhiteSpace(Host) || string.IsNullOrWhiteSpace(Database) || string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                throw new InvalidOperationException("Invalid properties. Please check carefully!");
            }
        }

        private DB2Connection GetConnection(CustomListData data)
        {
            data.Properties.TryGetValue("Host", StringComparison.OrdinalIgnoreCase, out var Host);
            data.Properties.TryGetValue("Database", StringComparison.OrdinalIgnoreCase, out var Database);
            data.Properties.TryGetValue("Username", StringComparison.OrdinalIgnoreCase, out var Username);
            data.Properties.TryGetValue("Password", StringComparison.OrdinalIgnoreCase, out var Password);
            
            string Db2ConnectionString = string.Format("server={0};database={1};uid={2};pwd={3};", Host, Database, Username, Password);
            DB2Connection Db2Connection = new DB2Connection(Db2ConnectionString);
            Db2Connection.Open();

            return Db2Connection;
        }
    }
}
