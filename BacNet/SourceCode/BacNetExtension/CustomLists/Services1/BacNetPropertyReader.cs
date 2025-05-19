using System;
using System.Collections.Generic;
using System.IO.BACnet;
using System.Linq;
using BacNetExtension.CustomLists.Exceptions;
using BacNetExtension.CustomLists.Helpers;
using BacNetExtension.CustomLists.Interfaces;
using Peakboard.ExtensionKit;

namespace BacNetExtension.CustomLists.Services
{
    public class BacNetPropertyReader
    {
        private readonly IBacNetClient _client;
        private readonly Dictionary<string, BacnetPropertyIds> _propertyMap;

        public BacNetPropertyReader(IBacNetClient client, Dictionary<string, BacnetPropertyIds> propertyMap)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _propertyMap = propertyMap ?? throw new ArgumentNullException(nameof(propertyMap));
        }

        public Dictionary<string, string> GetSupportedPropertiesWithValues(BacnetAddress address, string objectName, string instance)
        {
            BacNetValidationHelper.ValidateAddress(address);
            BacNetValidationHelper.ValidateInstanceNumber(instance);

            var properties = new Dictionary<string, string>();
            if (!_propertyMap.TryGetValue(objectName, out var type))
                return properties;

            var objectId = new BacnetObjectId(type, uint.Parse(instance));
            BacNetValidationHelper.ValidateObjectId(objectId);

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
                BacNetLoggingHelper.LogError($"Failed to read BACnet properties: {ex.Message}", ex);
                properties["Error"] = $"Failed to read BACnet properties: {ex.Message}";
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
    }
} 