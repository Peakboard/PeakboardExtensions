using Peakboard.ExtensionKit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.BACnet;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace BacNetExtension.CustomLists
{
    public class BacNetObjectsCustomList : CustomListBase
    {
        Dictionary<string, BacnetObjectTypes> bacnetObjectMap = Enum.GetValues(typeof(BacnetObjectTypes))
            .Cast<BacnetObjectTypes>()
            .Where(e => e.ToString().StartsWith("OBJECT_"))
            .ToDictionary(e => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(e.ToString().Replace("OBJECT_", "").Replace("_", "").ToLower()), e => e);
        BacnetClient client = null;
        IList<BacnetValue> objects = new List<BacnetValue>();
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "BacNetObjectCustomList",
                Name = "Objects",
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
            var columnColl = new CustomListColumnCollection()
            {

            };
            List<string> added = new List<string>();
            foreach (var item in objects)
            {
                string columnName = item.Value.ToString().Split(':')[0];
                if (!added.Contains(columnName))
                {
                    var name = bacnetObjectMap.FirstOrDefault(b => b.Value.ToString() == columnName).Key;
                    if (name != null)
                    {
                        var column = new CustomListColumn(name, CustomListColumnTypes.Number);
                        columnColl.Add(column);
                        added.Add(columnName);
                    }
                    else
                    {
                        var column = new CustomListColumn(columnName, CustomListColumnTypes.Number);
                        columnColl.Add(column);
                        added.Add(columnName);
                    }
                }
            }
            return columnColl;
        }
        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            try
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
                var itemElement = new CustomListObjectElement();
                var added = new List<string>();
                foreach (var item in objects)
                {
                    string column = item.Value.ToString().Split(':')[0];
                    var name = bacnetObjectMap.FirstOrDefault(b => b.Value.ToString() == column).Key;
                    string value = item.Value.ToString().Split(':')[1];
                    if (!added.Contains(column))
                    {
                        if (name != null)
                        {
                            itemElement.Add(name, value);
                            added.Add(column);
                        }
                        else
                        {
                            itemElement.Add(column, value);
                            added.Add(column);
                        }
                    }
                }
                objectElementCollection.Add(itemElement);
                return objectElementCollection;
            }
            catch (Exception ex)
            {
                Log.Error($"{ex.Message}");
                return new CustomListObjectElementCollection();
            }
        }
        private void GetAvailableObjects(BacnetAddress address, uint deviceId)
        {
            var objectId = new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, deviceId);
            var propertyId = BacnetPropertyIds.PROP_OBJECT_LIST;
            try
            {
                if (client.ReadPropertyRequest(address, objectId, propertyId, out objects))
                {
                    Log.Info($"Obejcts count = {objects.Count}");
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
