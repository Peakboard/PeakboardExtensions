using Peakboard.ExtensionKit;
using System;
using System.Collections.Generic;
using System.IO.BACnet;
using System.Linq;
using System.Timers;
using BacNetExtension.CustomLists.Helpers;
using BacNetExtension.CustomLists.Services;

namespace BacNetExtension.CustomLists
{
    [Serializable]
    [CustomListIcon("BacNetExtension.pb_datasource_bacnet.png")]
    public class BacNetPropertiesCustomList : CustomListBase
    {
        private readonly Dictionary<string, BacnetPropertyIds> _bacnetPropertiesMap;
        private readonly Dictionary<string, BacnetObjectTypes> _bacnetObjectsMap;
        private string _listName;
        private uint _subscriptionIdCounter;
        private CustomListObjectElementCollection _oldValues;
        private Dictionary<uint, Timer> _covTimers = new Dictionary<uint, Timer>();
        private BacNetPropertyReader _propertyReader;
        private BacNetPropertyWriter _propertyWriter;
        private BacNetSubscriptionManager _subscriptionManager;
        private readonly Action<string,string> _logger;
        public BacNetPropertiesCustomList()
        {
            _bacnetPropertiesMap = Enum.GetValues(typeof(BacnetPropertyIds))
                .Cast<BacnetPropertyIds>()
                .Where(e => e.ToString().StartsWith("PROP_"))
                .ToDictionary(
                    e => e.ToString().Replace("PROP_", "").ToPascalCase(),
                    e => e,
                    StringComparer.OrdinalIgnoreCase
                );

            _bacnetObjectsMap = Enum.GetValues(typeof(BacnetObjectTypes))
                .Cast<BacnetObjectTypes>()
                .Where(e => e.ToString().StartsWith("OBJECT_"))
                .ToDictionary(
                    e => e.ToString().Replace("OBJECT_", "").ToPascalCase(),
                    e => e,
                    StringComparer.OrdinalIgnoreCase
                );
            _logger = Logger;

            
        }

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
                    new CustomListFunctionDefinition
                    {
                        Name = "WriteData",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "BACnetProperty",
                                Description = "Enter BACnet property (e.g., Present Value): ",
                                Type = CustomListFunctionParameterTypes.String,
                            },
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "InstanceOffObject",
                                Description = "Enter BACnet property (e.g., Present Value): ",
                                Type = CustomListFunctionParameterTypes.String,
                            },
                            new CustomListFunctionInputParameterDefinition
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
                    new CustomListPropertyDefinition { Name = "InstanceNumber", Value = "" },
                    new CustomListPropertyDefinition { Name = "SubscribeCOV", Value = "True" }
                },
            };
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            try
            {
                var client = BacNetHelper.CreateBacNetClient(data);
                _propertyReader = new BacNetPropertyReader(client, _bacnetObjectsMap,_logger);

                var address = new BacnetAddress(BacnetAddressTypes.IP, data.Properties["Address"]);
                var objectName = data.Properties["Type"];
                var objectInstance = data.Properties["InstanceNumber"];

                var properties = _propertyReader.GetPropetiesWithTypes(address, objectName, objectInstance);
                var columns = new CustomListColumnCollection();

                foreach (var property in properties)
                {
                    var oldPropertyName = property.Key.ToString();
                    var columnName = _bacnetPropertiesMap.FirstOrDefault(p => p.Value.ToString() == oldPropertyName).Key;
                    columns.Add(new CustomListColumn(columnName ?? oldPropertyName, property.Value));
                }

                if (!columns.Any())
                {
                    throw new Exception("No properties found");
                }

                client.Dispose();
                return columns;
            }
            catch (Exception ex)
            {
                Log.Error("Error getting columns", ex);
                throw new Exception("Failed to get columns", ex);
            }
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            try
            {
                var client = BacNetHelper.CreateBacNetClient(data);
                _propertyReader = new BacNetPropertyReader(client, _bacnetObjectsMap,_logger);

                var address = new BacnetAddress(BacnetAddressTypes.IP, data.Properties["Address"]);
                var objectName = data.Properties["Type"];
                var objectInstance = data.Properties["InstanceNumber"];

                var items = new CustomListObjectElementCollection();

                if (objectInstance.Contains('-'))
                {
                    var instances = objectInstance.Split('-')
                        .Select(int.Parse)
                        .ToArray();
                    var instancesArray = Enumerable.Range(instances[0], instances[1] - instances[0] + 1).ToArray();

                    foreach (var instance in instancesArray)
                    {
                        var properties = _propertyReader.GetSupportedPropertiesWithValues(address, objectName, instance.ToString());
                        items.Add(CreateItemElement(properties));
                    }
                }
                else
                {
                    foreach (var instance in objectInstance.Split(';'))
                    {
                        var properties = _propertyReader.GetSupportedPropertiesWithValues(address, objectName, instance);
                        items.Add(CreateItemElement(properties));
                    }
                }

                client.Dispose();
                _oldValues = items;
                return items;
            }
            catch (Exception ex)
            {
                Log.Error("Error getting items", ex);
                throw new Exception("Failed to get items", ex);
            }
        }

        protected override void SetupOverride(CustomListData data)
        {
            try
            {
                AppDomain.CurrentDomain.ProcessExit += CurrentDomainOnProcessExit;
                _listName = data.ListName;
                Log.Info($"Setting up list: {_listName}");

                if (!bool.TryParse(data.Properties["SubscribeCOV"], out bool subscribeCov))
                {
                    throw new Exception("Invalid SubscribeCOV value");
                }

                if (!subscribeCov) return;

                var client = BacNetHelper.CreateBacNetClient(data);
                _subscriptionManager = new BacNetSubscriptionManager(client, _bacnetObjectsMap,_logger);

                var address = new BacnetAddress(BacnetAddressTypes.IP, data.Properties["Address"]);
                var objectName = data.Properties["Type"];
                var objectInstance = data.Properties["InstanceNumber"];

                var instances = objectInstance.Contains('-')
                    ? GetInstancesFromRange(objectInstance)
                    : objectInstance.Split(';');

                _subscriptionManager.SubscribeToObjects(address, objectName, instances);
                client.OnCOVNotification += HandleCovNotification;
                
            }
            catch (Exception ex)
            {
                Log.Error("Error in setup", ex);
                throw new Exception("Failed to setup", ex);
            }
        }

        private void CurrentDomainOnProcessExit(object sender, EventArgs e)
        {
            _subscriptionManager.Dispose();
        }

        protected override CustomListExecuteReturnContext ExecuteFunctionOverride(CustomListData data, CustomListExecuteParameterContext context)
        {
            try
            {
                var client = BacNetHelper.CreateBacNetClient(data);
                _propertyWriter = new BacNetPropertyWriter(client, _bacnetPropertiesMap, _bacnetObjectsMap,_logger);

                var address = new BacnetAddress(BacnetAddressTypes.IP, data.Properties["Address"]);
                var objectName = data.Properties["Type"];

                if (context.FunctionName == "WriteData")
                {
                    var propertyName = context.Values[0].StringValue;
                    var instance = context.Values[1].StringValue;
                    var value = context.Values[2].StringValue;

                    _propertyWriter.WriteProperty(address, objectName, instance, propertyName, value);
                }

                client.Dispose();
                return new CustomListExecuteReturnContext { new CustomListObjectElement { { "Result", "Success" } } };
            }
            catch (Exception ex)
            {
                Log.Error("Error executing function", ex);
                return new CustomListExecuteReturnContext { new CustomListObjectElement { { "Result", $"Error: {ex.Message}" } } };
            }
        }

        private void HandleCovNotification(BacnetClient sender, BacnetAddress address, byte invokeId, uint subscriberProcessIdentifier, 
            BacnetObjectId initiatingDeviceIdentifier, BacnetObjectId monitoredObjectIdentifier, uint timeRemaining, 
            bool needConfirm, ICollection<BacnetPropertyValue> values, BacnetMaxSegments maxSegments)
        {
            try
            {
                if (values == null || !values.Any())
                {
                    Log.Warning("No values in COV notification");
                    return;
                }
            
                foreach (var propertyValue in values)
                {
                    ProcessPropertyValue(propertyValue, monitoredObjectIdentifier);
                }
            
                if (needConfirm)
                {
                    sender.SimpleAckResponse(address, BacnetConfirmedServices.SERVICE_CONFIRMED_COV_NOTIFICATION, invokeId);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Error handling COV notification", ex);
            }
            
        }

        private void ProcessPropertyValue(BacnetPropertyValue propertyValue, BacnetObjectId monitoredObjectIdentifier)
        {
            var updatedObject = monitoredObjectIdentifier.ToString().Split(':');
           
            if (updatedObject.Length < 2)
            {
                Log.Warning("Invalid object identifier format");
                return;
            }

            var propertyName = GetPropertyName(propertyValue);
            if (string.IsNullOrEmpty(propertyName))
            {
                return;
            }

            var item = FindItemByInstance(updatedObject[1]);
            if (item == null)
            {
                Log.Warning("Invalid object identifier format");
                return;
            }
            Log.Info($"Updating property {propertyName} to {propertyValue.value[0].Value}");
            UpdateItemValue(item, propertyName, propertyValue);
        }

        private string GetPropertyName(BacnetPropertyValue propertyValue)
        {
            var kvp = _bacnetPropertiesMap.FirstOrDefault(x => x.Value.ToString() == propertyValue.ToString());
            return kvp.Equals(default(KeyValuePair<string, BacnetPropertyIds>)) ? null : kvp.Key;
        }

        private CustomListObjectElement FindItemByInstance(string instance)
        {
            return _oldValues?.FirstOrDefault(x => x.ContainsKey("ObjectIdentifier") && x["ObjectIdentifier"].ToString() == instance);
        }

        private void UpdateItemValue(CustomListObjectElement item, string propertyName, BacnetPropertyValue propertyValue)
        {
            if (!item.ContainsKey(propertyName))
            {
                Log.Warning($"Property '{propertyName}' not found in item");
                return;
            }

            if (propertyValue.value == null || !propertyValue.value.Any())
            {
                Log.Warning($"No values for property {propertyName}");
                return;
            }

            item[propertyName] = propertyValue.value[0].Value;
            var index = _oldValues.IndexOf(item);
            if (index != -1)
            {
                Data?.Push(_listName).Update(index, item);
            }
        }

       

        private string[] GetInstancesFromRange(string range)
        {
            var instances = range.Split('-')
                .Select(int.Parse)
                .ToArray();
            return Enumerable.Range(instances[0], instances[1] - instances[0] + 1)
                .Select(i => i.ToString())
                .ToArray();
        }

        private CustomListObjectElement CreateItemElement(Dictionary<string, string> properties)
        {
            var item = new CustomListObjectElement();
            foreach (var property in properties)
            {
                var newKey = _bacnetPropertiesMap.FirstOrDefault(p => p.Value.ToString() == property.Key).Key;
                if (newKey != null)
                {
                    var value = property.Value.Split(':');
                    item.Add(newKey, value.Length > 1 ? value[1] : property.Value);
                }
                else
                {
                    item.Add(property.Key, property.Value);
                }
            }
            return item;
        }
        private void Logger(string logType, string message)
        {
            switch (logType)
            {
                case "info":
                    Log.Info(message);
                    break;
                case "warning":
                    Log.Warning(message);
                    break;
                case "error":
                    Log.Error(message);
                    break;
            }
        }
    }
}