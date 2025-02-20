using BacNetExtension.Models;
using Peakboard.ExtensionKit;
using System;
using System.Collections.Generic;
using System.IO.BACnet;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace BacNetExtension.CustomLists
{
    public class BacNetDevicesCustomList : CustomListBase
    {
        private BacnetClient _client;
        private List<Device> _devices;

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
                    new CustomListPropertyDefinition { Name = "Ip", Value = GetLocalIpAddress() },
                    new CustomListPropertyDefinition { Name = "Port", Value = "47808" }
                },
            };
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            string ipAddress = data.Properties["Ip"].ToString();
            int tcpPort = int.Parse(data.Properties["Port"]);
            StartConnection(ipAddress, tcpPort);

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
            string ipAddress = data.Properties["Ip"].ToString();
            int tcpPort = int.Parse(data.Properties["Port"]);
            var objectElementCollection = new CustomListObjectElementCollection();
            StartConnection(ipAddress, tcpPort);

            foreach (var device in _devices)
            {
                var itemElement = new CustomListObjectElement();
                itemElement.Add("Address", device.Address.ToString());
                itemElement.Add("DeviceID", device.DeviceId);
                itemElement.Add("MaxAdpu", device.MaxAdpu);
                itemElement.Add("Segmentation", device.Segmentation.ToString());
                itemElement.Add("VendorID", device.VendorId.ToString());
                objectElementCollection.Add(itemElement);
            }

            return objectElementCollection;
        }

        private void StartConnection(string ip, int port)
        {
            _devices = new List<Device>();
            if (!string.IsNullOrEmpty(ip))
            {
                var transport = new BacnetIpUdpProtocolTransport(port, false, false, 1472, ip);
                _client = new BacnetClient(transport);
                _client.Start();
                Thread.Sleep(1000);
                _client.OnIam += OnIamReceived;
                _client.WhoIs();
                Thread.Sleep(1000);
                return;
            }
            throw new Exception("Incorrect IP address");
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
            }
        }

        public static string GetLocalIpAddress()
        {
            return Dns.GetHostEntry(Dns.GetHostName()).AddressList
                .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork)?.ToString() ?? string.Empty;
        }
    }
}
