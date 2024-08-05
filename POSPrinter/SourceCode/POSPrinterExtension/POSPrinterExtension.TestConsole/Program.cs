using System;
using System.Diagnostics;
using Peakboard.ExtensionKit;

namespace POSPrinter.TestConsole
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                //PrintWithESCPOS();
                PrintWithZPL();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            if (Debugger.IsAttached)
            {
                Console.WriteLine("Press any key to continue . . .");
                Console.ReadKey();
            }
        }

        static void PrintWithESCPOS()
        {
            if (TryGetCustomList("ESCPOSPrintCustomList", out var customList))
            {
                var data = new CustomListData
                {
                    ListName = "ESCPOS1",
                    Properties = { { "SerialPortName", "COM4" }, { "BaudRate", "9600" } }
                    //Properties = { { "IP", "192.168.42.154" }, { "Port", "9100" } } // iOS App Thermal Printer
                };

                if (TryCheckData(customList, data))
                {
                    var context = new CustomListExecuteParameterContext { FunctionName = "Print", ListName = data.ListName };
                    var text = @"
~(CentralAlign)~
Kopfzeile mit #[param1]#
~(Style:Bold)~
Zeile 2
Zeile 3
~(LeftAlign)~
~(Style:DoubleHeight)~
~(Barcode:CODE128,1234567890)~
Zeile 4
~(Style:Bold,Italic,DoubleHeight)~
Zeile 5
~(FullCutAfterFeed:1)~
";
                    context.Values.Add(CustomListExecuteFunctionValue.From(text));

                    var pdic = new CustomListObjectElementCollection
                    {
                        { "param1", "ABC" },
                    };

                    context.Values.Add(CustomListExecuteFunctionValue.From(pdic));

                    if (TryExecuteFunction(customList, data, context, out var returnContext))
                    {
                        var value = returnContext.Values[0].GetValue();

                        Console.WriteLine($"CustomList function Print returned value: {value}.");
                    }
                }
            }
        }

        static void PrintWithZPL()
        {
            if (TryGetCustomList("ZPLPrintCustomList", out var customList))
            {
                var data = new CustomListData
                {
                    ListName = "ZPL1",
                    Properties = { { "IP", "127.0.0.1" }, { "Port", "9100" } }
                };

                if (TryCheckData(customList, data))
                {
                    if (TryGetColumns(customList, data, out var columns))
                    {
                        if (TryGetItems(customList, data, out var items))
                        {
                            if (items == null || items.Count == 0)
                            {
                                Console.WriteLine($"CustomList not returned any data.");
                            }
                            else
                            {
                                Console.WriteLine($"CustomList returned items (first value: {items[0]["Dummy"]}).");
                            }
                        }
                    }

                    var context = new CustomListExecuteParameterContext { FunctionName = "Print", ListName = data.ListName };

                    var zplData = @"^XA^MMP^PW300^LS0^LT0^FT10,60^APN,30,30^FH\^FDI LOVE PEAKBOARD^FS^XZ";
                    
                    context.Values.Add(CustomListExecuteFunctionValue.From(zplData));

                    if (TryExecuteFunction(customList, data, context, out var returnContext))
                    {
                        var value = returnContext.Values[0].GetValue();

                        Console.WriteLine($"CustomList function Print returned value: {value}.");
                    }
                }
            }
        }

        #region Helper Methods

        private static bool TryGetCustomList(string id, out ICustomList customList)
        {
            var extension = new POSPrinterExtension(null);

            var resultDef = extension.GetCustomListDefinitions(); // Initialize system

            if (TryHandleResultOrIgnore(resultDef))
            {
                var result = extension.GetCustomList(id);

                customList = result.Value;

                return TryHandleResultOrIgnore(result);
            }

            customList = null;
            return false;
        }

        private static bool TryCheckData(ICustomList customList, CustomListData data)
        {
            var result = customList.CheckData(data);

            return TryHandleResultOrIgnore(result);
        }

        private static bool TryGetColumns(ICustomList customList, CustomListData data, out CustomListColumnCollection columns)
        {
            var result = customList.GetColumns(data);

            columns = result.Value;

            return TryHandleResultOrIgnore(result);
        }

        private static bool TryGetItems(ICustomList customList, CustomListData data, out CustomListObjectElementCollection items)
        {
            var result = customList.GetItems(data);

            items = result.Value;

            return TryHandleResultOrIgnore(result);
        }

        private static bool TryExecuteFunction(ICustomList customList, CustomListData data, CustomListExecuteParameterContext context, out CustomListExecuteReturnContext returnContext)
        {
            var result = customList.ExecuteFunction(data, context);

            returnContext = result.Value;

            return TryHandleResultOrIgnore(result);
        }

        private static bool TryHandleResultOrIgnore(Result result)
        {
            var hasErrors = result.HasErrors();
            var hasWarnings = result.HasWarnings();

            if (hasErrors || hasWarnings)
            {
                Console.Write($"CustomList execution ");

                if (hasErrors)
                {
                    Console.WriteLine("failed with error(s):");

                    Console.WriteLine(result.Errors.ToCombinedString(" => "));
                }

                if (result.HasWarnings())
                {
                    if (hasErrors)
                    {
                        Console.WriteLine("Additional warning(s):");
                    }
                    else
                    {
                        Console.WriteLine("succeeded with warning(s):");
                    }

                    Console.WriteLine(result.Warnings.ToCombinedString(" => "));
                }
            }

            return !hasErrors;
        }

        #endregion
    }
}