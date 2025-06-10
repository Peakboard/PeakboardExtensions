#nullable enable
using Peakboard.ExtensionKit;
using System;
using System.Collections.Generic;
using System.IO.BACnet;
using System.Linq;
using BacNetExtension.CustomLists.Helpers;
using BacNetExtension.CustomLists.Services;

namespace BacNetExtension.CustomLists
{
    [Serializable]
    [CustomListIcon("BacNetExtension.pb_datasource_bacnet.png")]
    public class BacNetObjectsCustomList : CustomListBase
    {
        private readonly Dictionary<string, BacnetObjectTypes> _bacnetObjectNameToTypeMap;

        private List<BacnetObjectDescription> _objectsDescriptionExternal = new List<BacnetObjectDescription>();
        private List<BacnetObjectDescription> _objectsDescriptionDefault = new List<BacnetObjectDescription>();
        private IList<BacnetValue>? _objects = new List<BacnetValue>();
        private string[] _propertyNames = { "ObjectName", "PresentValue", "Unit", "StatusFlags", "Description", "Type","InstanceNumber","Props" };

        public BacNetObjectsCustomList()
        {
            _bacnetObjectNameToTypeMap = Enum.GetValues(typeof(BacnetObjectTypes))
                .Cast<BacnetObjectTypes>()
                .Where(e => e.ToString().StartsWith("OBJECT_") ||
                            e == BacnetObjectTypes.MAX_BACNET_OBJECT_TYPE ||
                            e == BacnetObjectTypes.MAX_ASHRAE_OBJECT_TYPE)
                .ToDictionary(
                    e => e.ToString().Replace("OBJECT_", "").ToPascalCase(),
                    e => e,
                    StringComparer.OrdinalIgnoreCase
                );
        }

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
            BacnetClient client = null;
            try
            {
                client = BacNetHelper.CreateBacNetClient(data);
                BacnetAddress address = new BacnetAddress(BacnetAddressTypes.IP, data.Properties["Address"]);
                uint deviceId = uint.Parse(data.Properties["DeviceId"]);
                _objects = RetrieveAvailableObjects(client, address, deviceId);

                var objectElementCollection = new CustomListObjectElementCollection();
                if (_objects != null && _objects.Count > 0)
                {
                    foreach (var item in _objects)
                    {
                        try
                        {
                            var itemElement = new CustomListObjectElement();
                            string[] nameWithInstance = item.ToString().Split(':');
                            if (nameWithInstance.Length > 1)
                            {
                                string objectName = nameWithInstance[0];
                                string objectInstance = nameWithInstance[1];
                                string mappedName = _bacnetObjectNameToTypeMap.FirstOrDefault(b => b.Value.ToString() == objectName).Key ?? objectName;
                                
                                for (int i = 0; i < _propertyNames.Length; i++)
                                {
                                    // TYPE
                                    if (i == 5)
                                    {
                                        itemElement.Add(_propertyNames[i], mappedName);
                                        continue;
                                    }
                                    // INSTANCE NUMBER
                                    if (i == 6)
                                    {
                                        itemElement.Add(_propertyNames[i], objectInstance);
                                        continue;
                                    }
                                    string rawValue = GetPropertyValue(client, address, item.Value, _propertyNames[i]) ?? "";
                                    string value = rawValue.Contains("ERROR_CLASS_PROPERTY: ERROR_CODE_UNKNOWN_PROPERTY") ? "" : rawValue;
                                    itemElement.Add(_propertyNames[i], value);
                                }
                            }
                            objectElementCollection.Add(itemElement);
                        }
                        catch (Exception e)
                        {
                            Log.Error($"Failed to process item {item.Tag}: {e}");
                        }
                    }
                }
                else
                {
                    throw new Exception("No objects found. Please check the connection and device ID.");
                }
                return objectElementCollection;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to retrieve objects in GetItemsOverride(): {ex}");
                throw;
            }
            finally
            {
                client?.Dispose();
                Log.Info("BacnetClient disposed");
            }
        }

        private IList<BacnetValue>? RetrieveAvailableObjects(BacnetClient client, BacnetAddress address, uint deviceId)
        {
            var objectId = new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, deviceId);
            var propertyId = BacnetPropertyIds.PROP_OBJECT_LIST;
            
            IList<BacnetValue>? objects = null;
            
            try
            {
                if (!client.ReadPropertyRequest(address, objectId, propertyId, out  objects))
                {
                    Log.Warning("Didn't get response from 'Object List'");
                    objects = null;
                }
                return objects;
            }
            catch (Exception ex)
            {
                Log.Error($"Got exception from 'Object List': {ex}");
            }
            return null;
        }

        private string? GetPropertyValue(BacnetClient client, BacnetAddress address, object objectID, string propertyName)
        {
            if (objectID is BacnetObjectId)
            {
                var objectId = (BacnetObjectId) objectID;
                var propertyId = GetBacnetPropertyType(propertyName);

                BacnetPropertyReference[] request =
                {
                    new BacnetPropertyReference(propertyId, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL)
                };
                try
                {
                    if (!client.ReadPropertyMultipleRequest(address, objectId, request, out var readResults))
                    {
                        throw new Exception("Error: Failed to retrieve properties from the BACnet device.");
                    }

                    if (propertyId == (uint) BacnetPropertyIds.PROP_ALL)
                    {
                        return readResults[0].values.Count.ToString();
                    }

                    var result = BacNetPropertyReader.GetPropertyValueAsString(readResults[0].values[0]);
                    return result;
                }
                catch (Exception ex1)
                {
                    Log.Warning($"ReadPropertyMultipleRequest Exception: {ex1}");
                    try
                    {
                        if (!BacNetHelper.ReadAllPropertiesBySingle(client, address, objectId, out var multiValueList,
                                ref _objectsDescriptionExternal, ref _objectsDescriptionDefault))
                        {
                            Log.Error("Couldn't fetch properties.");
                            return string.Empty;
                        }

                        var result = BacNetPropertyReader.GetPropertyValueAsString(multiValueList[0].values[0]);
                        return result;
                    }
                    catch (Exception ex2)
                    {
                        Log.Error($"Error while reading properties: {ex2}");
                    }

                    return string.Empty;
                }
            }
            else
            {
                Log.Warning($"Invalid object ID type: {objectID.GetType().Name}. Expected BacnetObjectId.");
            }

            return string.Empty;
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
