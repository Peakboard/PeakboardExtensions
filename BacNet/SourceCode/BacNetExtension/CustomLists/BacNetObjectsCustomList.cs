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
        private string[] _propertyNames = { "Objectname", "Presentvalue", "Unit", "Statusflags", "Description", "Instancenumber" };

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
            return new CustomListColumnCollection()
            {
                new CustomListColumn("Objectname", CustomListColumnTypes.String),
                new CustomListColumn("Presentvalue", CustomListColumnTypes.String),
                new CustomListColumn("Unit", CustomListColumnTypes.String),
                new CustomListColumn("Statusflags", CustomListColumnTypes.String),
                new CustomListColumn("Description", CustomListColumnTypes.String),
                new CustomListColumn("Instancenumber", CustomListColumnTypes.String),
            };
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            try
            {
                int tcpPort = int.Parse("47808");
                var address = new BacnetAddress(BacnetAddressTypes.IP, "192.168.193.92:52193");
                uint deviceId = uint.Parse("1228812");

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
                        string objectinstance = nameWithInstance[1];
                        string mappedName = _bacnetObjectMap.FirstOrDefault(b => b.Value.ToString() == objectName).Key ?? objectName;
                        itemElement.Add(_propertyNames[0], mappedName);
                        for (int i = 1; i < _propertyNames.Length-1; i++)
                        {
                            string rawValue = GetPropertyValue(client, address, mappedName, objectinstance, _propertyNames[i]);
                            string value = rawValue.Contains("ERROR_CLASS_PROPERTY: ERROR_CODE_UNKNOWN_PROPERTY")?"":rawValue;
                            itemElement.Add(_propertyNames[i], value);
                        }
                        itemElement.Add(_propertyNames[_propertyNames.Length-1], objectinstance);
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

            BacnetPropertyReference[] request =
            {
                new BacnetPropertyReference(GetBacnetPropertyType(propertyName),
                    System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL)
            };
            if (!client.ReadPropertyMultipleRequest(address, objectId, request, out var readResults))
            {
                throw new Exception("Error: Failed to retrieve properties from the BACnet device.");
            }

            return GetPropertyValueAsString(readResults[0].values[0]);
        }

        private string GetPropertyValueAsString(BacnetPropertyValue property)
        {
            if (property.value == null || property.value.Count == 0)
                return null;
            BacnetValue[] values = new BacnetValue[property.value.Count];
            property.value.CopyTo(values, 0);

            if (values.Length == 1)
                return values[0].Value?.ToString() ?? null;
            if (values.Length > 1)
            {
                List<string> valesOfArray = new List<string>();
                foreach (var bacnetValue in values)
                {
                    valesOfArray.Add(bacnetValue.Value.ToString());
                }

                return JsonConvert.SerializeObject(valesOfArray);
            }

            return null;
        }

        private uint GetBacnetPropertyType(string property)
        {
            switch (property)
            {
                case "Presentvalue":
                    return (uint) BacnetPropertyIds.PROP_PRESENT_VALUE;
                case "Statusflags":
                    return (uint) BacnetPropertyIds.PROP_STATUS_FLAGS;
                case "Description":
                    return (uint) BacnetPropertyIds.PROP_DESCRIPTION;
                case "Objectname":
                    return (uint) BacnetPropertyIds.PROP_OBJECT_NAME;
                case "Unit":
                    return (uint) BacnetPropertyIds.PROP_UNITS;
                default:
                    throw new ArgumentException($"Invalid property: {property}");
            }
        }
    }
}
