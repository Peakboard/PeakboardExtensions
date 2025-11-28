using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Text.RegularExpressions;
using Peakboard.ExtensionKit;

namespace SerialBarcodeScanner.CustomLists
{
    [Serializable]
    [CustomListIcon("SerialBarcodeScanner.pb_datasource_barcode.png")]
    public class SerialBarcodeScannerList : CustomListBase
    {
        private readonly Dictionary<string, SerialPort> _listName2ScannerMap = [];
        private SerialPort mySerialPort = null;
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "ScanData",
                Name = "Scanner Data",
                Description = "Push Barcode data",
                PropertyInputPossible = true,
                SupportsPushOnly = true,
                PropertyInputDefaults = {
                    new CustomListPropertyDefinition() { Name = "SerialPort", Value = "COM8" },
                    new CustomListPropertyDefinition() { Name = "Baudrate", Value = "9600" },
                    new CustomListPropertyDefinition() { Name = "Parity", SelectableValues = ["None", "Odd", "Even", "Mark", "Space"], Value = "None"},
                    new CustomListPropertyDefinition() { Name = "StopBits", SelectableValues = ["None", "One", "Two", "OnePointFive"], Value = "One"},
                    new CustomListPropertyDefinition() { Name = "DataBits", SelectableValues = ["8", "7", "6", "5"], Value = "8"}
                },                
                Functions = new CustomListFunctionDefinitionCollection
                {
                    new CustomListFunctionDefinition()
                    {
                        Name = "SendSerialCommand",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "Command",
                                Description = "Command to send",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.String
                            }
                        },
                    }
                }
            };
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            Log?.Verbose("SerialBarcodeScannerList.GetColumnsOverride");

            return new CustomListColumnCollection
            {
                new CustomListColumn("Data", CustomListColumnTypes.String)
            };
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            Log?.Verbose("SerialBarcodeScannerList.GetItemsOverride");

            return new CustomListObjectElementCollection();
        }

        protected override void SetupOverride(CustomListData data)
        {
            Log?.Verbose("SerialBarcodeScannerList.Setup");

            if (!_listName2ScannerMap.ContainsKey(data.ListName))
            {

                data.Properties.TryGetValue("SerialPort", StringComparison.OrdinalIgnoreCase, out var SerialPort);
                data.Properties.TryGetValue("Baudrate", StringComparison.OrdinalIgnoreCase, out var Baudrate);
                data.Properties.TryGetValue("Parity", StringComparison.OrdinalIgnoreCase, out var ParityStr);
                data.Properties.TryGetValue("StopBits", StringComparison.OrdinalIgnoreCase, out var StopBitsStr);
                data.Properties.TryGetValue("DataBits", StringComparison.OrdinalIgnoreCase, out var DataBits);
                Enum.TryParse(ParityStr, true, out Parity Parity);
                Enum.TryParse(StopBitsStr, true, out StopBits StopBits);
                mySerialPort = new SerialPort(SerialPort, Int32.Parse(Baudrate), Parity, Int32.Parse(DataBits), StopBits);
                mySerialPort.DataReceived += (sender, args) => OnDataRecived(data.ListName, args);
                    
                try
                {
                    mySerialPort.Open();
                }
                catch (Exception ex) 
                {
                    Log?.Verbose($"SerialBarcodeScannerList Exeption {ex}");
                }

                _listName2ScannerMap.Add(data.ListName, mySerialPort);
            }
        }

        protected override CustomListExecuteReturnContext ExecuteFunctionOverride(CustomListData data, CustomListExecuteParameterContext context)
        {
            if (context.FunctionName.Equals("SendSerialCommand", StringComparison.InvariantCultureIgnoreCase))
            {
                if (_listName2ScannerMap.TryGetValue(data.ListName, out var mySerialPort) && mySerialPort.IsOpen)
                {
                    string commandFromParameter = context.Values[0].StringValue;

                    if (!string.IsNullOrEmpty(commandFromParameter))
                    {
                        try
                        {
                            string commandToSend = ConvertPlaceholdersToAscii(commandFromParameter);
                            mySerialPort.Write(commandToSend);
                            Log?.Info($"Command '{commandFromParameter}' sent to scanner as '{commandToSend}'.");
                        }
                        catch (Exception ex)
                        {
                            Log?.Error($"Failed to send or convert command '{commandFromParameter}'.", ex);
                        }
                    }
                    else
                    {
                        Log?.Warning("Parameter 'Command' wurde leer übergeben.");
                    }
                }
                else
                {
                    Log?.Warning($"Serial port for list '{data.ListName}' is not available or not open.");
                }
            }
            return new CustomListExecuteReturnContext();
        }

        private string ConvertPlaceholdersToAscii(string input)
        {
            return Regex.Replace(input, @"<(\d+)>", match =>
            {
                string numberStr = match.Groups[1].Value;

                if (int.TryParse(numberStr, out int asciiCode))
                {
                    return ((char)asciiCode).ToString();
                }
                return match.Value;
            });
        }

        private void OnDataRecived(object state, SerialDataReceivedEventArgs e)
        {
            Log?.Verbose("SerialBarcodeScannerList.OnDataRecived");

            string barcodeData = "";

            try
            {
                Log?.Verbose(barcodeData = mySerialPort.ReadExisting().Replace("\r\n", "").Replace("\r", "").Replace("\n", ""));
            }
            catch (Exception ex) 
            {
                Log?.Verbose($"SerialBarcodeScannerList Exeption {ex}");
            }

            if (state is string listName)
            {
                var items = new CustomListObjectElementCollection();
                var item = new CustomListObjectElement
                {
                    { "Data", barcodeData },
                };
                items.Add(item);
                Data?.Push(listName).Update(0, item);
                Log?.Verbose($" => Pushed data to list {listName}");
            }


        }

        protected override void CleanupOverride(CustomListData data)
        {
            Log?.Verbose("SerialBarcodeScannerList.Cleanup");

            if (_listName2ScannerMap.TryGetValue(data.ListName, out var mySerialPort))
            {
                mySerialPort.Close();

                _listName2ScannerMap.Remove(data.ListName);
            }
        }
    }
}
