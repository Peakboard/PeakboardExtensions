using System;
using System.Collections.Generic;
using System.IO.BACnet;
using System.Linq;
using BacNetExtension.CustomLists.Helpers;
using Peakboard.ExtensionKit;

namespace BacNetExtension.CustomLists.Services
{
    public class BacNetPropertyReader
    {
        private readonly BacnetClient _client;
        private readonly Dictionary<string, BacnetObjectTypes> _objectMap;
        private readonly Action<string, string> _logCallback;
        private readonly BacNetLoggingHelper _bacnetLoggingHelper;

        public BacNetPropertyReader(BacnetClient client, Dictionary<string, BacnetObjectTypes> objectMap,Action<string,string> logCallback)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _objectMap = objectMap ?? throw new ArgumentNullException(nameof(objectMap));
            _logCallback = logCallback;
            _bacnetLoggingHelper = new BacNetLoggingHelper(_logCallback);
        }

        public Dictionary<string, string> GetSupportedPropertiesWithValues(BacnetAddress address, string objectName, string instance)
        {
            BacNetHelper.ValidateAddress(address);
            BacNetHelper.ValidateInstanceNumber(instance);

            var properties = new Dictionary<string, string>();
            if (!_objectMap.TryGetValue(objectName, out var type))
                return properties;

            var objectId = new BacnetObjectId(type, uint.Parse(instance));
            BacNetHelper.ValidateObjectId(objectId);

            try
            {
                var request = new[]
                {
                    new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_ALL, 
                        System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL)
                };

                if (_client.ReadPropertyMultipleRequest(address, objectId, request, out var results))
                {
                    foreach (var result in results)
                    {
                        foreach (var prop in result.values)
                        {
                            string name = ((BacnetPropertyIds)prop.property.propertyIdentifier).ToString();
                            string value = GetPropertyValueAsString(prop);
                            properties[name] = value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _bacnetLoggingHelper.LogError($"Failed to read BACnet properties: {ex.Message}", ex);
                properties["Error"] = $"Failed to read BACnet properties: {ex.Message}";
            }

            return properties;
        }

        public Dictionary<BacnetPropertyIds, CustomListColumnTypes> GetPropetiesWithTypes(BacnetAddress address, string objectName, string instance)
        {
            BacNetHelper.ValidateAddress(address);
            BacNetHelper.ValidateInstanceNumber(instance);
            
            var properties = new Dictionary<BacnetPropertyIds, CustomListColumnTypes>();
            if (!_objectMap.TryGetValue(objectName, out var type))
                throw new ArgumentException($"Error: Object '{objectName}' not found in BACnet map.");
            

            BacnetObjectId objectId;

            if (instance.Contains('-'))
            {
                foreach (var singleInstance in instance.Split('-'))
                {
                    if (!uint.TryParse(singleInstance, out uint objectInstance))
                    {
                        throw new ArgumentException($"Error: '{instance}' is not a valid number.");
                    }
                }

                objectId = new BacnetObjectId(type, uint.Parse(instance.Split('-').First()));
            }
            else
            {
                foreach (var singleInstance in instance.Split(';'))
                {
                    if (!uint.TryParse(singleInstance, out uint objectInstance))
                    {
                        throw new ArgumentException($"Error: '{instance}' is not a valid number.");
                    }
                }

                objectId = new BacnetObjectId(type, uint.Parse(instance.Split(';').First()));
            }
            
            try
            {
                BacnetPropertyReference[] propertyReferences =
                {
                    new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_ALL,
                        System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL)
                };

                if (!_client.ReadPropertyMultipleRequest(address, objectId, propertyReferences, out var readResults))
                {
                    _bacnetLoggingHelper.LogError("Error: Failed to retrieve properties from the BACnet device.",new Exception());
                    return properties;
                }

                foreach (var result in readResults)
                {
                    foreach (var propValue in result.values)
                    {
                        BacnetPropertyIds propertyName = (BacnetPropertyIds)propValue.property.propertyIdentifier;
                        if (propValue.value == null || propValue.value.Count == 0)
                        {
                            properties[propertyName] = CustomListColumnTypes.String;
                            continue;
                        }
                        BacnetValue firstValue = propValue.value[0];
                        string tagName = firstValue.Tag.ToString();
                        CustomListColumnTypes columnType = GetColumnType(tagName);
                        properties[propertyName] = columnType;
                    }
                }
            }
            catch (Exception ex)
            {
                _bacnetLoggingHelper.LogError($"Exception: Failed to retrieve BACnet properties. Details: {ex.Message}",ex);
            }
            return properties;
        }
        private string GetPropertyValueAsString(BacnetPropertyValue property)
        {
            if (property.value == null || property.value.Count == 0)
                return null;

            var values = new BacnetValue[property.value.Count];
            property.value.CopyTo(values, 0);

            if (values.Length == 1)
                return values[0].Value?.ToString();

            if (values.Length > 1)
            {
                var valuesList = values.Select(v => v.Value?.ToString()).ToList();
                return string.Join(",", valuesList);
            }

            return null;
        }
        private CustomListColumnTypes GetColumnType(string tagName)
        {
            if (tagName.Contains("BOOLEAN"))
                return CustomListColumnTypes.Boolean;

            if (tagName.Contains("INT") || tagName.Contains("REAL") || tagName.Contains("DOUBLE") || tagName.Contains("ID"))
                return CustomListColumnTypes.Number;

            return CustomListColumnTypes.String;
        }
    }
} 