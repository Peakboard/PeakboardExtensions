using BacNetExtension.Models;
using Peakboard.ExtensionKit;
using System;
using System.Collections.Generic;
using System.IO.BACnet;
using System.Linq;
using System.Threading.Tasks;

namespace BacNetExtension.CustomLists
{
    [Serializable]
    [CustomListIcon("BacNetExtension.pb_datasource_bacnet.png")]
    public class BacNetDevicesCustomList : CustomListBase
    {
        private List<Device> _devices;
        private DateTime _lastEventTime;
        private const int _timeoutMilliseconds = 2000;

        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "BacNetDeviceCustomList",
                Name = "Devices",
                Description = "Add BacNet Data",
                PropertyInputPossible = true,
                PropertyInputDefaults =
                {
                    new CustomListPropertyDefinition { Name = "Port", Value = "47808" }
                }
            };
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            return new CustomListColumnCollection()
            {
                new CustomListColumn("Address", CustomListColumnTypes.String),
                new CustomListColumn("DeviceID", CustomListColumnTypes.Number),
                new CustomListColumn("MaxAdpu", CustomListColumnTypes.Number),
                new CustomListColumn("Segmentation", CustomListColumnTypes.String),
                new CustomListColumn("VendorID", CustomListColumnTypes.String),
            };
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            var task = Task.Run(async () =>
            {
                if (int.TryParse(data.Properties["Port"], out int tcpPort))
                {
                    var objectElementCollection = new CustomListObjectElementCollection();

                    try
                    {
                        await StartConnection(tcpPort);
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"Error while starting connection: {ex}");
                        return objectElementCollection;
                    }

                    if (_devices != null && _devices.Count > 0)
                    {
                        foreach (var device in _devices)
                        {
                            Log.Info(
                                $"Device found: Address={device.Address}, DeviceID={device.DeviceId}, VendorID={device.VendorId}");
                            var itemElement = new CustomListObjectElement()
                            {
                                {"Address", device.Address.ToString()},
                                {"DeviceID", device.DeviceId},
                                {"MaxAdpu", device.MaxAdpu},
                                {"Segmentation", device.Segmentation.ToString()},
                                {"VendorID", device.VendorId.ToString()}
                            };
                            objectElementCollection.Add(itemElement);
                        }
                    }
                    else
                    {
                        Log.Info("No devices found.");
                    }

                    return objectElementCollection;
                }

                throw new ArgumentException(
                    "Invalid port number. Please ensure that the port is a valid integer.");
            });

            task.Wait();
            return task.Result;
        }

        private async Task StartConnection(int port)
        {
            _devices = new List<Device>();
            var transport = new BacnetIpUdpProtocolTransport(port);
            BacnetClient client = new BacnetClient(transport);
            client.Start();
            client.OnIam += OnIamReceived;
            client.WhoIs();

            _lastEventTime = DateTime.Now;
            Log.Info("Waiting for devices...");

            while ((DateTime.Now - _lastEventTime).TotalMilliseconds < _timeoutMilliseconds)
            {
                await Task.Delay(100);
            }

            client.Dispose();
        }

        private void OnIamReceived(BacnetClient sender, BacnetAddress adr, uint deviceId, uint maxApdu, BacnetSegmentations segmentation, ushort vendorId)
        {
            Device device = new Device
            {
                Address = adr,
                DeviceId = deviceId,
                MaxAdpu = maxApdu,
                VendorId = vendorId,
                Segmentation = segmentation
            };

            if (_devices.FirstOrDefault(d => d.DeviceId == device.DeviceId) == null)
            {
                _devices.Add(device);
                Log.Info($"Device added: DeviceID={device.DeviceId}, Address={device.Address}");
            }

            _lastEventTime = DateTime.Now;
        }
    }
}
