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
                PrintWithESCPOS();
                //PrintWithZPL();
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
                    //Properties = { { "IP", "192.168.178.48" }, { "Port", "9100" } } // iOS App 'Virtual Thermal Printer' (DE) from Pascal Kimmel
                };

                if (TryCheckData(customList, data))
                {
                    var context = new CustomListExecuteParameterContext
                    {
                        FunctionName = "Print",
                        ListName = data.ListName
                    };
                    var text =
                        @"~(SetBarWidth:Thin)~
~(SetBarcodeHeightInDots:150)~
~(SetBarLabelPosition:Both)~
~(Barcode:CODE128,Hallo45689)~

~(Style:None)~
~(FullCutAfterFeed:25)~
";
                    context.Values.Add(CustomListExecuteFunctionValue.From(text));

                    var pdic = new CustomListObjectElementCollection { { "param1", "ABC" }, };

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
                    Properties = { { "IP", "192.168.42.136" }, { "Port", "9100" } } // Virtual ZPL Printer
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
                                Console.WriteLine(
                                    $"CustomList returned items (first value: {items[0]["Dummy"]})."
                                );
                            }
                        }
                    }

                    var context = new CustomListExecuteParameterContext
                    {
                        FunctionName = "Print",
                        ListName = data.ListName
                    };

                    var zplData =
                        @"SM10,21 SS3 SD20 SW832 SOT CS0,0 BD18,14,798,164,O T400,62,4,2,2,0,0,R,B,'BIXOLON' T65,98,3,1,1,0,0,R,B,'BIXOLON Label' T20,276,3,1,1,1,0,N,N,'  BIXOLON' T20,306,3,1,1,1,0,N,N,'  Yeongtong Dong' T20,336,3,1,1,1,0,N,N,'  Sowon City,South Korea' T22,218,4,1,1,0,0,N,B,'SHIP TO:' BD18,410,784,415,O BD553,197,558,413,O B169,458,0,4,8,137,0,0,0,'*1234567890*' T26,421,1,1,1,0,0,N,N,'POSTAL CODE:' BD18,616,784,621,O BD20,781,786,786,O T503,798,1,1,1,0,0,N,N,'DESTINATION:' T42,841,5,1,1,0,0,N,B,'30 Kg' BD18,928,784,933,O T25,798,1,1,1,0,0,N,N,'WEIGHT:' T259,798,1,1,1,0,0,N,N,'DELIVERY NO:' T23,630,1,1,1,0,0,N,N,'AWB:' BD241,783,246,932,O BD486,784,491,933,O T274,841,5,1,1,0,0,N,B,'425518' T104,627,3,1,1,0,0,N,N,'8741493121' T565,841,5,1,1,0,0,N,B,'ICN' B1127,672,4,4,8,90,0,0,0,'8741493121' B2560,180,M,0,'999,840,06810,7317,THIS IS A TEST OF MODE 0 STRUCTURED CARRIER MESSAGE ENCODING. THIS IS AN 84 CHAR MSG' B280,960,P,30,10,0,0,0,1,3,14,0,'BIXOLON Label Printer, This is Test Printing.' P1";

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

        private static bool TryGetColumns(
            ICustomList customList,
            CustomListData data,
            out CustomListColumnCollection columns
        )
        {
            var result = customList.GetColumns(data);

            columns = result.Value;

            return TryHandleResultOrIgnore(result);
        }

        private static bool TryGetItems(
            ICustomList customList,
            CustomListData data,
            out CustomListObjectElementCollection items
        )
        {
            var result = customList.GetItems(data);

            items = result.Value;

            return TryHandleResultOrIgnore(result);
        }

        private static bool TryExecuteFunction(
            ICustomList customList,
            CustomListData data,
            CustomListExecuteParameterContext context,
            out CustomListExecuteReturnContext returnContext
        )
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
