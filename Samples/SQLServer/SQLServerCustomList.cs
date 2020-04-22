using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Peakboard.ExtensionKit;
using System.Data.SqlClient;
using System.Data;
using System.Windows.Forms;

namespace PeakboardExtensionsSQLServer
{
    [Serializable]
    class SQLServerCustomList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = $"SQLServerCustomList",
                Name = "SQLServer List",
                Description = "Returns SQL Server data",
                PropertyInputPossible = true,
                PropertyInputDefaults = {
                    new CustomListPropertyDefinition() { Name = "DBServer", Value = "sunshine.database.windows.net" },
                    new CustomListPropertyDefinition() { Name = "DBName", Value = "SunshineDB" },
                    new CustomListPropertyDefinition() { Name = "Username", Value = "MySQLAccess" },
                    new CustomListPropertyDefinition() { Name = "Password", Masked = true },
                    new CustomListPropertyDefinition() { Name = "SQLStatement", Value = "Select * from Auftrag" },
                },
            };
        }

        protected override void CheckDataOverride(CustomListData data)
        {
            CheckProperties(data);
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            CustomListColumnCollection cols = new CustomListColumnCollection();
            DataTable sqlresult = GetSQLTable(data);

            foreach(DataColumn sqlcol in sqlresult.Columns)
            {
                CustomListColumn newcol = new CustomListColumn(sqlcol.ColumnName);

                // We convert the SQdL type to one of the three Peakboard types (string, number or boolean)
                if (sqlcol.DataType.ToString().Equals("System.Int32"))
                    newcol.Type = CustomListColumnTypes.Number;
                else if (sqlcol.DataType.ToString().Equals("System.Boolean"))
                    newcol.Type = CustomListColumnTypes.Boolean;
                else
                    newcol.Type = CustomListColumnTypes.String;

                cols.Add(newcol);
            }

            return cols;
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            DataTable sqlresult = GetSQLTable(data);

            var items = new CustomListObjectElementCollection();

            // We simply transfer the Datatable object from SQL Server to a CustomListObjectCollection
            foreach(DataRow sqlrow in sqlresult.Rows)
            {
                CustomListObjectElement newitem = new CustomListObjectElement();
                foreach(DataColumn sqlcol in sqlresult.Columns)
                {
                    newitem.Add(sqlcol.ColumnName, sqlrow[sqlcol.ColumnName]);
                }
                items.Add(newitem);
            }

            this.Log?.Info(string.Format("SQL Server extension fetched {0} rows.", items.Count));
            
            return items;
        }

        private DataTable GetSQLTable(CustomListData data)
        {
            SqlConnection con = GetConnection(data);
            data.Properties.TryGetValue("SQLStatement", StringComparison.OrdinalIgnoreCase, out var SQLStatement);

            SqlDataAdapter da = new SqlDataAdapter(new SqlCommand(SQLStatement, con));
            DataTable sqlresult = new DataTable();
            da.Fill(sqlresult);
            con.Close();
            da.Dispose();
            return sqlresult;
        }

        private void CheckProperties(CustomListData data)
        {
            data.Properties.TryGetValue("DBServer", StringComparison.OrdinalIgnoreCase, out var DBServer);
            data.Properties.TryGetValue("DBName", StringComparison.OrdinalIgnoreCase, out var DBName);
            data.Properties.TryGetValue("Username", StringComparison.OrdinalIgnoreCase, out var Username);
            data.Properties.TryGetValue("Password", StringComparison.OrdinalIgnoreCase, out var Password);

            if (string.IsNullOrWhiteSpace(DBServer) || string.IsNullOrWhiteSpace(DBName) || string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                throw new InvalidOperationException("Invalid properties. Please check carefully!");
            }
        }

        private SqlConnection GetConnection(CustomListData data)
        {
            data.Properties.TryGetValue("DBServer", StringComparison.OrdinalIgnoreCase, out var DBServer);
            data.Properties.TryGetValue("DBName", StringComparison.OrdinalIgnoreCase, out var DBName);
            data.Properties.TryGetValue("Username", StringComparison.OrdinalIgnoreCase, out var Username);
            data.Properties.TryGetValue("Password", StringComparison.OrdinalIgnoreCase, out var Password);

            SqlConnection con = new SqlConnection(string.Format("Server={0};Database={1};User Id={2};Password={3};", DBServer, DBName, Username, Password));
            con.Open();

            return con;
        }
    }
}
