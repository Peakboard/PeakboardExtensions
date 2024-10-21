using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ESCPOS_NET;
using ESCPOS_NET.Emitters;
using ESCPOS_NET.Utilities;
using Peakboard.ExtensionKit;

namespace POSPrinter
{
    [Serializable]
    class ESCPOSPrintCustomList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = $"ESCPOSPrintCustomList",
                Name = "ESC/POS printing",
                PropertyInputPossible = true,
                PropertyInputDefaults =
                {
                    new CustomListPropertyDefinition { Name = "IP", Value = "127.0.0.1" },
                    new CustomListPropertyDefinition { Name = "Port", Value = "9100" },
                    new CustomListPropertyDefinition { Name = "SerialPortName", Value = "COM3" },
                    new CustomListPropertyDefinition { Name = "BaudRate", Value = "9600" },
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
                                Description = "Text to print (may contain style commands)",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.String
                            },
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "params",
                                Description = "Parameters to replace in text (format #{param1}#)",
                                Optional = true,
                                Type = CustomListFunctionParameterTypes.Collection
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
                var text = default(string);

                if (context.Values.Count > 0)
                {
                    text = context.Values[0].StringValue;
                }

                if (string.IsNullOrEmpty(text))
                {
                    Log?.Warning("Function print: No text passed to print");
                    return false;
                }

                var pars = default(CustomListObjectElementCollection);

                if (context.Values.Count > 1)
                {
                    pars = context.Values[1].CollectionValue;
                }

                var pdic = new Dictionary<string, string>();

                if (pars != null)
                {
                    foreach (var item in pars)
                    {
                        foreach (var kvp in item)
                        {
                            if (pdic.ContainsKey(kvp.Key))
                            {
                                pdic[kvp.Key] = kvp.Value as string;
                            }
                            else
                            {
                                pdic.Add(kvp.Key, kvp.Value as string);
                            }
                        }
                    }
                }

                /*
                var text = @"
~(CentralAlign)~
Kopfzeile mit #[param1]#
~(Style:Bold)~
Zeile 2
Zeile 3
~(LeftAlign)~
~(Style:DoubleHeight)~
Zeile 4
~(FullCutAfterFeed:1)~
";
                */

                var emitter = new EPSON();
                var barrays = ESCPOSHelper.CreateCommands(text, emitter, pdic);

                var ip = data.Properties["IP"];
                var port = int.TryParse(data.Properties["Port"], out var p) ? p : 9100;
                var serialPortName = data.Properties["SerialPortName"];
                var baudRate = int.TryParse(data.Properties["BaudRate"], out var r) ? r : 9600;

                // TODO: May check values in more detail?

                if (!string.IsNullOrEmpty(ip) && port > 0)
                {
                    var printer = new ImmediateNetworkPrinter(
                        new ImmediateNetworkPrinterSettings() { ConnectionString = $"{ip}:{port}" }
                    );
                    printer
                        .WriteAsync(ByteSplicer.Combine(barrays.ToArray()))
                        .GetAwaiter()
                        .GetResult();
                }
                else if (!string.IsNullOrEmpty(serialPortName) && baudRate > 0)
                {
                    using (
                        var printer = new SerialPrinter(
                            portName: serialPortName,
                            baudRate: baudRate
                        )
                    )
                    {
                        byte[] printData = ByteSplicer.Combine(barrays.ToArray());
                        double timer = ((printData.Length * 8) / 100);
                        if (timer < 100)
                            timer = 100;
                        printer.Write(printData);
                        Task.Delay((int)timer).Wait();
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Log?.Error("Print function failed.", e);
                return false;
            }
        }

        #endregion
    }
}
