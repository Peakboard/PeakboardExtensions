using System;
using System.IO;
using System.IO.Ports;
using System.Net.Sockets;
using Peakboard.ExtensionKit;

namespace POSPrinter
{
    [Serializable]
    [CustomListIcon("POSPrinterExtension.pb_datasource_pos_printer.png")]
    class ZPLPrintCustomList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = $"ZPLPrintCustomList",
                Name = "ZPL printing",
                PropertyInputPossible = true,
                PropertyInputDefaults =
                {
                    new CustomListPropertyDefinition { Name = "IP", Value = "127.0.0.1" },
                    new CustomListPropertyDefinition { Name = "Port", Value = "9100" },
                    new CustomListPropertyDefinition { Name = "SerialPortName", Value = "COM3" },
                    new CustomListPropertyDefinition { Name = "BaudRate", Value = "9600" }
                },
                Functions =
                {
                    new CustomListFunctionDefinition()
                    {
                        Name = "print",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "text",
                                Description = "Text to print (in ZPL format)",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.String
                            }
                        },
                        ReturnParameters = new CustomListFunctionReturnParameterDefinitionCollection
                        {
                            new CustomListFunctionReturnParameterDefinition
                            {
                                Name = "retval",
                                Description = "Returns if the printing was successful",
                                Type = CustomListFunctionParameterTypes.Boolean
                            }
                        }
                    }
                }
            };
        }

        protected override CustomListExecuteReturnContext ExecuteFunctionOverride(
            CustomListData data,
            CustomListExecuteParameterContext context
        )
        {
            //Log?.Verbose($"ExecuteFunctionOverride for CustomList '{data.ListName ?? "?"}' executing");

            if (context.TryExecute("Print", data, PrintFunction, out var returnContext))
            {
                return returnContext;
            }

            // Ignore by not doing anything OR throw exception to return error.
            throw new DataErrorException("Function is not supported in this version.");
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            return new CustomListColumnCollection
            {
                new CustomListColumn("Dummy", CustomListColumnTypes.Boolean)
            };
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            return new CustomListObjectElementCollection
            {
                new CustomListObjectElement { { "Dummy", true } }
            };
        }

        #region Functions

        protected bool PrintFunction(CustomListData data, CustomListExecuteParameterContext context)
        {
            Log?.Verbose($"Function 'Print' for CustomList '{data.ListName ?? "?"}' executing");

            try
            {
                var ip = data.Properties["IP"];
                var port = int.TryParse(data.Properties["Port"], out var p) ? p : 9100;
                var serialPortName = data.Properties["SerialPortName"];
                var baudRate = int.TryParse(data.Properties["BaudRate"], out var r) ? r : 9600;

                var zplData = default(string);

                if (context.Values.Count > 0)
                {
                    zplData = context.Values[0].StringValue;
                }

                if (string.IsNullOrEmpty(zplData))
                {
                    Log?.Warning("Function print: No text passed to print");
                    return false;
                }

                if (!string.IsNullOrEmpty(ip) && port > 0)
                {
                    try
                    {
                        var tcpClient = new TcpClient();
                        tcpClient.Connect(ip, port);
                        var writer = new StreamWriter(tcpClient.GetStream());
                        writer.Write(zplData);
                        writer.Flush();
                        writer.Close();
                        tcpClient.Close();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        Log?.Warning("Network not working");
                        return false;
                    }
                }
                else if (!string.IsNullOrEmpty(serialPortName) && baudRate > 0)
                {
                    using (SerialPort serialPort = new SerialPort(serialPortName, baudRate))
                    {
                        try
                        {
                            serialPort.Parity = Parity.None;
                            serialPort.DataBits = 8;
                            serialPort.StopBits = StopBits.One;
                            serialPort.Handshake = Handshake.None;

                            serialPort.ReadTimeout = -1;
                            serialPort.WriteTimeout = -1;

                            serialPort.Open();
                            serialPort.WriteLine(zplData);
                            serialPort.Close();
                            return true;
                        }
                        catch (Exception ex)
                        {
                            Log?.Warning("SerialPort not working");
                            return false;
                        }
                    }
                }
                return false;
            }
            catch (Exception e)
            {
                Log?.Error("Function print failed:", e);
                return false;
            }
        }

        #endregion
    }
}
