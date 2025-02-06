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
        BacnetClient client = null;
        List<Device> devices = new List<Device>();
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "BacNetDeviceCustomList",
                Name = "DevicesCustomList",
                Description = "Add BacNet Data",
                PropertyInputPossible = true,
                PropertyInputDefaults =
                {
                    new CustomListPropertyDefinition { Name = "Ip",Value =GetLocalIPAddress() },
                    new CustomListPropertyDefinition { Name = "Port",Value ="47808" }
                },
            };
        }
        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            string ipAddress = data.Properties["Ip"].ToString();
            int tcpPort = int.Parse(data.Properties["Port"]);
            Start(ipAddress, tcpPort);
            return new CustomListColumnCollection()
            {
                new CustomListColumn("Address",CustomListColumnTypes.String),
                new CustomListColumn("DeviceID",CustomListColumnTypes.Number),
                new CustomListColumn("MaxAdpu",CustomListColumnTypes.Number),
                new CustomListColumn("Segmentation",CustomListColumnTypes.String),
                new CustomListColumn("VendorID",CustomListColumnTypes.String),
            };
        }
        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            string ipAddress = data.Properties["Ip"].ToString();
            int tcpPort = int.Parse(data.Properties["Port"]);
            var objectElementCollection = new CustomListObjectElementCollection();
            Log.Info($"Devics count = {devices.Count}");
            if (devices.Count <= 0)
            {
                Start(ipAddress, tcpPort);
            }
            foreach (var item in devices)
            {
                var itemElement = new CustomListObjectElement();
                itemElement.Add("Address", item.Address.ToString());
                itemElement.Add("DeviceID", item.DeviceId);
                itemElement.Add("MaxAdpu", item.MaxAdpu);
                itemElement.Add("Segmentation", item.Segmentation.ToString());
                itemElement.Add("VendorID", item.VendorId.ToString());
                objectElementCollection.Add(itemElement);
            }
            return objectElementCollection;
        }
        private void Start(string ip,int port)
        {
            if (!string.IsNullOrEmpty(ip))
            {
                var transport = new BacnetIpUdpProtocolTransport(port, false, false, 1472, ip);
                client = new BacnetClient(transport);
                client.Start();
                Thread.Sleep(1000);
                client.OnIam += OnIamReceived;
                client.WhoIs();
                Thread.Sleep(1000);
                return;
            }
            throw new Exception("incorrect iP address");
        }
        private void OnIamReceived(BacnetClient sender, BacnetAddress adr, uint deviceId, uint maxAPDU, BacnetSegmentations segmentation, ushort vendorId)
        {
            Device device = new Device();
            device.Address = adr;
            device.DeviceId = deviceId;
            device.MaxAdpu = maxAPDU;
            device.VendorId = vendorId;
            device.Segmentation = segmentation;
            if (devices.FirstOrDefault(d => d.DeviceId == device.DeviceId) == null)
            {
                devices.Add(device);
            }
        }
        public string GetLocalIPAddress()
        {
            string localIPAddress = "";
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return localIPAddress;
        }
    }
}
