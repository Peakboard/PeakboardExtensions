using Newtonsoft.Json;
using Peakboard.ExtensionKit;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.IO.BACnet;
using System.IO.BACnet.Storage;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BacNetExtension.CustomLists
{
    public class BacNetPropertysCustomList : CustomListBase
    {
        BacnetClient client = null;
        IList<BacnetValue> keys = new List<BacnetValue>();
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "BacNetPropertieCustomList",
                Name = "Properties",
                Description = "Add BacNet Data",
                PropertyInputPossible = true,
                PropertyInputDefaults =
                {
                    new CustomListPropertyDefinition { Name = "Ip",Value =GetLocalIPAddress() },
                    new CustomListPropertyDefinition { Name = "Port",Value ="47808" },
                    new CustomListPropertyDefinition { Name = "Address",Value ="192.168.20.54:47808" },
                    new CustomListPropertyDefinition { Name = "ObjectName",Value ="Object_Device" },
                    new CustomListPropertyDefinition { Name = "ObjectInstance",Value ="799877" }
                },
            };
        }
        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            string ipAddress = data.Properties["Ip"].ToString();
            int tcpPort = int.Parse(data.Properties["Port"]);
            BacnetAddress adddress = new BacnetAddress(BacnetAddressTypes.IP, data.Properties["Address"]);
            string objectName = data.Properties["ObjectName"];
            string objectInstance = data.Properties["ObjectInstance"];
            var transport = new BacnetIpUdpProtocolTransport(tcpPort, false, false, 1472, ipAddress);
            client = new BacnetClient(transport);
            client.Start();
            var values = GetPropetiesWithTypes(adddress, objectName, objectInstance);
            var customListCollection = new CustomListColumnCollection();
            foreach(var value in values)
            {
                customListCollection.Add(new CustomListColumn(value.Key.ToString(), value.Value));
            }
            if (values==null||values.Count<=0)
            {
                return new CustomListColumnCollection()
                {
                    new CustomListColumn("Error see Log",CustomListColumnTypes.String)
                };
            }
            return customListCollection;
        }
        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            string ipAddress = data.Properties["Ip"].ToString();
            int tcpPort = int.Parse(data.Properties["Port"]);
            BacnetAddress adddress = new BacnetAddress(BacnetAddressTypes.IP, data.Properties["Address"]);
            string objectName = data.Properties["ObjectName"];
            string objectInstance = data.Properties["ObjectInstance"];
            var transport = new BacnetIpUdpProtocolTransport(tcpPort, false, false, 1472, ipAddress);
            client = new BacnetClient(transport);
            client.Start();
            var values = GetSupportedPropertiesWithValues(adddress, objectName, objectInstance);
            var customListObjectColl = new CustomListObjectElementCollection();
            var itemElement = new CustomListObjectElement();
            var added = new List<string>();
            foreach ( var value in values)
            {
                if (!added.Contains(value.Key))
                {
                    itemElement.Add(value.Key, value.Value);
                    added.Add(value.Key);
                }
            }
            customListObjectColl.Add(itemElement);
            return customListObjectColl;
        }
        private Dictionary<string, string> GetSupportedPropertiesWithValues(BacnetAddress address, string objectName, string instance)
        {
            Dictionary<string,string> propertiesWithValues = new Dictionary<string,string>();
            if (Enum.TryParse(objectName,true,out BacnetObjectTypes type))
            {
                if (uint.TryParse(instance,out uint parsedValue))
                {
                    var objectId = new BacnetObjectId(type, parsedValue);
                    try
                    {
                        if (client.ReadPropertyRequest(address, objectId, BacnetPropertyIds.PROP_PROPERTY_LIST, out  keys))
                        {
                            foreach (var key in keys)
                            {
                                // Der Wert ist die ID der Eigenschaft (z. B. PROP_PRESENT_VALUE)
                                var propertyId = (BacnetPropertyIds)(uint)key.Value;
                                string value = "";
                                try
                                {
                                    if (client.ReadPropertyRequest(address, objectId, propertyId, out var propertyValues))
                                    {
                                        if (propertyValues.Count == 1)
                                        {
                                            value = propertyValues[0].Value.ToString();
                                        }else if (propertyValues.Count >1)
                                        {
                                            value = JsonConvert.SerializeObject(propertyValues[0].Value.ToString());
                                        }
                                        else
                                        {
                                            value = $"property: {propertyId} could not be read.";
                                        }
                                        propertiesWithValues.Add(propertyId.ToString(), value);
                                    }
                                    else
                                    {
                                        value = $"property: {propertyId} could not be read.";
                                        propertiesWithValues.Add(propertyId.ToString(), value);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log.Error($" - Error reading the property {propertyId}: {ex.Message}");
                                }
                            }
                        }
                        else
                        {
                            Log.Error("Error property not found");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error($" - Error reading the property: {ex.Message}");
                    }
                }
            }
           return propertiesWithValues;
        }

        private Dictionary<BacnetPropertyIds, CustomListColumnTypes> GetPropetiesWithTypes(BacnetAddress address, string objectName, string instance)
        {
            Dictionary<BacnetPropertyIds, CustomListColumnTypes> typesAndValues = new Dictionary<BacnetPropertyIds, CustomListColumnTypes>();
            if (Enum.TryParse(objectName, true, out BacnetObjectTypes type))
            {
                if (uint.TryParse(instance, out uint parsedValue))
                {
                    var objectId = new BacnetObjectId(type, parsedValue);
                    if (client.ReadPropertyRequest(address, objectId, BacnetPropertyIds.PROP_PROPERTY_LIST, out var values))
                    {
                        foreach (var key in values)
                        {
                            var propertyId = (BacnetPropertyIds)(uint)key.Value;
                            try
                            {
                                if (client.ReadPropertyRequest(address, objectId, propertyId, out var propertyValues))
                                {
                                    var columnType = new CustomListColumnTypes();
                                    foreach (var propValue in propertyValues)
                                    {
                                        string[] propertyType = propValue.Tag.ToString().Split('_');
                                        switch (propertyType[propertyType.Length - 1])
                                        {
                                            case "BOOLEAN":
                                                columnType = CustomListColumnTypes.Boolean;
                                                break;
                                            case "INT":
                                                columnType = CustomListColumnTypes.Number;
                                                break;
                                            case "REAL":
                                                columnType = CustomListColumnTypes.Number;
                                                break;
                                            case "DOUBLE":
                                                columnType = CustomListColumnTypes.Number;
                                                break;
                                                default:
                                                columnType = CustomListColumnTypes.String;
                                                break;
                                        }
                                    }
                                    typesAndValues.Add(propertyId, columnType);
                                }
                                else
                                {
                                    Log.Error($" property: {propertyId} could not be read.");
                                }
                            }
                            catch (Exception ex)
                            {
                                Log.Error($" - Error reading the property {propertyId}: {ex.Message}");
                            }
                        }
                    }
                    else
                    {
                        Log.Error("Error property not found");
                    }
                }
                else
                {
                    Log.Error("Incorrect object Instance");
                }
            }
            else
            {
                Log.Error("Incorrect object name");
            }
            return typesAndValues;
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
