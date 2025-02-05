using Peakboard.ExtensionKit;
using System;
using System.Collections.Generic;
using System.IO.BACnet;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BacNetExtension.CustomLists
{
    public class BacNetObjectsCustomList : CustomListBase
    {
        BacnetClient client = null;
        IList<BacnetValue> objects = new List<BacnetValue>();
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "BacNetObjectCustomList",
                Name = "BacNetObjectCustomList",
                Description = "Add BacNet Data",
                PropertyInputPossible = true,
                PropertyInputDefaults =
                {
                    new CustomListPropertyDefinition { Name = "Ip",Value =GetLocalIPAddress() },
                    new CustomListPropertyDefinition { Name = "Port",Value ="47808" },
                    new CustomListPropertyDefinition { Name = "Address",Value ="192.168.20.54:47808" },
                    new CustomListPropertyDefinition { Name = "DeviceId",Value ="799877" }
                },
            };
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            string ipAddress = data.Properties["Ip"].ToString();
            int tcpPort = int.Parse(data.Properties["Port"]);
            BacnetAddress adddress = new BacnetAddress(BacnetAddressTypes.IP, data.Properties["Address"]);
            uint deviceId = uint.Parse(data.Properties["DeviceId"]);
            var transport = new BacnetIpUdpProtocolTransport(tcpPort, false, false, 1472, ipAddress);
            client = new BacnetClient(transport);
            client.Start();
            GetAvailableObjects(adddress, deviceId);
            var columnColl = new CustomListColumnCollection() {

                new CustomListColumn("Objects",CustomListColumnTypes.String)
            };
            return columnColl;
        }

       

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            string ipAddress = data.Properties["Ip"].ToString();
            int tcpPort = int.Parse(data.Properties["Port"]);
            BacnetAddress adddress = new BacnetAddress(BacnetAddressTypes.IP, data.Properties["Address"]);
            uint deviceId = uint.Parse(data.Properties["DeviceId"]);
            var transport = new BacnetIpUdpProtocolTransport(tcpPort, false, false, 1472, ipAddress);
            client = new BacnetClient(transport);
            client.Start();
            GetAvailableObjects(adddress, deviceId);
            var objectElementCollection = new CustomListObjectElementCollection();
            foreach (var item in objects)
            {
                var itemElement = new CustomListObjectElement();
                itemElement.Add("Objects", item.Value.ToString());
                objectElementCollection.Add(itemElement);
            }
            return objectElementCollection;
        }
        private void GetAvailableObjects(BacnetAddress address, uint deviceId)
        {
            var objectId = new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, deviceId);
            var propertyId = BacnetPropertyIds.PROP_OBJECT_LIST;
            try
            {
                if (client.ReadPropertyRequest(address, objectId, propertyId, out objects))
                {
                    //foreach (var value in values)
                    //{
                    //    if (objects.FirstOrDefault(o=>o==value.Value)== null)
                    //    {
                    //        objects.Add(value.Value.ToString());
                    //    }
                    //}
                }
                else
                {
                   Log.Info("Objektliste konnte nicht gelesen werden.");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Fehler beim Lesen der Objektliste: {ex.Message}");
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
