using System;
using System.Globalization;
using System.IO.BACnet;
using System.Linq;
using BacNetExtension.CustomLists.Exceptions;
using BacNetExtension.CustomLists.Helpers;
using BacNetExtension.CustomLists.Interfaces;

namespace BacNetExtension.CustomLists.Services
{
    public class BacNetPropertyWriter
    {
        private readonly IBacNetClient _client;
        private readonly Dictionary<string, BacnetPropertyIds> _propertyMap;
        private readonly Dictionary<string, BacnetObjectTypes> _objectMap;

        public BacNetPropertyWriter(
            IBacNetClient client, 
            Dictionary<string, BacnetPropertyIds> propertyMap,
            Dictionary<string, BacnetObjectTypes> objectMap)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _propertyMap = propertyMap ?? throw new ArgumentNullException(nameof(propertyMap));
            _objectMap = objectMap ?? throw new ArgumentNullException(nameof(objectMap));
        }

        public void WriteProperty(BacnetAddress address, string objectName, string instanceId, string propertyName, string value)
        {
            try
            {
                ValidateInputs(address, objectName, instanceId, propertyName, value);

                var (tag, isArray) = IsPropertyValueArray(address, objectName, instanceId, propertyName);
                var objectId = new BacnetObjectId(_objectMap[objectName], uint.Parse(instanceId));
                var propertyId = _propertyMap[propertyName];

                var valueList = CreateValueList(value, tag, isArray);
                WritePropertyToDevice(address, objectId, propertyId, valueList);
            }
            catch (Exception ex)
            {
                BacNetLoggingHelper.LogError($"Error writing to BACnet: {ex.Message}", ex);
                throw new BacNetPropertyException($"Failed to write property: {ex.Message}", ex);
            }
        }

        private void ValidateInputs(BacnetAddress address, string objectName, string instanceId, string propertyName, string value)
        {
            BacNetValidationHelper.ValidateAddress(address);

            if (string.IsNullOrWhiteSpace(objectName))
                throw new BacNetValidationException("Object name cannot be empty");

            if (!_objectMap.ContainsKey(objectName))
                throw new BacNetValidationException($"Object {objectName} not found");

            BacNetValidationHelper.ValidateInstanceNumber(instanceId);

            if (string.IsNullOrWhiteSpace(propertyName))
                throw new BacNetValidationException("Property name cannot be empty");

            if (!_propertyMap.ContainsKey(propertyName))
                throw new BacNetValidationException($"Property {propertyName} not found");

            if (value == null)
                throw new BacNetValidationException("Value cannot be null");
        }

        private (BacnetApplicationTags Tag, bool IsArray) IsPropertyValueArray(BacnetAddress address, string objectName, string instance, string propertyName)
        {
            var objectId = new BacnetObjectId(_objectMap[objectName], uint.Parse(instance));
            var propertyId = _propertyMap[propertyName];

            var request = new[]
            {
                new BacnetPropertyReference((uint)propertyId, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL)
            };

            if (_client.ReadPropertyMultipleRequest(address, objectId, request, out var results))
            {
                foreach (var result in results)
                {
                    foreach (var prop in result.values)
                    {
                        if (prop.property.propertyIdentifier == (uint)propertyId)
                        {
                            if (prop.value == null || prop.value.Count == 0)
                            {
                                throw new BacNetPropertyException($"Property {propertyName} has no values");
                            }

                            var values = new BacnetValue[prop.value.Count];
                            prop.value.CopyTo(values, 0);
                            return (values[0].Tag, values.Length > 1);
                        }
                    }
                }
            }

            throw new BacNetPropertyException($"Failed to determine property type for {propertyName}");
        }

        private BacnetValue[] CreateValueList(string value, BacnetApplicationTags tag, bool isArray)
        {
            if (isArray)
            {
                var values = value.Split(',');
                return values.Select(v => new BacnetValue(tag, ConvertValue(v.Trim(), tag))).ToArray();
            }

            return new[] { new BacnetValue(tag, ConvertValue(value, tag)) };
        }

        private object ConvertValue(string value, BacnetApplicationTags tag)
        {
            switch (tag)
            {
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_REAL:
                    if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue))
                        return floatValue;
                    break;

                case BacnetApplicationTags.BACNET_APPLICATION_TAG_BOOLEAN:
                    if (bool.TryParse(value, out bool boolValue))
                        return boolValue;
                    break;

                case BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT:
                    if (uint.TryParse(value, out uint uintValue))
                        return uintValue;
                    break;

                case BacnetApplicationTags.BACNET_APPLICATION_TAG_CHARACTER_STRING:
                    return value;

                case BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED:
                    if (int.TryParse(value, out int enumValue))
                        return enumValue;
                    break;

                default:
                    throw new BacNetPropertyException($"Unsupported BACnet type: {tag}");
            }

            throw new BacNetPropertyException($"Invalid value for type {tag}: {value}");
        }

        private void WritePropertyToDevice(BacnetAddress address, BacnetObjectId objectId, BacnetPropertyIds propertyId, BacnetValue[] values)
        {
            if (!_client.WritePropertyRequest(address, objectId, propertyId, values))
            {
                throw new BacNetPropertyException($"Failed to write property {propertyId} to device");
            }

            BacNetLoggingHelper.LogInfo($"Property {propertyId} successfully written");
        }
    }
} 