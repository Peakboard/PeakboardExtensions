using Newtonsoft.Json;
using Peakboard.ExtensionKit;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO.BACnet;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;

namespace BacNetExtension.CustomLists
{
    [Serializable]
    [CustomListIcon("BacNetExtension.pb_datasource_bacnet.png")]
    public class BacNetPropertiesCustomList : CustomListBase
    {
        private readonly Dictionary<string, BacnetPropertyIds> _bacnetPropertiesMap = Enum.GetValues(typeof(BacnetPropertyIds))
            .Cast<BacnetPropertyIds>()
            .Where(e => e.ToString().StartsWith("PROP_"))
            .ToDictionary(e => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(e.ToString().Replace("PROP_", "").Replace("_", "").ToLower()), e => e);

        private readonly Dictionary<string, BacnetObjectTypes> _bacnetObjectsMap = Enum.GetValues(typeof(BacnetObjectTypes))
            .Cast<BacnetObjectTypes>()
            .Where(e => e.ToString().StartsWith("OBJECT_"))
            .ToDictionary(e => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(e.ToString().Replace("OBJECT_", "").Replace("_", "").ToLower()), e => e);

        private string _listName;
        private uint _subscriptionIdCounter;
        private const uint _subsriptionDuraion = 120;
        private CustomListObjectElementCollection _oldValues;
        private Dictionary<uint,Timer> _covTimers = new Dictionary<uint, Timer>();
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "BacNetPropertiesCustomList",
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
                            },
                            new CustomListFunctionInputParameterDefinition()
                            {
                                Name = "InstanceOffObject",
                                Description = "Enter BACnet property (e.g., Present Value): ",
                                Type = CustomListFunctionParameterTypes.String,
                            },
                            new CustomListFunctionInputParameterDefinition()
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
                    new CustomListPropertyDefinition { Name = "Port", Value = "47808" },
                    new CustomListPropertyDefinition { Name = "Address", Value = "" },
                    new CustomListPropertyDefinition { Name = "Type", Value = "" },
                    new CustomListPropertyDefinition { Name = "Instancenumber", Value = "" },
                    new CustomListPropertyDefinition { Name = "SubscribeCOV", Value = "True" }

                },
            };
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            try
            {
                int tcpPort = int.Parse(data.Properties["Port"]);
                BacnetAddress adddress = new BacnetAddress(BacnetAddressTypes.IP, data.Properties["Address"]);
                string objectName = data.Properties["Type"];
                string objectInstance = data.Properties["Instancenumber"];

                BacnetIpUdpProtocolTransport transport = new BacnetIpUdpProtocolTransport(tcpPort);

                BacnetClient client = new BacnetClient(transport);
                client.Start();

                Dictionary<BacnetPropertyIds,CustomListColumnTypes> values = GetPropetiesWithTypes(client, adddress, objectName, objectInstance);
                CustomListColumnCollection customListCollection = new CustomListColumnCollection();

                foreach (var value in values)
                {
                    string oldName = value.Key.ToString();
                    string newName = _bacnetPropertiesMap.FirstOrDefault(p => p.Value.ToString() == oldName).Key;
                    customListCollection.Add(new CustomListColumn(newName ?? oldName, value.Value));
                }

                if (values == null || values.Count <= 0)
                {
                    throw new Exception("No values found");
                }

                client.Dispose();
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
                int tcpPort = int.Parse(data.Properties["Port"]);
                BacnetAddress address = new BacnetAddress(BacnetAddressTypes.IP, data.Properties["Address"]);
                string objectName = data.Properties["Type"];
                string objectInstance = data.Properties["Instancenumber"];

                BacnetIpUdpProtocolTransport transport = new BacnetIpUdpProtocolTransport(tcpPort);

                BacnetClient client = new BacnetClient(transport);
                client.Start();

                CustomListObjectElementCollection customListObjectColl = new CustomListObjectElementCollection();

                if (objectInstance.Contains('-'))
                {
                    int[] instances = objectInstance
                        .Split('-')
                        .Select(int.Parse)
                        .ToArray();
                    int[] instancesArray = Enumerable.Range(instances[0], instances[1] - instances[0] + 1).ToArray();

                    customListObjectColl = GetMultipleItems(client, address, objectName, instancesArray);
                }
                else
                {
                    foreach (var instance in objectInstance.Split(';'))
                    {
                        CustomListObjectElement item = GetSingleItem(client, address, objectName, instance);
                        customListObjectColl.Add(item);
                    }
                }

                client.Dispose();
                _oldValues = customListObjectColl;
                return customListObjectColl;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error in GetItemsOverride: {ex.Message}");
            }
        }

        protected override void SetupOverride(CustomListData data)
        {
            try
            {
                BacnetClient _client;
                _listName = data.ListName;
                Log.Info(_listName);
                if (bool.TryParse(data.Properties["SubscribeCOV"], out bool subscribeCov))
                {
                    if (subscribeCov)
                    {
                        if (int.TryParse(data.Properties["Port"], out int tcpPort))
                        {
                            BacnetAddress address =
                                new BacnetAddress(BacnetAddressTypes.IP, data.Properties["Address"]);
                            BacnetIpUdpProtocolTransport transport = new BacnetIpUdpProtocolTransport(tcpPort);
                            _client = new BacnetClient(transport);
                            _client.Start();
                            _client.OnCOVNotification += HandleCovNotification;

                            string objectName = data.Properties["ObjectName"];
                            string objectInstance = data.Properties["ObjectInstance"];
                            int[] instancesArray;
                            if (objectInstance.Contains('-'))
                            {
                                int[] instances = objectInstance
                                    .Split('-')
                                    .Select(int.Parse)
                                    .ToArray();
                                instancesArray = Enumerable.Range(instances[0], instances[1] - instances[0] + 1).ToArray();
                            }
                            else
                            {
                                instancesArray = objectInstance.Split(';').Select(int.Parse).ToArray();
                            }

                            bool getObjectName = _bacnetObjectsMap.TryGetValue(objectName, out var type);
                            if (!getObjectName) Log.Error("Invalid object name");
                            else
                            {
                                foreach (var instance in instancesArray)
                                {
                                    var obj = new BacnetObjectId(type, (uint)instance);
                                    _subscriptionIdCounter++;
                                    bool subscribed = _client.SubscribeCOVRequest(address, obj, _subscriptionIdCounter, false, true, _subsriptionDuraion);
                                    if (subscribed)
                                    {
                                        Log.Info("Subscription to the object " + obj + " has been established.");
                                        Timer timer = new Timer((_subsriptionDuraion/2) * 1000);
                                        timer.Elapsed += (sender, e) =>
                                        {
                                            ReSubscribe(address, objectName, instance, _subsriptionDuraion, _client);
                                        };
                                        timer.AutoReset = true;
                                        timer.Start();
                                        _covTimers.Add(_subscriptionIdCounter,timer);
                                    }
                                    else
                                       Log.Info("Failed to subscribe to the object " + obj);
                                }
                            }
                        }
                        else
                        {
                            throw new FormatException("The value for 'Port' is not a valid integer.");
                        }
                    }
                }
                else
                {
                    throw new FormatException("The value for 'SubscribeCOV' is not a valid boolean.");
                }
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
            }
        }

        private void HandleCovNotification(BacnetClient sender, BacnetAddress address, byte invokeId, uint subscriberProcessIdentifier, BacnetObjectId initiatingDeviceIdentifier, BacnetObjectId monitoredObjectIdentifier, uint timeRemaining, bool needConfirm, ICollection<BacnetPropertyValue> values, BacnetMaxSegments maxSegments)
        {
            try
            {
                if (values == null || values.Count == 0)
                {
                    Log.Error("values collection is null or empty.");
                    return;
                }

                foreach (var bacnetPropertyValue in values)
                {
                    if (values.Count == 1)
                    {
                        if (monitoredObjectIdentifier == null)
                        {
                            Log.Info("monitoredObjectIdentifier is null.");
                            continue;
                        }

                        string[] updatedObject = monitoredObjectIdentifier.ToString().Split(':');
                        if (updatedObject.Length < 2 || string.IsNullOrEmpty(updatedObject[1]))
                        {
                            Log.Info("Invalid updatedObject identifier.");
                            continue;
                        }

                        string objectName = "Objectidentifier";

                        if (_bacnetPropertiesMap == null)
                        {
                            Log.Info("_bacnetPropertiesMap is null.");
                            continue;
                        }

                        var kvp = _bacnetPropertiesMap
                            .FirstOrDefault(x => x.Value.ToString() == bacnetPropertyValue.ToString());

                        if (kvp.Equals(default(KeyValuePair<string, BacnetValue>)))
                        {
                            Log.Info($"Property name not found for bacnetPropertyValue: {bacnetPropertyValue}");
                            continue;
                        }

                        string propertyName = kvp.Key;

                        if (_oldValues == null)
                        {
                            Log.Info("_oldValues is null.");
                            continue;
                        }

                        var item = _oldValues.FirstOrDefault(x =>
                            x.ContainsKey(objectName) && x[objectName].ToString() == updatedObject[1]);

                        if (item == null)
                        {
                            Log.Info($"Item not found for object identifier: {updatedObject[1]}");
                            continue;
                        }

                        int indexOfItem = _oldValues.IndexOf(item);
                        if (indexOfItem == -1)
                        {
                            Log.Info($"Item not found in _oldValues for object identifier: {updatedObject[1]}");
                            continue;
                        }

                        if (!item.ContainsKey(propertyName))
                        {
                            Log.Info($"Property '{propertyName}' not found in item dictionary.");
                            continue;
                        }

                        if (bacnetPropertyValue.value == null || bacnetPropertyValue.value.Count == 0)
                        {
                            Log.Info($"BacnetPropertyValue.value is null or empty for property {propertyName}");
                            continue;
                        }

                        item[propertyName] = bacnetPropertyValue.value[0].Value;
                        Data?.Push(_listName).Update(indexOfItem, item);
                    }
                }

                if (needConfirm)
                {
                    sender.SimpleAckResponse(address, BacnetConfirmedServices.SERVICE_CONFIRMED_COV_NOTIFICATION,
                        invokeId);
                }
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
                throw;
            }
        }

        private void ReSubscribe(BacnetAddress address, string objectName, int instance, uint duration, BacnetClient client)
        {
            if (!_bacnetObjectsMap.TryGetValue(objectName, out var type))
                Log.Error("Invalid object name");
            var obj = new BacnetObjectId(type, (uint)instance);
            _subscriptionIdCounter++;
            bool subscribed = client.SubscribeCOVRequest(address, obj, _subscriptionIdCounter, false, true, _subsriptionDuraion);
            if (subscribed)
                Log.Info("Re-subscription to the object " + obj + " was successful.");
            else
                Log.Info("Re-subscription to the object " + obj + " failed.");
        }
        private CustomListObjectElement GetSingleItem(BacnetClient client, BacnetAddress address, string objectName, string instance)
        {
            var properties = GetSupportedPropertiesWithValues(client, address, objectName, instance);
            CustomListObjectElement item = new CustomListObjectElement();
            List<string> added = new List<string>();

            foreach (var property in properties)
            {
                if (!added.Contains(property.Key))
                {
                    string oldKey = property.Key;
                    string newKey = _bacnetPropertiesMap.FirstOrDefault(p => p.Value.ToString() == oldKey).Key;

                    if (newKey != null)
                    {
                        var splitted = property.Value.Split(':');
                        if (splitted.Length > 1)
                        {
                            item.Add(newKey, splitted[1]);
                            added.Add(property.Key);
                        }
                        else
                        {
                            item.Add(newKey, property.Value);
                            added.Add(property.Key);
                        }
                    }
                    else
                    {
                        item.Add(property.Key, property.Value);
                        added.Add(property.Key);
                    }
                }
            }

            return item;
        }

        public CustomListObjectElementCollection GetMultipleItems(BacnetClient client, BacnetAddress address,
            string objectName, int[] instances)
        {
            try
            {
                if (!_bacnetObjectsMap.TryGetValue(objectName, out var type))
                    throw new KeyNotFoundException($"BACnet object '{objectName}' not found.");
                var requstAccess = new List<BacnetReadAccessSpecification>();
                var requestedPropertys = new BacnetPropertyReference[]
                {
                    new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_ALL,
                        System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL)
                };
                foreach (var instance in instances)
                {
                    var objectId = new BacnetObjectId(type, (uint)instance);
                    requstAccess.Add(new BacnetReadAccessSpecification(objectId, requestedPropertys));
                }

                List<string> added = new List<string>();
                var item = new CustomListObjectElement();
                var res = new CustomListObjectElementCollection();
                if (client.ReadPropertyMultipleRequest(address, requstAccess.ToArray(), out var values))
                {
                    foreach (var bacnetReadAccessResult in values)
                    {
                        item = new CustomListObjectElement();
                        added = new List<string>();
                        foreach (var bacnetPropertyValue in bacnetReadAccessResult.values)
                        {
                            string name =
                                ((BacnetPropertyIds)bacnetPropertyValue.property.propertyIdentifier).ToString();
                            string value = GetPropertyValueAsString(bacnetPropertyValue);
                            if (!added.Contains(name))
                            {
                                string newKey = _bacnetPropertiesMap.FirstOrDefault(p => p.Value.ToString() == name)
                                    .Key;
                                if (newKey != null)
                                {
                                    var splitted = value.Split(':');
                                    if (splitted.Length > 1)
                                    {
                                        item.Add(newKey, splitted[1]);
                                        added.Add(name);
                                    }
                                    else
                                    {
                                        item.Add(newKey, value);
                                        added.Add(name);
                                    }
                                }
                                else
                                {
                                    item.Add(name, value);
                                    added.Add(name);
                                }
                            }
                        }

                        res.Add(item);
                    }
                }

                return res;
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
                throw;
            }
        }

        protected override CustomListExecuteReturnContext ExecuteFunctionOverride(CustomListData data, CustomListExecuteParameterContext context)
        {
            int tcpPort = int.Parse(data.Properties["Port"]);
            BacnetAddress adddress = new BacnetAddress(BacnetAddressTypes.IP, data.Properties["Address"]);
            string objectName = data.Properties["ObjectName"];
            string objectInstance = data.Properties["ObjectInstance"];
            var transport = new BacnetIpUdpProtocolTransport(tcpPort);

            BacnetClient client = new BacnetClient(transport);
            client.Start();

            string functionName = context.FunctionName;
            switch (functionName)
            {
                case "WriteData":
                    string propertyName = context.Values[0].StringValue;
                    string instance = context.Values[1].StringValue;
                    string value = context.Values[2].StringValue;
                    WriteProperty(client, adddress, objectName, instance, propertyName, value);
                    break;
                default:
                    break;
            }

            client.Dispose();

            return new CustomListExecuteReturnContext
                {
                    new CustomListObjectElement
                    {
                        { "Result", "null" }
                    }
                };
        }

        private Dictionary<string, string> GetSupportedPropertiesWithValues(BacnetClient client, BacnetAddress address, string objectName, string instance)
        {
            var properties = new Dictionary<string, string>();
            if (!_bacnetObjectsMap.TryGetValue(objectName, out var type))
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

        public Dictionary<BacnetPropertyIds, CustomListColumnTypes> GetPropetiesWithTypes(BacnetClient client, BacnetAddress address, string objectName, string instance)
        {
            var properties = new Dictionary<BacnetPropertyIds, CustomListColumnTypes>();

            if (!_bacnetObjectsMap.TryGetValue(objectName, out var type))
            {
                throw new ArgumentException($"Error: Object '{objectName}' not found in BACnet map.");
            }

            var objectId = default(BacnetObjectId);

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

        public void WriteProperty(BacnetClient client, BacnetAddress address, string objectName, string instanceId, string propertyName, string value)
        {
            try
            {
                if (!_bacnetObjectsMap.TryGetValue(objectName, out BacnetObjectTypes bacnetObject))
                {
                    Log.Error($"Error: Object {objectName} not found.");
                    return;
                }

                if (!uint.TryParse(instanceId, out uint instance))
                {
                    Log.Error("Error: Instance ID must be a number.");
                    return;
                }

                if (!_bacnetPropertiesMap.TryGetValue(propertyName, out BacnetPropertyIds propertyId))
                {
                    Log.Error($"Error: Property {propertyName} not found.");
                    return;
                }

                var (tag, isArray) = IsPropertyValueArray(client, address, objectName, instanceId, propertyName);
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

        private (BacnetApplicationTags Tag, bool IsArray) IsPropertyValueArray(BacnetClient client, BacnetAddress address, string objectName,
            string instance, string propertyName)
        {
            if (!_bacnetObjectsMap.TryGetValue(objectName, out var type))
            {
                Log.Error($"Error: Object {objectName} not found.");
                return (BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL, false);
            }

            if (!uint.TryParse(instance, out uint objectInstance))
            {
                Log.Error("Error: Instance ID must be a number.");
                return (BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL, false);
            }

            if (!_bacnetPropertiesMap.TryGetValue(propertyName, out BacnetPropertyIds bacProp))
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
                                    Log.Error($"Error: Property {propertyName} has no _oldValues.");
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
                    if (float.TryParse(userInput, NumberStyles.Float, CultureInfo.InvariantCulture,
                            out float floatValue))
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