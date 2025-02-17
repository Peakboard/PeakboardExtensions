using Newtonsoft.Json;
using Peakboard.ExtensionKit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.BACnet;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace BacNetExtension.CustomLists
{
    public class BacNetPropertysCustomList : CustomListBase
    {
        //BacnetPropertyIds
        Dictionary<string, BacnetPropertyIds> bacnetPropertiesMap = Enum.GetValues(typeof(BacnetPropertyIds))
            .Cast<BacnetPropertyIds>()
            .Where(e => e.ToString().StartsWith("PROP_"))
            .ToDictionary(e => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(e.ToString().Replace("PROP_", "").Replace("_", "").ToLower()), e => e);
        Dictionary<string, BacnetObjectTypes> bacnetObjectMap = Enum.GetValues(typeof(BacnetObjectTypes))
            .Cast<BacnetObjectTypes>()
            .Where(e => e.ToString().StartsWith("OBJECT_"))
            .ToDictionary(e => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(e.ToString().Replace("OBJECT_", "").Replace("_", "").ToLower()), e => e);
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
                Functions =
                {
                    new CustomListFunctionDefinition()
                    {
                        Name = "WriteData",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            new CustomListFunctionInputParameterDefinition()
                            {
                                Name = "BACnetProperty",
                                Description = "Enter BACnet property (e.g., Present Value): ",
                                Type = CustomListFunctionParameterTypes.String,
                            },new CustomListFunctionInputParameterDefinition()
                            {
                                Name = "Value",
                                Description = "Enter value:",
                                Type = CustomListFunctionParameterTypes.String,
                            }
                        }
                    }
                },
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
            try
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
                foreach (var value in values)
                {
                    string oldName = value.Key.ToString();
                    string newName = bacnetPropertiesMap.FirstOrDefault(p => p.Value.ToString() == oldName).Key;
                    if (newName != null)
                    {
                        customListCollection.Add(new CustomListColumn(newName, value.Value));
                    }
                    else
                    {
                        customListCollection.Add(new CustomListColumn(oldName, value.Value));
                    }
                }
                if (values == null || values.Count <= 0)
                {
                    new CustomListColumn("Error see Log", CustomListColumnTypes.String);
                }
                return customListCollection;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("ERROR_CLASS_OBJECT - ERROR_CODE_UNKNOWN_OBJECT"))
                {
                    throw new Exception("The object name or instance ID is incorrect");
                }
                throw new Exception(ex.Message);
            }
        }
        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            try
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

                foreach (var value in values)
                {
                    if (!added.Contains(value.Key))
                    {
                        string oldKey = value.Key;
                        string newKey = bacnetPropertiesMap.FirstOrDefault(p => p.Value.ToString() == oldKey).Key;

                        if (newKey != null)
                        {
                            var splitted = value.Value.Split(':');
                            if (splitted.Length > 1)
                            {
                                itemElement.Add(newKey, splitted[1]);
                                added.Add(value.Key);
                            }
                            else
                            {
                                itemElement.Add(newKey, value.Value);
                                added.Add(value.Key);
                            }
                        }
                        else
                        {
                            itemElement.Add(value.Key, value.Value);
                            added.Add(value.Key);
                        }
                    }
                }
                customListObjectColl.Add(itemElement);
                return customListObjectColl;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in GetItemsOverride: {ex.Message}");
            }
        }

        protected override CustomListExecuteReturnContext ExecuteFunctionOverride(CustomListData data, CustomListExecuteParameterContext context)
        {
            string ipAddress = data.Properties["Ip"].ToString();
            int tcpPort = int.Parse(data.Properties["Port"]);
            BacnetAddress adddress = new BacnetAddress(BacnetAddressTypes.IP, data.Properties["Address"]);
            string objectName = data.Properties["ObjectName"];
            string objectInstance = data.Properties["ObjectInstance"];
            var transport = new BacnetIpUdpProtocolTransport(tcpPort, false, false, 1472, ipAddress);
            client = new BacnetClient(transport);
            client.Start();
            string functionName = context.FunctionName;
            switch (functionName)
            {
                case "WriteData":
                    string propertyName = context.Values[0].StringValue;
                    string value = context.Values[1].StringValue;
                    WriteProperty(adddress, objectName, objectInstance, propertyName, value);
                    break;
                default:
                    break;
            }
            return new CustomListExecuteReturnContext
                {
                    new CustomListObjectElement
                    {
                        { "Result", "null" }
                    }
                };
        }
        private Dictionary<string, string> GetSupportedPropertiesWithValues(BacnetAddress address, string objectName, string instance)
        {
            var properties = new Dictionary<string, string>();
            if (!bacnetObjectMap.TryGetValue(objectName, out var type))
                return properties;
            if (!uint.TryParse(instance, out uint objectInstance))
                return properties;
            var objectId = new BacnetObjectId(type, objectInstance);
            try
            {
                BacnetPropertyReference[] request =
                {
                    new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_ALL, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL)
                };
                if (client.ReadPropertyMultipleRequest(address, objectId, request, out var results))
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
                properties["Error"] = $"Failed to read BACnet properties: {ex.Message}";
            }
            return properties;
        }

        public Dictionary<BacnetPropertyIds, CustomListColumnTypes> GetPropetiesWithTypes(BacnetAddress address, string objectName, string instance)
        {
            var properties = new Dictionary<BacnetPropertyIds, CustomListColumnTypes>();
            if (!bacnetObjectMap.TryGetValue(objectName, out var type))
            {
                Log.Error($"Error: Object '{objectName}' not found in BACnet map.");
                return properties;
            }
            if (!uint.TryParse(instance, out uint objectInstance))
            {
                Log.Error($"Error: '{instance}' is not a valid number.");
                return properties;
            }
            var objectId = new BacnetObjectId(type, objectInstance);
            try
            {
                BacnetPropertyReference[] propertyReferences =
                {
                    new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_ALL,
                        System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL)
                };
                if (!client.ReadPropertyMultipleRequest(address, objectId, propertyReferences, out var readResults))
                {
                    Log.Error("Error: Failed to retrieve properties from the BACnet device.");
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
                Log.Error($"Exception: Failed to retrieve BACnet properties. Details: {ex.Message}");
            }
            return properties;
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
        private CustomListColumnTypes GetColumnType(string tagName)
        {
            if (tagName.Contains("BOOLEAN"))
                return CustomListColumnTypes.Boolean;

            if (tagName.Contains("INT") || tagName.Contains("REAL") || tagName.Contains("DOUBLE") || tagName.Contains("ID"))
                return CustomListColumnTypes.Number;

            return CustomListColumnTypes.String;
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
      
        public void WriteProperty(BacnetAddress address, string objectName, string instanceId, string propertyName, string value)
        {
            try
            {
                if (!bacnetObjectMap.TryGetValue(objectName, out BacnetObjectTypes bacnetObject))
                {
                    Log.Error($"Error: Object {objectName} not found.");
                    return;
                }

                if (!uint.TryParse(instanceId, out uint instance))
                {
                    Log.Error("Error: Instance ID must be a number.");
                    return;
                }

                if (!bacnetPropertiesMap.TryGetValue(propertyName, out BacnetPropertyIds propertyId))
                {
                    Log.Error($"Error: Property {propertyName} not found.");
                    return;
                }
                var (tag, isArray) = IsPropertyValueArray(address, objectName, instanceId, propertyName);
                var objectId = new BacnetObjectId(bacnetObject, instance);
                BacnetValue[] valueList;
                if (isArray)
                {
                    var deserializedValues = JsonConvert.DeserializeObject<string[]>(value);
                    if (deserializedValues == null || deserializedValues.Length == 0)
                    {
                        Log.Error("Error: Unable to deserialize data into an array.");
                        return;
                    }
                    valueList = deserializedValues
                        .Select(val => new BacnetValue(tag, ConvertUserInput(val, tag)))
                        .ToArray();
                }
                else
                {
                    object convertedValue = ConvertUserInput(value, tag);
                    valueList = new BacnetValue[] { new BacnetValue(tag, convertedValue) };
                }

                bool result = client.WritePropertyRequest(address, objectId, propertyId, valueList);

                if (result)
                    Log.Info($"Property {propertyName} successfully written.");
                else
                    Log.Error("Error writing to BACnet.");
            }
            catch (Exception ex)
            {
                Log.Error($"Error writing to BACnet: {ex.Message}");
            }
        }

        private (BacnetApplicationTags Tag, bool IsArray) IsPropertyValueArray(BacnetAddress address, string objectName,
            string instance, string propertyName)
        {
            if (!bacnetObjectMap.TryGetValue(objectName, out var type))
            {
                Log.Error($"Error: Object {objectName} not found.");
                return (BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL, false);
            }

            if (!uint.TryParse(instance, out uint objectInstance))
            {
                Log.Error("Error: Instance ID must be a number.");
                return (BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL, false);
            }

            if (!bacnetPropertiesMap.TryGetValue(propertyName, out BacnetPropertyIds bacProp))
            {
                Log.Error($"Error: Property {propertyName} not found.");
                return (BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL, false);
            }

            var objectId = new BacnetObjectId(type, objectInstance);

            try
            {
                BacnetPropertyReference[] request =
                {
                    new BacnetPropertyReference((uint)bacProp, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL)
                };

                if (client.ReadPropertyMultipleRequest(address, objectId, request, out var results))
                {
                    foreach (var result in results)
                    {
                        foreach (var prop in result.values)
                        {
                            if (prop.property.propertyIdentifier == (uint)bacProp)
                            {
                                if (prop.value == null || prop.value.Count == 0)
                                {
                                    Log.Error($"Error: Property {propertyName} has no values.");
                                    return (BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL, false);
                                }

                                BacnetValue[] values = new BacnetValue[prop.value.Count];
                                prop.value.CopyTo(values, 0);
                                BacnetApplicationTags tag = values[0].Tag;
                                bool isArray = values.Length > 1;

                                return (tag, isArray);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error reading property {propertyName}: {ex.Message}");
                return (BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL, false);
            }

            return (BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL, false);
        }
      
        private static object ConvertUserInput(string userInput, BacnetApplicationTags expectedType)
        {
            switch (expectedType)
            {
                case BacnetApplicationTags.BACNET_APPLICATION_TAG_REAL:
                    if (float.TryParse(userInput, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue))
                        return floatValue;
                    break;

                case BacnetApplicationTags.BACNET_APPLICATION_TAG_BOOLEAN:
                    if (bool.TryParse(userInput, out bool boolValue))
                        return boolValue;
                    break;

                case BacnetApplicationTags.BACNET_APPLICATION_TAG_UNSIGNED_INT:
                    if (uint.TryParse(userInput, out uint uintValue))
                        return uintValue;
                    break;

                case BacnetApplicationTags.BACNET_APPLICATION_TAG_CHARACTER_STRING:
                    return userInput;

                case BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED:
                    if (int.TryParse(userInput, out int enumValue))
                        return enumValue;
                    break;

                case BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID:
                    string[] elements = userInput.Split(',');
                    return elements.Select(e => e.Trim()).ToArray();

                default:
                    throw new Exception($"Unsupported BACnet type: {expectedType}");
            }

            throw new Exception($"Invalid value for type {expectedType}: {userInput}");
        }
    }
}
