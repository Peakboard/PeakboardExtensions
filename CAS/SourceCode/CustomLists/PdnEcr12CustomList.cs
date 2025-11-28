using CASScaleExtension.Shared;
using Peakboard.ExtensionKit;
using System.IO.Ports;
using System.Text;

namespace CASScaleExtension.CustomLists
{
    [Serializable]
    [CustomListIcon("CASScaleExtension.pb_datasource_cas.png")]
    public class PdnEcr12CustomList : CustomListBase
    {
        private readonly Dictionary<string, SerialPortService> _listName2ScannerMap = [];
        private readonly Dictionary<string, StringBuilder> _listName2BufferMap = [];

        private const char STX = (char)0x02;
        private const char ETX = (char)0x03;
        private const char EOT = (char)0x04;
        private const char ENQ = (char)0x05;
        private const char ACK = (char)0x06;
        private const char NAK = (char)0x15;
        private const char ESC = (char)0x1B;

        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "PdnEcr12",
                Name = "PDN ECR Typ 12", 
                Description = "Get Scale Data (ECR Type 12 Protocol)",
                PropertyInputPossible = true,
                SupportsPushOnly = true,
                PropertyInputDefaults = {
                    new CustomListPropertyDefinition() { Name = "SerialPort", Value = "COM7" },
                    new CustomListPropertyDefinition() { Name = "Baudrate", Value = "9600" },
                    new CustomListPropertyDefinition() { Name = "Parity", SelectableValues = new [] { "None", "Odd", "Even", "Mark", "Space" }, Value = "Odd"},
                    new CustomListPropertyDefinition() { Name = "StopBits", SelectableValues = new [] { "None", "One", "Two", "OnePointFive" }, Value = "One"},
                    new CustomListPropertyDefinition() { Name = "DataBits", SelectableValues = new [] { "8", "7", "6", "5" }, Value = "7"}
                },
                Functions = new CustomListFunctionDefinitionCollection
                {
                    new CustomListFunctionDefinition()
                    {
                        Name = "SetZero" 
                    },
                    new CustomListFunctionDefinition()
                    {
                        Name = "GetData"
                    }
                }
            };
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            var columns = new CustomListColumnCollection
            {
                new CustomListColumn("Unit", CustomListColumnTypes.String),
                new CustomListColumn("Weight", CustomListColumnTypes.Number),
                new CustomListColumn("UnitPrice", CustomListColumnTypes.Number),
                new CustomListColumn("TotalPrice", CustomListColumnTypes.Number),
                new CustomListColumn("Timestamp", CustomListColumnTypes.Number)
            };
            return columns;
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            return new CustomListObjectElementCollection();
        }

        protected override void SetupOverride(CustomListData data)
        {
            Log?.Verbose($"PdnEcr12CustomList.Setup for {data.ListName}");

            if (!_listName2ScannerMap.ContainsKey(data.ListName))
            {
                data.Properties.TryGetValue("SerialPort", StringComparison.OrdinalIgnoreCase, out var serialPort);
                data.Properties.TryGetValue("Baudrate", StringComparison.OrdinalIgnoreCase, out var baudrateStr);
                data.Properties.TryGetValue("Parity", StringComparison.OrdinalIgnoreCase, out var parityStr);
                data.Properties.TryGetValue("StopBits", StringComparison.OrdinalIgnoreCase, out var stopBitsStr);
                data.Properties.TryGetValue("DataBits", StringComparison.OrdinalIgnoreCase, out var dataBitsStr);

                if (!int.TryParse(baudrateStr, out int baudrate)) { baudrate = 9600; }
                if (!int.TryParse(dataBitsStr, out int dataBits)) { dataBits = 7; }

                Enum.TryParse(parityStr, true, out Parity parity);
                Enum.TryParse(stopBitsStr, true, out StopBits stopBits);

                var portService = new SerialPortService(serialPort, baudrate, parity, dataBits, stopBits);

                portService.DataReceived += (sender, eventArgs) => OnDataReceived(data.ListName, eventArgs);

                try
                {
                    portService.Open();
                    Log?.Info($"Serial port {serialPort} opened for list {data.ListName} with {baudrate}, {parity}, {dataBits}, {stopBits}.");
                }
                catch (Exception ex)
                {
                    Log?.Error($"PdnEcr12CustomList Exception on port open for {data.ListName}: {ex.Message}");
                }

                _listName2ScannerMap.Add(data.ListName, portService);

                _listName2BufferMap.Add(data.ListName, new StringBuilder());
            }
        }

        protected override CustomListExecuteReturnContext ExecuteFunctionOverride(CustomListData data, CustomListExecuteParameterContext context)
        {
            if (!_listName2ScannerMap.TryGetValue(data.ListName, out var portService))
            {
                Log?.Warning($"Serial port for list '{data.ListName}' is not available or not open.");
                return new CustomListExecuteReturnContext();
            }

            try
            {
                if (context.FunctionName.Equals("SetZero", StringComparison.InvariantCultureIgnoreCase))
                {
                    Log?.Verbose($"Executing 'Set Zero' (Type 12) for {data.ListName}");
                    byte[] command = { (byte)EOT, (byte)STX, 0x33, 0x31, (byte)ETX };
                    _ = portService.SendDataAsync(command);
                }
                else if (context.FunctionName.Equals("GetData", StringComparison.InvariantCultureIgnoreCase))
                {
                    Log?.Verbose($"Executing 'Get Data' (Type 12) for {data.ListName}");
                    byte[] command = { (byte)EOT, (byte)ENQ }; 
                    _ = portService.SendDataAsync(command);
                }
            }
            catch (Exception ex)
            {
                Log?.Error($"Failed to send command '{context.FunctionName}' for {data.ListName}.", ex);
            }

            return new CustomListExecuteReturnContext();
        }

        private void OnDataReceived(string listName, byte[] data)
        {
            if (!_listName2BufferMap.TryGetValue(listName, out var buffer))
            {
                Log?.Warning($"[ECR Type 12] Received data for {listName}, but buffer is not initialized.");
                return;
            }

            if (data.Length == 1)
            {
                if (data[0] == ACK) { Log?.Verbose($"[ECR Type 12] ACK received for {listName}"); return; }
                if (data[0] == NAK) { Log?.Warning($"[ECR Type 12] NAK received for {listName}"); return; }
            }

            buffer.Append(Encoding.ASCII.GetString(data));
            ProcessBuffer(listName, buffer);
        }

        private void ProcessBuffer(string listName, StringBuilder buffer)
        {
            string content = buffer.ToString();
            int startIndex = content.IndexOf((char)0x02);
            if (startIndex == -1)
            {
                if (buffer.Length > 256) buffer.Clear(); 
                return;
            }

            int endIndex = content.IndexOf((char)0x03, startIndex); 
            if (endIndex == -1)
            {
                return;
            }

            string packet = content.Substring(startIndex, endIndex - startIndex + 1);
            buffer.Remove(0, endIndex + 1); 

            ParseAndDisplayScaleInfo(listName, packet);

            if (buffer.Length > 0)
            {
                ProcessBuffer(listName, buffer);
            }
        }

        private void ParseAndDisplayScaleInfo(string listName, string packet)
        {
            try
            {
                string data = packet.Substring(1, packet.Length - 2);

                 string[] parts = data.Split(ESC);

                if (parts.Length < 5 || parts[0] != "02")
                {
                    throw new FormatException($"Invalid response format. Expected 5 parts, got {parts.Length}. Data: {parts[0]}");
                }

                string statusRaw = parts[1];
                string weightRaw = parts[2];
                string unitPriceRaw = parts[3];
                string totalPriceRaw = parts[4];

                string unit = statusRaw switch
                {
                    "3" => "kg",
                    "2" => "g",
                    "1" => "oz",
                    "0" => "lb",
                    _ => "Unbekannt"
                };

                double weight = double.Parse(weightRaw) / 1000.0;
                double unitPrice = double.Parse(unitPriceRaw) / 100.0;
                double totalPrice = double.Parse(totalPriceRaw) / 100.0;
                long unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();


                var item = new CustomListObjectElement
                {
                    { "Unit", unit },
                    { "Weight", weight },
                    { "UnitPrice", unitPrice },
                    { "TotalPrice", totalPrice },
                    { "Timestamp", unixTimestamp }
                };

                Data?.Push(listName).Update(0, item);
                Log?.Verbose($"=> Pushed data to list {listName}");
            }
            catch (Exception ex)
            {
                Log?.Error($"[ECR Type 12] Error parsing response for {listName}: {ex.Message}");
                Log?.Error($"[ECR Type 12] Received packet: {packet}");
            }
        }

        protected override void CleanupOverride(CustomListData data)
        {
            Log?.Verbose($"PdnEcr12CustomList.Cleanup for {data.ListName}");

            if (_listName2ScannerMap.TryGetValue(data.ListName, out var portService))
            {
                portService?.Close();
                _listName2ScannerMap.Remove(data.ListName);
            }

            if (_listName2BufferMap.ContainsKey(data.ListName))
            {
                _listName2BufferMap.Remove(data.ListName);
            }
        }
    }
}