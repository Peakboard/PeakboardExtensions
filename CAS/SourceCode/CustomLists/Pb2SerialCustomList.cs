using CASScaleExtension.Shared;
using Peakboard.ExtensionKit;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Threading.Tasks;

namespace CASScaleExtension.CustomLists
{
    [Serializable]
    [CustomListIcon("CASScaleExtension.pb_datasource_cas.png")]
    public class Pb2SerialCustomList : CustomListBase
    {
        private readonly Dictionary<string, CasScaleSerialClient> _clients = new();
        private readonly HashSet<string> _initializedLists = new();
        private readonly object _lock = new object();

        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "PB2Serial",
                Name = "PB2-Serial",
                Description = "Connection to CAS scales via Serial Port (RS232/USB)",
                PropertyInputPossible = true,
                SupportsPushOnly = true,
                PropertyInputDefaults = {
                    new CustomListPropertyDefinition() { Name = "SerialPort", Value = "COM3" },
                    new CustomListPropertyDefinition() { Name = "Baudrate", Value = "9600" },
                    new CustomListPropertyDefinition() { Name = "Parity", SelectableValues = new [] { "None", "Odd", "Even", "Mark", "Space" }, Value = "None"},
                    new CustomListPropertyDefinition() { Name = "StopBits", SelectableValues = new [] { "None", "One", "Two", "OnePointFive" }, Value = "One"},
                    new CustomListPropertyDefinition() { Name = "DataBits", SelectableValues = new [] { "8", "7", "6", "5" }, Value = "8"},
                    // AutoConnect implied
                },
                Functions = new CustomListFunctionDefinitionCollection
                {
                    // Connect removed (Auto-Connect on setup)
                    new CustomListFunctionDefinition { Name = "GetData" },
                    new CustomListFunctionDefinition { Name = "SetZero" },
                    new CustomListFunctionDefinition { Name = "Tare" },
                    new CustomListFunctionDefinition { Name = "GetBattery" }
                }
            };
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            return new CustomListColumnCollection
            {
                new CustomListColumn("Status", CustomListColumnTypes.String),
                new CustomListColumn("Weight", CustomListColumnTypes.Number),
                new CustomListColumn("Battery", CustomListColumnTypes.String),
                new CustomListColumn("Timestamp", CustomListColumnTypes.Number)
            };
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            return new CustomListObjectElementCollection();
        }

        protected override void SetupOverride(CustomListData data)
        {
            Log?.Verbose($"[Pb2Serial] Setup for list: {data.ListName}");

            lock (_lock)
            {
                if (!_clients.ContainsKey(data.ListName))
                {
                    var client = new CasScaleSerialClient(msg => Log?.Verbose(msg));

                    // 1. Weight Received -> Set Weight, Clear Battery
                    client.MeasurementReceived += (sender, measurement) =>
                    {
                        var item = new CustomListObjectElement();
                        item.Add("Status", measurement.Status);
                        item.Add("Weight", measurement.Weight);
                        item.Add("Battery", "");
                        item.Add("Timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                        PushRow(data.ListName, item);
                    };

                    // 2. Battery Received -> Set Battery, Weight = 0
                    client.BatteryReceived += (sender, batt) =>
                    {
                        var item = new CustomListObjectElement();
                        item.Add("Status", "Battery Info");
                        item.Add("Weight", 0);
                        item.Add("Battery", batt);
                        item.Add("Timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                        PushRow(data.ListName, item);
                    };

                    // 3. Connection Status
                    client.ConnectionStatusChanged += (sender, statusMsg) =>
                    {
                        var item = new CustomListObjectElement();
                        item.Add("Status", statusMsg);
                        item.Add("Weight", 0);
                        item.Add("Battery", "");
                        item.Add("Timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                        PushRow(data.ListName, item);
                    };

                    // 4. Command Feedback
                    client.CommandStatusReceived += (sender, statusMsg) =>
                    {
                        var item = new CustomListObjectElement();
                        item.Add("Status", statusMsg);
                        item.Add("Weight", 0);
                        item.Add("Battery", "");
                        item.Add("Timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                        PushRow(data.ListName, item);
                    };

                    _clients.Add(data.ListName, client);

                    // Connect immediately
                    ConnectInternal(client, data);
                }
            }
        }

        private void ConnectInternal(CasScaleSerialClient client, CustomListData data)
        {
            data.Properties.TryGetValue("SerialPort", out var port);
            data.Properties.TryGetValue("Baudrate", out var baud);
            data.Properties.TryGetValue("Parity", out var parityStr);
            data.Properties.TryGetValue("StopBits", out var stopStr);
            data.Properties.TryGetValue("DataBits", out var dataBitsStr);

            Enum.TryParse(parityStr, true, out Parity parity);
            Enum.TryParse(stopStr, true, out StopBits stopBits);

            if (!int.TryParse(baud, out int baudRate)) baudRate = 9600;
            if (!int.TryParse(dataBitsStr, out int dataBits)) dataBits = 7;

            // Connect in background
            Task.Run(() => client.Connect(port, baudRate, parity, dataBits, stopBits));
        }

        private void PushRow(string listName, CustomListObjectElement item)
        {
            lock (_lock)
            {
                if (!_initializedLists.Contains(listName))
                {
                    Data?.Push(listName).Add(item);
                    _initializedLists.Add(listName);
                }
                else
                {
                    Data?.Push(listName).Update(0, item);
                }
            }
        }

        protected override CustomListExecuteReturnContext ExecuteFunctionOverride(CustomListData data, CustomListExecuteParameterContext context)
        {
            if (!_clients.TryGetValue(data.ListName, out var client))
                return new CustomListExecuteReturnContext();

            string func = context.FunctionName.ToLowerInvariant();

            switch (func)
            {
                case "getdata": Task.Run(() => client.RequestWeightAsync()); break;
                case "setzero": Task.Run(() => client.ZeroScaleAsync()); break;
                case "tare": Task.Run(() => client.TareScaleAsync()); break;
                case "getbattery": Task.Run(() => client.RequestBatteryAsync()); break;
            }

            return new CustomListExecuteReturnContext();
        }

        protected override void CleanupOverride(CustomListData data)
        {
            lock (_lock)
            {
                if (_clients.TryGetValue(data.ListName, out var client))
                {
                    client.Dispose();
                    _clients.Remove(data.ListName);
                }
                if (_initializedLists.Contains(data.ListName))
                {
                    _initializedLists.Remove(data.ListName);
                }
            }
        }
    }
}