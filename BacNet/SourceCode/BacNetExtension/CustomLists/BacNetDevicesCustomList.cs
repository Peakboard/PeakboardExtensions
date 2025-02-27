using BacNetExtension.Models;
using Peakboard.ExtensionKit;
using System;
using System.Collections.Generic;
using System.IO.BACnet;
using System.Linq;
using System.Threading;

namespace BacNetExtension.CustomLists
{
    [Serializable]
    [CustomListIcon("BacNetExtension.pb_datasource_bacnet.png")]
    public class BacNetDevicesCustomList : CustomListBase
    {
        private BacnetClient _client;
        private List<Device> _devices;
        private ManualResetEventSlim _waitHandle;
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
            var tcpPort = int.Parse(data.Properties["Port"]);
            var objectElementCollection = new CustomListObjectElementCollection();
            StartConnection(tcpPort);

            foreach (var device in _devices)
            {
                var itemElement = new CustomListObjectElement()
                {
                    { "Address", device.Address.ToString() },
                    { "DeviceID", device.DeviceId },
                    { "MaxAdpu", device.MaxAdpu },
                    { "Segmentation", device.Segmentation.ToString() },
                    { "VendorID", device.VendorId.ToString() }
                };
                objectElementCollection.Add(itemElement);
            }

            return objectElementCollection;
        }

        private void StartConnection(int port)
        {
            _waitHandle = new ManualResetEventSlim(false);
            _devices = new List<Device>();
            var transport = new BacnetIpUdpProtocolTransport(port);
            _client = new BacnetClient(transport);
            _client.Start();
            _client.OnIam += OnIamReceived;
            _client.WhoIs();
            _waitHandle.Wait(3000);
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
                _waitHandle.Set();
            }
            
        }
    }
}
