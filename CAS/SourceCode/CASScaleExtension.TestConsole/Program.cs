using Peakboard.ExtensionKit;


namespace CASScaleExtension.TestConsole
{
    internal class Program
    {
        // --- KONFIGURATION ---
        const string BLE_DEVICE_ID = "CAS-BLE"; // Deine MAC
        const string SERIAL_PORT = "COM12";                // <--- HIER ANPASSEN!
        const string SERIAL_BAUD = "9600";
        // ---------------------

        static void Main(string[] args)
        {
            try
            {
                RunMenu().Wait();
            }
            catch (Exception e)
            {
                Console.WriteLine($"CRITICAL ERROR: {e}");
            }

            Console.WriteLine("\nPress any key to exit . . .");
            Console.ReadKey();
        }

        static async Task RunMenu()
        {
            Console.WriteLine("--- CAS Scale Extension Test Console ---");
            Console.WriteLine("Welche Schnittstelle möchtest du testen?");
            Console.WriteLine(" [1] Bluetooth LE (PB2-BLE)");
            Console.WriteLine(" [2] Seriell / RS232 (PB2-Serial)");
            Console.Write("Auswahl: ");

            var key = Console.ReadKey();
            Console.WriteLine();

            if (key.Key == ConsoleKey.D1 || key.Key == ConsoleKey.NumPad1)
            {
                await StartTest("PB2BLE", CreateBleData());
            }
            else if (key.Key == ConsoleKey.D2 || key.Key == ConsoleKey.NumPad2)
            {
                await StartTest("PB2Serial", CreateSerialData());
            }
            else
            {
                Console.WriteLine("Ungültige Auswahl.");
            }
        }

        static CustomListData CreateBleData()
        {
            return new CustomListData
            {
                ListName = "TestWaage_BLE",
                Properties = new KeyValueItemCollection
                {
                    { "DeviceIdentifier", BLE_DEVICE_ID },
                    { "AutoConnect", "false" }
                }
            };
        }

        static CustomListData CreateSerialData()
        {
            return new CustomListData
            {
                ListName = "TestWaage_Serial",
                Properties = new KeyValueItemCollection
                {
                    { "SerialPort", SERIAL_PORT },
                    { "Baudrate", SERIAL_BAUD },
                    { "Parity", "None" },      // Ggf. anpassen (None/Odd/Even)
                    { "DataBits", "8" },      // Ggf. anpassen (7/8)
                    { "StopBits", "One" },
                    { "AutoConnect", "false" }
                }
            };
        }

        static async Task StartTest(string listId, CustomListData data)
        {
            Console.WriteLine($"\nInitialisiere Liste '{listId}'...");

            if (TryGetCustomList(listId, out var customList))
            {
                // Daten prüfen & Setup
                if (TryCheckData(customList, data))
                {
                    Console.WriteLine($"Setup ausgeführt für '{data.ListName}'.");
                    customList.Setup(data);

                    Console.WriteLine("\n--- STEUERUNG ---");
                    Console.WriteLine("[C] Connect (Verbinden)");
                    Console.WriteLine("[W] Get Data (Wiegen)");
                    Console.WriteLine("[Z] Set Zero (Nullstellen)");
                    Console.WriteLine("[T] Tare (Tarieren)");
                    Console.WriteLine("[B] Get Battery (Batterie)");
                    Console.WriteLine("[X] Beenden");
                    Console.WriteLine("-----------------");

                    bool running = true;
                    while (running)
                    {
                        if (Console.KeyAvailable)
                        {
                            var key = Console.ReadKey(true).Key;
                            string funcName = "";

                            switch (key)
                            {
                                case ConsoleKey.C: funcName = "Connect"; break;
                                case ConsoleKey.W: funcName = "GetData"; break;
                                case ConsoleKey.Z: funcName = "SetZero"; break;
                                case ConsoleKey.T: funcName = "Tare"; break;
                                case ConsoleKey.B: funcName = "GetBattery"; break;
                                case ConsoleKey.X: running = false; break;
                            }

                            if (!string.IsNullOrEmpty(funcName))
                            {
                                Console.WriteLine($"\nSende Befehl: {funcName}...");

                                var context = new CustomListExecuteParameterContext
                                {
                                    FunctionName = funcName,
                                    ListName = data.ListName
                                };

                                // Funktion ausführen
                                if (TryExecuteFunction(customList, data, context, out var returnContext))
                                {
                                    Console.WriteLine(" -> Befehl an Extension übergeben.");
                                }
                            }
                        }
                        await Task.Delay(100);
                    }

                    Console.WriteLine("Cleanup...");
                    customList.Cleanup(data);
                }
            }
        }

        #region Helper Methods

        private static bool TryGetCustomList(string id, out ICustomList customList)
        {
            // Namespace-Alias Konflikt vermeiden durch direkten Aufruf:
            var extension = new CASScaleExtension(null);

            var resultDef = extension.GetCustomListDefinitions();

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
            if (result == null) return false;

            var hasErrors = result.HasErrors();
            var hasWarnings = result.HasWarnings();

            if (hasErrors || hasWarnings)
            {
                Console.Write($"Execution ");

                if (hasErrors)
                {
                    Console.WriteLine("failed with error(s):");
                    Console.WriteLine(result.Errors.ToCombinedString(" => "));
                }

                if (result.HasWarnings())
                {
                    Console.WriteLine(hasErrors ? "Additional warning(s):" : "succeeded with warning(s):");
                    Console.WriteLine(result.Warnings.ToCombinedString(" => "));
                }
            }

            return !hasErrors;
        }

        #endregion
    }
}