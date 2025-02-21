using Newtonsoft.Json.Linq;
using Peakboard.ExtensionKit;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.IO.BACnet;
using System.Linq;
using System.Net;
using System.Net.Sockets;

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

        private BacnetClient _client;
        private IList<BacnetValue> _objects = new List<BacnetValue>();

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
            try
            {
                HashSet<string> addedColumns = new HashSet<string>();
                int tcpPort = int.Parse(data.Properties["Port"]);
                BacnetAddress address = new BacnetAddress(BacnetAddressTypes.IP, data.Properties["Address"]);
                uint deviceId = uint.Parse(data.Properties["DeviceId"]);

                BacnetIpUdpProtocolTransport transport = new BacnetIpUdpProtocolTransport(tcpPort);
                _client = new BacnetClient(transport);
                _client.Start();
                RetrieveAvailableObjects(address, deviceId);

                CustomListColumnCollection columnCollection = new CustomListColumnCollection();

                foreach (var item in _objects)
                {
                    string value = item.Value.ToString();
                    string columnName = value.Split(':')[0];

                    if (!addedColumns.Contains(columnName))
                    {
                        var name = _bacnetObjectMap.FirstOrDefault(b => b.Value.ToString() == columnName).Key ?? columnName;
                        columnCollection.Add(new CustomListColumn(name, CustomListColumnTypes.String));
                        addedColumns.Add(columnName);
                    }
                }
                _client.Dispose();
                return columnCollection;
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("System.NullReferenceException"))
                {
                    throw new ArgumentException("Invalid address or port.");
                }

                throw new Exception(ex.ToString());
            }
           
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            try
            {
                int tcpPort = int.Parse(data.Properties["Port"]);
                var address = new BacnetAddress(BacnetAddressTypes.IP, data.Properties["Address"]);
                uint deviceId = uint.Parse(data.Properties["DeviceId"]);

                var transport = new BacnetIpUdpProtocolTransport(tcpPort);
                _client = new BacnetClient(transport);
                _client.Start();
                RetrieveAvailableObjects(address, deviceId);

                var objectElementCollection = new CustomListObjectElementCollection();
                var uniqueColumnNames = _objects.Select(obj => obj.Value.ToString().Split(':')[0]).Distinct().ToList();

                int maxRowCount = _objects.GroupBy(obj => obj.Value.ToString().Split(':')[0]).Max(g => g.Count());
                int columnCount = uniqueColumnNames.Count;

                string[,] itemsMatrix = new string[maxRowCount, columnCount];
                var addedObjectNames = new HashSet<string>();

                for (int i = 0; i < maxRowCount; i++)
                {
                    for (int j = 0; j < columnCount; j++)
                    {
                        string columnName = uniqueColumnNames[j];
                        string value = _objects.Select(x => x.Value.ToString())
                                               .FirstOrDefault(x => !addedObjectNames.Contains(x) && x.Contains(columnName))
                                        ?? columnName;

                        itemsMatrix[i, j] = value;
                        addedObjectNames.Add(value);
                    }
                }

                foreach (var row in Enumerable.Range(0, maxRowCount))
                {
                    var itemElement = new CustomListObjectElement();

                    foreach (var col in Enumerable.Range(0, columnCount))
                    {
                        string value = itemsMatrix[row, col];
                        string[] nameWithValue = value.Split(':');
                       
                        if (nameWithValue.Length > 1)
                        {
                            string column = nameWithValue[0];
                            string instance = nameWithValue[1];
                            string name = _bacnetObjectMap.FirstOrDefault(b => b.Value.ToString() == column).Key ?? column;
                            itemElement.Add(name, instance);
                        }
                        else
                        {
                            string name = _bacnetObjectMap.FirstOrDefault(b => b.Value.ToString() == value).Key ?? value;
                            itemElement.Add(name, "null");
                        }
                    }
                    objectElementCollection.Add(itemElement);
                }
                _client.Dispose();
                return objectElementCollection;
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                _client.Dispose();
                return new CustomListObjectElementCollection();
            }
        }

        private void RetrieveAvailableObjects(BacnetAddress address, uint deviceId)
        {
            var objectId = new BacnetObjectId(BacnetObjectTypes.OBJECT_DEVICE, deviceId);
            var propertyId = BacnetPropertyIds.PROP_OBJECT_LIST;

            try
            {
                if (_client.ReadPropertyRequest(address, objectId, propertyId, out _objects))
                {
                    Log.Info($"Objects count = {_objects.Count}");
                }
                else
                {
                    Log.Info("Failed to read object list.");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Error reading object list: {ex}");
            }
        }
    }
}
