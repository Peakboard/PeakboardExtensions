using System;
using System.IO;
using System.Net.Sockets;
using Peakboard.ExtensionKit;

namespace POSPrinter
{
    [Serializable]
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
                    new CustomListPropertyDefinition { Name = "Port", Value = "9100" }
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

        protected override CustomListExecuteReturnContext ExecuteFunctionOverride(CustomListData data, CustomListExecuteParameterContext context)
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
                if (!data.Properties.TryGetValue("IP", StringComparison.OrdinalIgnoreCase, out var ip))
                {
                    throw new DataErrorException("IP value must be defined.");
                }

                if (!data.Properties.TryGetValue("Port", StringComparison.OrdinalIgnoreCase, out var portString))
                {
                    throw new DataErrorException("Port value must be defined.");
                }

                if (!int.TryParse(portString, out var port))
                {
                    throw new DataErrorException("Port value is not valid.");
                }

                //var zplData = "^XA^MMP^PW300^LS0^LT0^FT10,60^APN,30,30^FH\\^FDSAMPLE TEXT^FS^XZ";
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

                var tcpClient = new TcpClient();
                tcpClient.Connect(ip, port);
                var writer = new StreamWriter(tcpClient.GetStream());
                writer.Write(zplData);
                writer.Flush();
                writer.Close();
                tcpClient.Close();
                return true;
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