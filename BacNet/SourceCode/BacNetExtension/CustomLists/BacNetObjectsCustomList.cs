using Peakboard.ExtensionKit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.BACnet;
using System.Linq;
using Newtonsoft.Json;

namespace BacNetExtension.CustomLists
{
    [Serializable]
    [CustomListIcon("BacNetExtension.pb_datasource_bacnet.png")]
    public class BacNetObjectsCustomList : CustomListBase
    {
        private readonly Dictionary<string, BacnetObjectTypes> _bacnetObjectMap = Enum.GetValues(typeof(BacnetObjectTypes))
            .Cast<BacnetObjectTypes>()
            .Where(e => e.ToString().StartsWith("OBJECT_"))
            .ToDictionary(e => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(e.ToString().Replace("OBJECT_", "").Replace("_", "").ToLower()), e => e);

        private readonly Dictionary<string, BacnetObjectTypes> _bacnetObjectsMap = Enum.GetValues(typeof(BacnetObjectTypes))
            .Cast<BacnetObjectTypes>()
            .Where(e => e.ToString().StartsWith("OBJECT_"))
            .ToDictionary(e => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(e.ToString().Replace("OBJECT_", "").Replace("_", "").ToLower()), e => e);

        private IList<BacnetValue> _objects = new List<BacnetValue>();
        private string[] _propertyNames = { "ObjectName", "PresentValue", "Unit", "StatusFlags", "Description", "Type","Instancenumber","Props" };

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
                    new CustomListPropertyDefinition { Name = "Port", Value = "47808" },
                    new CustomListPropertyDefinition { Name = "Address", Value = "" },
                    new CustomListPropertyDefinition { Name = "DeviceId", Value = "" }
                },
            };
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            var customListColumnCollection = new CustomListColumnCollection();
            for (int i = 0; i < _propertyNames.Length; i++)
            {
                customListColumnCollection.Add(new CustomListColumn(_propertyNames[i],CustomListColumnTypes.String));
            }
            return customListColumnCollection;
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            try
            {
                int tcpPort = int.Parse(data.Properties["Port"]);
                BacnetAddress address = new BacnetAddress(BacnetAddressTypes.IP, data.Properties["Address"]);
                uint deviceId = uint.Parse(data.Properties["DeviceId"]);

                var transport = new BacnetIpUdpProtocolTransport(tcpPort);
                BacnetClient client = new BacnetClient(transport);
                client.Start();
                RetrieveAvailableObjects(client, address, deviceId);

                var objectElementCollection = new CustomListObjectElementCollection();
    
                foreach (var item in _objects)
                {
                    var itemElement = new CustomListObjectElement();
                    string[] nameWithInstance = item.ToString().Split(':');
                    if (nameWithInstance.Length > 1)
                    {
                        string objectName = nameWithInstance[0];
                        string objectInstance = nameWithInstance[1];
                        string mappedName = _bacnetObjectMap.FirstOrDefault(b => b.Value.ToString() == objectName).Key ?? objectName;
                       
                        for (int i = 0; i < _propertyNames.Length; i++)
                        {
                            //Type
                            if (i==5)
                            {
                                itemElement.Add(_propertyNames[i], mappedName);
                                continue;
                            }
                            
                            //Instance number
                            if (i==6)
                            {
                                itemElement.Add(_propertyNames[i], objectInstance);
                                continue;
                            }
                            
                            string rawValue = GetPropertyValue(client, address, mappedName, objectInstance, _propertyNames[i]);
                            string value = rawValue.Contains("ERROR_CLASS_PROPERTY: ERROR_CODE_UNKNOWN_PROPERTY")?"":rawValue;
                            
                            //if _propertyNames[i] == "Unit"
                            if (i==2)
                            {
                                switch (rawValue)
                                {
                                    case "62":
                                        value = "\u00b0C";
                                        break;
                                    case "90":
                                        value = "\u00b0";
                                        break;
                                    case "66":
                                        value = "\u00b0F·d";
                                        break;
                                    case "65":
                                        value = "\u00b0C·d";
                                        break;
                                    case "91":
                                        value = "\u00b0C/h";
                                        break;
                                }
                            }
                           
                            itemElement.Add(_propertyNames[i], value);
                        }
                        
                    }
                    objectElementCollection.Add(itemElement);

                }
                client.Dispose();
                return objectElementCollection;
            }
            catch (Exception ex)
            {
               throw new Exception($"Error retrieving BACnet objects: {ex.Message}", ex);
            }
        }

        private void RetrieveAvailableObjects(BacnetClient client, BacnetAddress address, uint deviceId)
        {
            var objectId = new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, deviceId);
            var propertyId = BacnetPropertyIds.PROP_OBJECT_LIST;

            try
            {
                if (!client.ReadPropertyRequest(address, objectId, propertyId, out _objects))
                {
                    throw new Exception("Failed to read object list.");
                }

                Log.Info($"Objects count = {_objects.Count}");
            }
            catch (Exception ex)
            {
                Log.Error($"Error reading object list: {ex}");
            }
        }

        private string GetPropertyValue(BacnetClient client, BacnetAddress address, string objectName, string instance,
            string propertyName)
        {
            if (!_bacnetObjectsMap.TryGetValue(objectName, out var type))
                throw new ArgumentException($"Invalid object name: {objectName}");
            if (!uint.TryParse(instance, out uint objectInstance))
                throw new ArgumentException($"Invalid object instance: {instance}");
            var objectId = new BacnetObjectId(type, objectInstance);
            
            var propertyId = GetBacnetPropertyType(propertyName);
            
            BacnetPropertyReference[] request =
            {
                new BacnetPropertyReference(propertyId,
                    System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL)
            };
            if (!client.ReadPropertyMultipleRequest(address, objectId, request, out var readResults))
            {
                throw new Exception("Error: Failed to retrieve properties from the BACnet device.");
            }
            if (propertyId == (uint) BacnetPropertyIds.PROP_ALL)
            {
               return readResults[0].values.Count.ToString();
            }

            return GetPropertyValueAsString(readResults[0].values[0]);
        }

        private string GetPropertyValueAsString(BacnetPropertyValue property)
        {
            if (property.value == null || property.value.Count == 0)
                return null;
            BacnetValue[] values = new BacnetValue[property.value.Count];
            property.value.CopyTo(values, 0);

            if (property.property.propertyIdentifier == (uint)BacnetPropertyIds.PROP_STATUS_FLAGS)
            {
                string value = values[0].Value?.ToString();
                switch (value)
                {
                    case "0000":
                        return "Normal";
                    case "1000":
                        return "In alarm";
                    case "0100":
                        return "Fault";
                    case "0010":
                        return "Overridden";
                    case "0001":
                        return "Out of service";
                    default:
                        var parts = new List<string>();
                        if (value?[0]=='1') parts.Add("In alarm");
                        if (value?[1]=='1') parts.Add("Fault");
                        if (value?[2]=='1') parts.Add("Overridden");
                        if (value?[3]=='1') parts.Add("Out of service");
                        return parts.Any()
                            ? string.Join(", ", parts)
                            : "Unknown status";
                }
            }
            
            
            if (values.Length == 1)
                return values[0].Value?.ToString();
            
            if (values.Length > 1)
            {
                List<string> valesOfArray = new List<string>();
                foreach (var bacnetValue in values)
                {
                    valesOfArray.Add(bacnetValue.Value.ToString());
                }

                return JsonConvert.SerializeObject(valesOfArray);
            }

            return "";
        }

        private uint GetBacnetPropertyType(string property)
        {
            switch (property)
            {
                case "PresentValue":
                    return (uint) BacnetPropertyIds.PROP_PRESENT_VALUE;
                case "StatusFlags":
                    return (uint) BacnetPropertyIds.PROP_STATUS_FLAGS;
                case "Description":
                    return (uint) BacnetPropertyIds.PROP_DESCRIPTION;
                case "ObjectName":
                    return (uint) BacnetPropertyIds.PROP_OBJECT_NAME;
                case "Unit":
                    return (uint) BacnetPropertyIds.PROP_UNITS;
                case "Props":
                    return (uint) BacnetPropertyIds.PROP_ALL;
                default:
                    throw new ArgumentException($"Invalid property: {property}");
            }
        }
    }
}
