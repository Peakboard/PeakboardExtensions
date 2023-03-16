using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using Peakboard.ExtensionKit;

namespace PeakboardExtensionGraph
{
    [Serializable]
    public class MsGraphCustomList : CustomListBase
    {
        private bool _initialized = false;
        private string _path = @"C:\Users\YannisHartmann\Documents\queries.json";
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = $"MsGraphCustomList",
                Name = "MsGraph List",
                Description = "Returns data from MySql database",
                PropertyInputPossible = true,
                PropertyInputDefaults = {
                    new CustomListPropertyDefinition() { Name = "ClientID", Value = "" },
                    new CustomListPropertyDefinition() { Name = "TenantID", Value = "" },
                    new CustomListPropertyDefinition() { Name = "Data", Value = "contacts" },
                    new CustomListPropertyDefinition() { Name = "RefreshToken", Value = ""},
                    new CustomListPropertyDefinition() { Name = "Path", Value = @"C:\Users\YannisHartmann\Documents\auth.txt" },
                    new CustomListPropertyDefinition() { Name = "QueryPath", Value = @"C:\Users\YannisHartmann\Documents\queries.json"}
                },
            };
        }
        
        protected override FrameworkElement GetControlOverride()
        {
            // return an instance of the UI user control
            return new GraphUIControl();
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            if (!_initialized)
            { 
                InitializeGraph(data);
            }
            
            data.Properties.TryGetValue("Data", StringComparison.OrdinalIgnoreCase, out var type);
            var task = GraphHelper.MakeGraphCall(type);
            task.Wait();
            var response = task.Result;

            var cols = new CustomListColumnCollection();
            
            JsonTextReader reader = new JsonTextReader(new StringReader(response));
            bool start = false;
            string lastValue = "";
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.PropertyName) lastValue = reader.Value.ToString();
                else if (reader.TokenType == JsonToken.StartArray && lastValue == "value") start = true;
                else if (start && reader.TokenType == JsonToken.StartObject)
                {
                    ColumnsWalkThroughObject(reader, "root", cols);
                    break;
                }
            }

            if (!start)
            {
                reader = new JsonTextReader(new StringReader(response));
                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.StartObject)
                    {
                        ColumnsWalkThroughObject(reader, "root", cols);
                        break;
                    }
                }
            }

            return cols;
        }
        

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            if (!_initialized)
            { 
                InitializeGraph(data);
            }

            data.Properties.TryGetValue("Data", StringComparison.OrdinalIgnoreCase, out var type);
            var task = GraphHelper.MakeGraphCall(type);
            task.Wait();
            var response = task.Result;

            var items = new CustomListObjectElementCollection();

            JsonTextReader reader = new JsonTextReader(new StringReader(response));
            bool start = false;
            string lastValue = "";
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.PropertyName) lastValue = reader.Value.ToString();
                else if (reader.TokenType == JsonToken.StartArray && lastValue == "value") start = true;
                else if (start && reader.TokenType == JsonToken.StartObject)
                {
                    var item = new CustomListObjectElement();
                    ItemsWalkThroughObject(reader, "root", item);
                    items.Add(item);
                }
            }
            
            if (!start)
            {
                reader = new JsonTextReader(new StringReader(response));
                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.StartObject)
                    {
                        var item = new CustomListObjectElement();
                        ItemsWalkThroughObject(reader, "root", item);
                        items.Add(item);
                    }
                }
            }

            return items;
        }

        private void InitializeGraph(CustomListData data)
        {
            string refreshToken = data.Parameter.Split(';')[6];
            data.Properties.TryGetValue("Path", StringComparison.OrdinalIgnoreCase, out var path);
            data.Properties.TryGetValue("QueryPath", StringComparison.OrdinalIgnoreCase, out var queries);
            
            // check if refresh token is available
            if (string.IsNullOrEmpty(refreshToken))
            {
                // if not (in designer) initialize by authentication
                var task = GraphHelper.InitGraph(queries,(code, url) =>
                {
                    StreamWriter writer = new StreamWriter(path);
                    writer.WriteLine(code);
                    writer.Close();
                    Process.Start(url);
                    return Task.FromResult(0);
                });
                task.Wait();

                StreamWriter writer1 = new StreamWriter(path);
                writer1.WriteLine(GraphHelper.GetRefreshToken());
                writer1.Close();
            }
            else
            {
                // if available initialize by refresh token (in runtime)
                var task = GraphHelper.InitGraphInRuntime(refreshToken, _path);
                task.Wait();
                
            }
            _initialized = true;
        }
        
        private void ColumnsWalkThroughObject(JsonReader reader, string objPrefix, CustomListColumnCollection cols)
        {
            var lastName = "";
            var value = false;
        
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    lastName = reader.Value.ToString();
                    value = true;
                }
                else if (reader.TokenType == JsonToken.StartObject)
                {
                    value = false;
                    ColumnsWalkThroughObject(reader, $"{objPrefix}-{lastName}", cols);
                }
                else if (reader.TokenType == JsonToken.StartArray)
                {
                    cols.Add(new CustomListColumn($"{objPrefix}-{lastName}-Array", CustomListColumnTypes.String));
                    SkipArray(reader);
                    value = false;
                }
                else if (reader.TokenType == JsonToken.EndObject)
                {
                    return;
                }
                else if(value)
                {
                    Console.WriteLine($"{reader.TokenType} {objPrefix}-{lastName} = {reader.Value ?? "null"}"); 
                    CustomListColumn newCol;
                    if (reader.TokenType == JsonToken.Boolean)
                    {
                        newCol = new CustomListColumn($"{objPrefix}-{lastName}", CustomListColumnTypes.Boolean);
                    }
                    else if (reader.TokenType == JsonToken.Integer || reader.TokenType == JsonToken.Float)
                    {
                        newCol = new CustomListColumn($"{objPrefix}-{lastName}", CustomListColumnTypes.Number);
                    }
                    else
                    {
                        newCol = new CustomListColumn($"{objPrefix}-{lastName}", CustomListColumnTypes.String);
                    }
                    cols.Add(newCol);
                    value = false;
                }
            }
        }
        
        private void ItemsWalkThroughObject(JsonReader reader, string objPrefix, CustomListObjectElement item)
        {
            var lastName = "";
            var value = false;
        
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    lastName = reader.Value.ToString();
                    value = true;
                }
                else if (reader.TokenType == JsonToken.StartObject)
                {
                    value = false;
                    ItemsWalkThroughObject(reader, $"{objPrefix}-{lastName}", item);
                }
                else if (reader.TokenType == JsonToken.StartArray)
                {
                    var arr = WalkThroughArray(reader, $"{objPrefix}-{lastName}-Array", item);
                    item.Add($"{objPrefix}-{lastName}-Array", $"'{lastName}': [ {arr} ]");
                }
                else if (reader.TokenType == JsonToken.EndObject)
                {
                    return;
                }
                else if(value)
                {
                    if (reader.TokenType == JsonToken.Boolean || reader.TokenType == JsonToken.Float ||
                        reader.TokenType == JsonToken.Integer)
                    {
                           item.Add($"{objPrefix}-{lastName}", reader.Value);
                    }
                    else
                    {
                        string objectValue = reader.Value?.ToString();
                        if (objectValue?.Length - 1024 > 0)
                        { 
                            objectValue.Remove(1024);
                        }
                        item.Add($"{objPrefix}-{lastName}", objectValue);
                    }
                }
            }
        }
        
        private string WalkThroughArray(JsonReader reader, string objPrefix, CustomListObjectElement item)
        {
            bool value = false;
            string lastname = "";
            string arr = "";
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    value = true;
                    lastname = reader.Value.ToString();
                }
                else if (reader.Value != null && value)
                {
                    arr += $"'{lastname}': '{reader.Value}', ";
                }
                else if (reader.TokenType == JsonToken.StartArray)
                {
                    WalkThroughArray(reader, objPrefix, item);
                }
                else if (reader.TokenType == JsonToken.EndArray)
                {
                    if (arr.Length > 2)
                    {
                        arr.Remove(arr.Length - 2);
                    }
                    return arr;
                }
            }

            return arr;
        }

        private void SkipArray(JsonReader reader)
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.StartArray)
                {
                    SkipArray(reader);
                }
                else if (reader.TokenType == JsonToken.EndArray)
                {
                    return;
                }
            }
        }

        
    }
}