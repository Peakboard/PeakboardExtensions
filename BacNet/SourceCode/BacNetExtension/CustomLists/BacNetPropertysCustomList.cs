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
                            //new CustomListFunctionInputParameterDefinition()
                            //{
                            //    Name = "BACnetObject",
                            //    Description = "Enter BACnet object (e.g., Analog Value): ",
                            //    Type = CustomListFunctionParameterTypes.String,
                            //},
                            //new CustomListFunctionInputParameterDefinition()
                            //{
                            //    Name = "instanceID",
                            //    Description = "Enter instance ID (e.g., 10000): ",
                            //    Type = CustomListFunctionParameterTypes.String,
                            //},new CustomListFunctionInputParameterDefinition()
                            //{
                            //    Name = "instanceID",
                            //    Description = "Enter instance ID (e.g., 10000): ",
                            //    Type = CustomListFunctionParameterTypes.String,
                            //},
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
                            itemElement.Add(newKey, FormatValue(value.Value));
                            added.Add(value.Key);
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
                if (ex.Message.Contains("ERROR_CLASS_OBJECT - ERROR_CODE_UNKNOWN_OBJECT"))
                {
                    throw new Exception("The object name or instance ID is incorrect");
                }
                throw new Exception(ex.Message);
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
                    WriteProperty(adddress,objectName,objectInstance,propertyName,value);
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
            try
            {
                Dictionary<string, string> propertiesWithValues = new Dictionary<string, string>();
                objectName = bacnetObjectMap.FirstOrDefault(p => p.Key == objectName).Value.ToString();
                if (Enum.TryParse(objectName, true, out BacnetObjectTypes type))
                {
                    if (uint.TryParse(instance, out uint parsedValue))
                    {
                        var objectId = new BacnetObjectId(type, parsedValue);
                        try
                        {
                            if (client.ReadPropertyRequest(address, objectId, BacnetPropertyIds.PROP_PROPERTY_LIST, out keys))
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
                                            }
                                            else if (propertyValues.Count > 1&& propertyValues != null)
                                            {
                                               List<object> values = new List<object>();
                                                foreach (var propertyValue in propertyValues)
                                                {
                                                    values.Add(propertyValue.Value);
                                                }
                                               value = JsonConvert.SerializeObject(values);
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
                                        Log.Error($" - Error reading the {propertyId}: {ex}");
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
                            Log.Error($" - Error reading the property: {ex}");
                        }
                    }
                }
                return propertiesWithValues;
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

        private Dictionary<BacnetPropertyIds, CustomListColumnTypes> GetPropetiesWithTypes(BacnetAddress address, string objectName, string instance)
        {
            Dictionary<BacnetPropertyIds, CustomListColumnTypes> typesAndValues = new Dictionary<BacnetPropertyIds, CustomListColumnTypes>();

            objectName = bacnetObjectMap.FirstOrDefault(p=>p.Key == objectName).Value.ToString();
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
        public string FormatValue(object value)
        {
            if (value == null)
                return "null"; 

            switch (value)
            {
                case float f:
                    return f.ToString("0.###"); 
                case double d:
                    return d.ToString("0.###");
                default:
                    return value.ToString();
            }
        }
        private void WriteProperty(BacnetAddress address, string objectName, string instanceId, string propertyName, string value)
        {
            try
            {
                if (!bacnetObjectMap.TryGetValue(objectName, out BacnetObjectTypes bacnetObject))
                {
                    Log.Error($"Error: Object not found.");
                    return;
                }
                if (!uint.TryParse(instanceId, out uint instance))
                {
                    Log.Error($"Error: Instance ID must be a number.");
                    return;
                }
                if (!bacnetPropertiesMap.TryGetValue(propertyName, out BacnetPropertyIds propertyId))
                {
                    Log.Error($"Error: Property not found");
                    return;
                }
                BacnetApplicationTags expectedType = GetTagFromDevice(address, bacnetObject, instance, propertyId);
                Log.Info($"Expected format: {expectedType}");
                object convertedValue = ConvertUserInput(value, expectedType);
                var objectId = new BacnetObjectId(bacnetObject, instance);
                BacnetValue[] valueList = new BacnetValue[] { new BacnetValue(expectedType, convertedValue) };
                bool result = client.WritePropertyRequest(address, objectId, propertyId, valueList);
                if (result)
                    Log.Info($"Data successfully written!");

                if (!result)
                {
                    Log.Error("Error writing to BACnet.");
                }
            }
            catch (Exception ex)
            {

                Log.Error($"{ex}");
            }


        }
    
        private BacnetApplicationTags GetTagFromDevice(BacnetAddress adress,BacnetObjectTypes objectTypes,uint instance,BacnetPropertyIds property)
        {
            var objectID = new BacnetObjectId(objectTypes,instance);
            try
            {
                if (client.ReadPropertyRequest(adress,objectID,property, out var propertyValues) && propertyValues != null && propertyValues.Count > 0)
                {
                    return propertyValues[0].Tag;
                }
                else
                {
                    Log.Info($"Error: Could not determine TAG for {property}.");
                    return BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL;
                }
            }
            catch (Exception ex)
            {
                Log.Info($"{ex}");
                return BacnetApplicationTags.BACNET_APPLICATION_TAG_NULL;
            }
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
                    return userInput; // Просто строка, конвертация не нужна

                case BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED:
                    if (int.TryParse(userInput, out int enumValue))
                        return enumValue;
                    break;

                case BacnetApplicationTags.BACNET_APPLICATION_TAG_OBJECT_ID:
                    // Если это массив, разбиваем строку по запятым и парсим
                    string[] elements = userInput.Split(',');
                    return elements.Select(e => e.Trim()).ToArray();

                default:
                    throw new Exception($"Unsupported BACnet type: {expectedType}");
            }

            throw new Exception($"Invalid value for type {expectedType}: {userInput}");
        }
        private static BacnetBitString ParseBitString(string input)
        {
            var bitString = new BacnetBitString();

            string[] bits = input.Split(',').Select(s => s.Trim()).ToArray();

            for (byte i = 0; i < bits.Length; i++)
            {
                bool bitValue = bits[i] == "1" || bits[i].ToLower() == "true";
                bitString.SetBit(i, bitValue);
            }

            return bitString;
        }

        private BacnetObjectId ParseObjectId(string input)
        {
            string[] parts = input.Split(',');
            if (parts.Length == 2 && Enum.TryParse(parts[0].Trim(), true, out BacnetObjectTypes type) && uint.TryParse(parts[1].Trim(), out uint instance))
            {
                return new BacnetObjectId(type, instance);
            }
            throw new Exception($"Invalid BACnet OBJECT_ID format. Expected format: <ObjectType>, <InstanceNumber>");
        }

    }
}
