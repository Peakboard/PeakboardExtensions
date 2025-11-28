using CASScaleExtension.Shared;
using Peakboard.ExtensionKit;

namespace CASScaleExtension.CustomLists
{
    [Serializable]
    [CustomListIcon("CASScaleExtension.pb_datasource_cas.png")]
    public class Pb2BleCustomList : CustomListBase
    {
        private readonly Dictionary<string, CasScaleBleClient> _clients = new();
        private readonly HashSet<string> _initializedLists = new();
        private readonly object _lock = new object();

        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "PB2BLE",
                Name = "PB2-BLE (Bluetooth)",
                Description = "Connection to CAS PB2 scales via Bluetooth Low Energy (BLE)",
                PropertyInputPossible = true,
                SupportsPushOnly = true,
                PropertyInputDefaults = {
                    new CustomListPropertyDefinition() { Name = "DeviceIdentifier", Value = "CAS-BLE" },
                    new CustomListPropertyDefinition() { Name = "AutoConnect", Value = "true" }
                },
                Functions = new CustomListFunctionDefinitionCollection
                {
                    new CustomListFunctionDefinition { Name = "Connect" },
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
            Log?.Verbose($"[Pb2Ble] Setup for list: {data.ListName}");

            lock (_lock)
            {
                if (!_clients.ContainsKey(data.ListName))
                {
                    var client = new CasScaleBleClient(msg => Log?.Verbose(msg));

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

                    // 4. Command Feedback (Tare/Zero)
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

                    if (data.Properties.TryGetValue("AutoConnect", out var ac) &&
                        bool.TryParse(ac, out bool auto) && auto)
                    {
                        data.Properties.TryGetValue("DeviceIdentifier", out var deviceId);
                        if (!string.IsNullOrWhiteSpace(deviceId))
                        {
                            Task.Run(() => client.ConnectAsync(deviceId));
                        }
                    }
                }
            }
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

            data.Properties.TryGetValue("DeviceIdentifier", out var deviceId);
            string func = context.FunctionName.ToLowerInvariant();

            switch (func)
            {
                case "connect":
                    if (!string.IsNullOrEmpty(deviceId)) Task.Run(() => client.ConnectAsync(deviceId));
                    break;
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