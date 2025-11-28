using CASScaleExtension.Shared;
using Peakboard.ExtensionKit;
using System.IO.Ports;
using System.Text;

namespace CASScaleExtension.CustomLists
{
    [Serializable]
    [CustomListIcon("CASScaleExtension.pb_datasource_cas.png")]
    public class PdnEcr14CustomList : CustomListBase
    {
        private readonly Dictionary<string, SerialPortService> _listName2ScannerMap = [];
        private readonly StringBuilder _responseBuffer = new StringBuilder();
        private const int PACKET_LENGTH = 6;

        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "PdnEcr14",
                Name = "PDN ECR Typ 14",
                Description = "Get Scale Data",
                PropertyInputPossible = true,
                SupportsPushOnly = true,
                PropertyInputDefaults = {
                    new CustomListPropertyDefinition() { Name = "SerialPort", Value = "COM3" },
                    new CustomListPropertyDefinition() { Name = "Baudrate", Value = "9600" },
                    new CustomListPropertyDefinition() { Name = "Parity", SelectableValues = new [] { "None", "Odd", "Even", "Mark", "Space" }, Value = "Odd"},
                    new CustomListPropertyDefinition() { Name = "StopBits", SelectableValues = new [] { "None", "One", "Two", "OnePointFive" }, Value = "One"},
                    new CustomListPropertyDefinition() { Name = "DataBits", SelectableValues = new [] { "8", "7", "6", "5" }, Value = "7"}
                },
            };
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            var columns = new CustomListColumnCollection
            {
                new CustomListColumn("Weight", CustomListColumnTypes.Number)                
            };
            return columns;
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            return new CustomListObjectElementCollection();
        }

        protected override void SetupOverride(CustomListData data)
        {
            Log?.Verbose($"PdnEcr14CustomList.Setup for {data.ListName}");

            if (!_listName2ScannerMap.ContainsKey(data.ListName))
            {
                data.Properties.TryGetValue("SerialPort", StringComparison.OrdinalIgnoreCase, out var serialPort);
                data.Properties.TryGetValue("Baudrate", StringComparison.OrdinalIgnoreCase, out var baudrate);
                data.Properties.TryGetValue("Parity", StringComparison.OrdinalIgnoreCase, out var parityStr);
                data.Properties.TryGetValue("StopBits", StringComparison.OrdinalIgnoreCase, out var stopBitsStr);
                data.Properties.TryGetValue("DataBits", StringComparison.OrdinalIgnoreCase, out var dataBits);

                Enum.TryParse(parityStr, true, out Parity parity);
                Enum.TryParse(stopBitsStr, true, out StopBits stopBits);

                var portService = new SerialPortService(serialPort, int.Parse(baudrate), parity, int.Parse(dataBits), stopBits);

                portService.DataReceived += (sender, eventArgs) => OnDataReceived(data.ListName, eventArgs);

                try
                {
                    portService.Open();
                    Log?.Info($"Serial port {serialPort} opened for list {data.ListName}.");
                }
                catch (Exception ex)
                {
                    Log?.Error($"PdnEcr14CustomList Exception on port open for {data.ListName}: {ex.Message}");
                }

                _listName2ScannerMap.Add(data.ListName, portService);
            }
        }

        protected override CustomListExecuteReturnContext ExecuteFunctionOverride(CustomListData data, CustomListExecuteParameterContext context)
        {
            return new CustomListExecuteReturnContext();
        }

        private void OnDataReceived(string listName, byte[] data)
        {
            _responseBuffer.Append(Encoding.ASCII.GetString(data));

            while (_responseBuffer.Length >= PACKET_LENGTH)
            {
                string packet = _responseBuffer.ToString(0, PACKET_LENGTH);

                if (packet.StartsWith("S") && double.TryParse(packet.Substring(1), out double weightValue))
                {
                    var item = new CustomListObjectElement
                {
                    { "Weight", weightValue / 1000.0 }
                };

                    Data?.Push(listName).Update(0, item);
                    Log?.Verbose($"=> Pushed data to list {listName}");
                    _responseBuffer.Remove(0, PACKET_LENGTH);
                }
                else
                {
                    int nextS = _responseBuffer.ToString().IndexOf('S', 1);
                    if (nextS != -1)
                    {
                        _responseBuffer.Remove(0, nextS);
                    }
                    else
                    {
                        _responseBuffer.Clear();
                    }
                }
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
        }
    }
}