using Peakboard.ExtensionKit;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeakboardExtensionWerma
{
    [Serializable]
    [CustomListIcon("PeakboardExtensionWerma.werma.png")]
    class WermaTimestampList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = $"WermaTimestampList",
                Name = "Werma Timestamp List",
                Description = "Returns historical data of the selectable light",
                PropertyInputPossible = true,
                PropertyInputDefaults =
                {
                    new CustomListPropertyDefinition() { Name = "Host", Value = @"LAPTOP-2FRPHOE7\WERMAWIN" },
                    new CustomListPropertyDefinition() { Name = "Database", Value = "WERMAWIN" },
                    new CustomListPropertyDefinition() { Name = "Username", Value = "WERMAWIN" },
                    new CustomListPropertyDefinition() { Name = "Password", Masked = true, Value="Tyz19$lx50WsR3Ed7m" },
                    new CustomListPropertyDefinition() { Name = "LightID", Value = "009E40" },
                    new CustomListPropertyDefinition() { Name = "Max_Rows", Value = "1000" },
                },
            };
        }

        protected override void CheckDataOverride(CustomListData data)
        {
            CheckProperties(data);
        }


        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            return new CustomListColumnCollection
            {
                new CustomListColumn("Timestamp", CustomListColumnTypes.String),
                new CustomListColumn("Channel1", CustomListColumnTypes.String),
                new CustomListColumn("Channel2", CustomListColumnTypes.String),
                new CustomListColumn("Channel3", CustomListColumnTypes.String),
                new CustomListColumn("Channel4", CustomListColumnTypes.String),
            };
        }

        

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            var items = new CustomListObjectElementCollection();

            data.Properties.TryGetValue("LightID", StringComparison.OrdinalIgnoreCase, out var macID);

            if ( string.IsNullOrWhiteSpace(macID))
            {
                throw new InvalidOperationException("Invalid LightID property. Please check carefully!");
            }

            data.Properties.TryGetValue("Max_Rows", StringComparison.OrdinalIgnoreCase, out var maxRows);

            if (string.IsNullOrWhiteSpace(maxRows))
            {
                throw new InvalidOperationException("Invalid Max_Rows property. Please check carefully!");
            }

            string commandText= $"SELECT TOP {maxRows} CONVERT(varchar, datStart,103) +' '+ CONVERT(varchar,datStart,8) AS Timestamp, Channel1, Channel2, Channel3, Channel4 FROM dbo.slaveData WHERE slaveId=(SELECT id FROM dbo.slaveDevice WHERE MacId='{macID}') ORDER BY id DESC";

            DataTable sqlresult = GetSQLTable(data, commandText);

            foreach (DataRow sqlrow in sqlresult.Rows)
            {
                CustomListObjectElement newitem = new CustomListObjectElement();
                foreach (DataColumn sqlcol in sqlresult.Columns)
                {
                    string cs = sqlrow[sqlcol.ColumnName].ToString();

                    if (cs == "0")
                    {
                        cs = "Off";
                    }
                    else if (cs == "1" || cs == "2" || cs == "3" || cs == "16" || cs == "17" || cs == "18" || cs == "19")
                    {
                        cs = "On";
                    }
                    else if (cs == "20" || cs == "21" || cs == "22" || cs == "23")
                    {
                        cs = "Blinking";
                    }

                    newitem.Add(sqlcol.ColumnName, cs);
                }
                items.Add(newitem);
            }
                
            this.Log?.Info(string.Format("SQL Server extension fetched {0} rows.", items.Count));

            return items;
        }

        private DataTable GetSQLTable(CustomListData data, string commandText)
        {
            SqlConnection con = GetConnection(data);

            SqlDataAdapter da = new SqlDataAdapter(new SqlCommand(commandText, con));
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

        private SqlConnection GetConnection(CustomListData data)
        {
            data.Properties.TryGetValue("Host", StringComparison.OrdinalIgnoreCase, out var Host);
            data.Properties.TryGetValue("Database", StringComparison.OrdinalIgnoreCase, out var Database);
            data.Properties.TryGetValue("Username", StringComparison.OrdinalIgnoreCase, out var Username);
            data.Properties.TryGetValue("Password", StringComparison.OrdinalIgnoreCase, out var Password);

            SqlConnection con = new SqlConnection(string.Format("server={0};database={1};user id={2};password={3}", Host, Database, Username, Password));
            con.Open();

            return con;
        }
    }
}
