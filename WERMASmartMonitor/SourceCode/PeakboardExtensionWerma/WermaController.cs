using Peakboard.ExtensionKit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace PeakboardExtensionWerma
{
    [Serializable]
    [CustomListIcon("PeakboardExtensionWerma.werma.png")]
    public class WermaController : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = $"WermaController",
                Name = "Werma Controller",
                Description = "Returns Werma Data",
                PropertyInputPossible = true,
                PropertyInputDefaults = 
                {
                    new CustomListPropertyDefinition() { Name = "Host", Value = @"LAPTOP-2FRPHOE7\WERMAWIN" },
                    new CustomListPropertyDefinition() { Name = "Database", Value = "WERMAWIN" },
                    new CustomListPropertyDefinition() { Name = "Username", Value = "WERMAWIN" },
                    new CustomListPropertyDefinition() { Name = "Password", Masked = true, Value="Tyz19$lx50WsR3Ed7m" },
                },
                Functions = new CustomListFunctionDefinitionCollection
                {
                    new CustomListFunctionDefinition
                    {
                        Name = "switchstate",
                        Description = "Switches a channel Off, On or Blinking",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "macid",
                                Type = CustomListFunctionParameterTypes.String,
                                Optional = false,
                                Description = "The ID of your signal light (Example : 009E40)"
                            },
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "nameofchannel",
                                Type = CustomListFunctionParameterTypes.Number,
                                Optional = false,
                                Description = "The name of the channel (Possible values : 1/2/3/4)"
                            },
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "state",
                                Type = CustomListFunctionParameterTypes.String,
                                Optional = false,
                                Description = "The state you want to perform on the selected channel (Possible values : Off/On/Blinking)"
                            },
                        },
                    },
                }
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
                new CustomListColumn("ID", CustomListColumnTypes.String),
                new CustomListColumn("Name", CustomListColumnTypes.String),
                new CustomListColumn("Channel1", CustomListColumnTypes.String),
                new CustomListColumn("Channel2", CustomListColumnTypes.String),
                new CustomListColumn("Channel3", CustomListColumnTypes.String),
                new CustomListColumn("Channel4", CustomListColumnTypes.String),
            };
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            string commandText1 = "SELECT id FROM dbo.slaveDevice ORDER BY macId";
            DataTable sqlresult1 = GetSQLTable(data, commandText1);

            var items = new CustomListObjectElementCollection();

            foreach (DataRow sqlrow1 in sqlresult1.Rows)
            {
                foreach (DataColumn sqlcol1 in sqlresult1.Columns)
                {
                    string deviceSlaveId = sqlrow1[sqlcol1.ColumnName].ToString();

                    string commandText2 = $"SELECT MacId as ID, Name, Channel1, Channel2, Channel3, Channel4 FROM dbo.slaveData, dbo.slaveDevice WHERE dbo.slaveData.slaveId = dbo.slaveDevice.id AND dbo.slaveData.id = (SELECT MAX(id) FROM dbo.slaveData WHERE slaveId = {deviceSlaveId})";
                    DataTable sqlresult2 = GetSQLTable(data, commandText2);

                    foreach (DataRow sqlrow2 in sqlresult2.Rows)
                    {
                        CustomListObjectElement newitem = new CustomListObjectElement();
                        foreach (DataColumn sqlcol2 in sqlresult2.Columns)
                        {
                            string cs = sqlrow2[sqlcol2.ColumnName].ToString();

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
                            
                            newitem.Add(sqlcol2.ColumnName, cs);
                        }
                        items.Add(newitem);
                    }
                }
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

        protected override CustomListExecuteReturnContext ExecuteFunctionOverride(CustomListData data, CustomListExecuteParameterContext context)
        {
            var returnContext = default(CustomListExecuteReturnContext);

            if (context.FunctionName.Equals("switchstate", StringComparison.InvariantCultureIgnoreCase))
            {
                string macID = context.Values[0].StringValue;
                string channelName = context.Values[1].StringValue;
                string action = context.Values[2].StringValue;

                this.Log?.Info(string.Format("nameofchannel: {0} -> switchlighton", channelName));

                StartCommand(channelName,action, macID);
            }
            else
            {
                throw new DataErrorException("Function is not supported in this version.");
            }

            return returnContext;
        }

        protected void StartCommand(string channel, string action, string macID)
        {
            if(action.ToLower()=="off")
            {
                action = "0";
            }
            else if (action.ToLower() == "on")
            {
                action = "1";
            }
            else if (action.ToLower() == "blinking")
            {
                action = "2";
            }

            string command = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\..\Local\Peakboard\Extensions\Werma\WermaUtilities\WIN-CLI.exe /switchcontrol " + "\"macid:"+ macID + "\" " + channel + " " + action;

            this.Log?.Info(string.Format(command));

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/K " + command;
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;

            Process process = new Process();
            process.StartInfo = startInfo;

            process.Start();
            process.WaitForExit(10000);
            process.Kill();
        }

    }
}
