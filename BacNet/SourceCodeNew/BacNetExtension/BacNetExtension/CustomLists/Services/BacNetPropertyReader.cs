using System.IO.BACnet;
using BacNetExtension.CustomLists.Helpers;
using Newtonsoft.Json;
using Peakboard.ExtensionKit;

namespace BacNetExtension.CustomLists.Services
{
    public class BacNetPropertyReader
    {
        private readonly BacnetClient _client;
        private readonly Dictionary<string, BacnetObjectTypes> _objectMap;
        private readonly Action<string, string> _logCallback;
        private List<BacnetObjectDescription> _objectsDescriptionExternal;
        private List<BacnetObjectDescription> _objectsDescriptionDefault;
        private readonly BacNetLoggingHelper _bacnetLoggingHelper;

        public BacNetPropertyReader(BacnetClient client, Dictionary<string, BacnetObjectTypes> objectMap,Action<string,string> logCallback,ref List<BacnetObjectDescription> objectsDescriptionExternal,ref List<BacnetObjectDescription> objectsDescriptionDefault)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _objectMap = objectMap ?? throw new ArgumentNullException(nameof(objectMap));
            _logCallback = logCallback;
            _objectsDescriptionExternal = objectsDescriptionExternal;
            _objectsDescriptionDefault = objectsDescriptionDefault;
            _bacnetLoggingHelper = new BacNetLoggingHelper(_logCallback);
        }

        public Dictionary<string, string> GetSupportedPropertiesWithValues(BacnetAddress address, string objectName, string instance)
        {
            BacNetHelper.ValidateAddress(address);
            BacNetHelper.ValidateInstanceNumber(instance);

            var properties = new Dictionary<string, string>();
            
            var type = BacNetHelper.GetObjectTypeFromName(_objectMap, objectName);

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
                else
                {
                    throw new Exception("Failed to read properties from the BACnet device.");
                }
            }
            catch (Exception exception)
            {
                _bacnetLoggingHelper.LogWarning($"Failed to read BACnet properties: {exception}");
                try
                {
                    if (BacNetHelper.ReadAllPropertiesBySingle(_client, address, objectId, out var results, ref _objectsDescriptionExternal, ref _objectsDescriptionDefault))
                    {
                        properties.Clear();
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
                    else
                    {
                       throw new Exception("Couldn't fetch properties");
                    }
                }
                catch (Exception ex)
                {
                    _bacnetLoggingHelper.LogError($"Failed to read BACnet properties: {exception.Message}", ex);
                    properties["Error"] = $"Failed to read BACnet properties: {ex}";
                }
            }
            return properties;
        }

        public Dictionary<BacnetPropertyIds, CustomListColumnTypes> GetPropetiesWithTypes(BacnetAddress address, string objectName, string instance)
        {
            BacNetHelper.ValidateAddress(address);
            BacNetHelper.ValidateInstanceNumber(instance);
            
            var properties = new Dictionary<BacnetPropertyIds, CustomListColumnTypes>();
            
            var type = BacNetHelper.GetObjectTypeFromName(_objectMap, objectName);
          
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
                    throw new Exception("Error: Failed to retrieve properties from the BACnet device.");
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
                        if (propValue.value.Count > 1)
                        {
                            properties[propertyName] = CustomListColumnTypes.String;
                            continue;
                        }
                        
                        BacnetValue firstValue = propValue.value[0];
                        properties[propertyName] = GetColumnType(firstValue.Tag.ToString());
                    }
                }
            }
            catch (Exception exception)
            {
                _bacnetLoggingHelper.LogWarning($"Failed to read BACnet properties: {exception}");
                try
                {
                    if (BacNetHelper.ReadAllPropertiesBySingle(_client, address, objectId, out var results, ref _objectsDescriptionExternal, ref _objectsDescriptionDefault))
                    {
                        properties.Clear();
                        foreach (var result in results)
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
                    else
                    {
                        throw new Exception("Couldn't fetch properties");
                    }
                }
                catch (Exception ex)
                {
                    _bacnetLoggingHelper.LogError($"Failed to read BACnet properties: {ex}", ex);
                }
            }
            return properties;
        }
        public static string GetPropertyValueAsString(BacnetPropertyValue property)
        {
            if (property.value == null || property.value.Count == 0)
                return string.Empty;

            var values = new BacnetValue[property.value.Count];
            property.value.CopyTo(values, 0);
            
            if (property.property.propertyIdentifier == (uint) BacnetPropertyIds.PROP_STATUS_FLAGS)
            {
                string value = values[0].Value?.ToString();
                return BacNetHelper.GetStatusFlagsDescription(value);
            }
            
            if (property.property.propertyIdentifier == (uint) BacnetPropertyIds.PROP_UNITS)
            {
                string value = values[0].Value?.ToString();
                return BacNetHelper.GetUnitStringFromId(value);
            }

            if (property.property.propertyIdentifier == (uint) BacnetPropertyIds.PROP_RELIABILITY)
            {
                string value = values[0].Value?.ToString();
                return BacNetHelper.GetReliabilityDescription(value);
            }

            if (property.property.propertyIdentifier == (uint) BacnetPropertyIds.PROP_EVENT_STATE)
            {
                string value = values[0].Value?.ToString();
                return BacNetHelper.GetEventStateDescription(value);
            }
            
            if (values.Length == 1)
                return values[0].Value?.ToString();

            if (values.Length > 1)
            {
                var valuesList = values.Select(v => v.Value?.ToString()).ToList();
                var jsonArray =  JsonConvert.SerializeObject(valuesList);
                return jsonArray ;
            }

            return string.Empty;
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