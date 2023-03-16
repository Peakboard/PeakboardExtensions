using System;
using System.IO;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Peakboard.ExtensionKit;

namespace PeakboardExtensionGraph
{
    [Serializable]
    public class MsGraphCustomList : CustomListBase
    {
        private bool _initialized;
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = $"MsGraphCustomList",
                Name = "MsGraph List",
                Description = "Returns data from MySql database",
                PropertyInputPossible = true,
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
            var checkTask = GraphHelper.CheckIfTokenExpiredAsync();
            checkTask.Wait();
            
            // get parameter for graph call
            string type = data.Parameter.Split(';')[3];     // request type
            string select = data.Parameter.Split(';')[4];   // select   
            string orderBy = data.Parameter.Split(';')[5];  // order by
            string topString = data.Parameter.Split(';')[6];// top
            int top = 10;
            
            try
            {
                top = Int32.Parse(topString);
            }
            catch (Exception)
            {
                // ignored
            }


            // make graph call
            var task = GraphHelper.MakeGraphCall(type, new RequestParameters()
            {
                OrderBy = orderBy,
                Select = select,
                Top = top
            });
            task.Wait();
            var response = task.Result;

            var cols = new CustomListColumnCollection();
            
            // parse json to PB Columns
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
            // check if GraphHelper & RequestBuilder are initialized
            if (!_initialized)
            { 
                InitializeGraph(data);
            }
            
            // check if access token expired
            var checkTask = GraphHelper.CheckIfTokenExpiredAsync();
            checkTask.Wait();
            
            // update refresh token in parameter if renewed
            if (checkTask.Result)
            {
                UpdateRefreshToken(GraphHelper.GetRefreshToken(), data);
            }

            // get parameter for graph call
            string type = data.Parameter.Split(';')[3];     // request type
            string select = data.Parameter.Split(';')[4];   // select   
            string orderBy = data.Parameter.Split(';')[5];  // order by
            string topString = data.Parameter.Split(';')[6];// top
            int top = 10;
            
            try
            {
                top = Int32.Parse(topString);
            }
            catch (Exception)
            {
                // ignored
            }
            
            // make graph call
            var task = GraphHelper.MakeGraphCall(type, new RequestParameters()
            {
                OrderBy = orderBy,
                Select = select,
                Top = top
            });
            task.Wait();
            var response = task.Result;

            var items = new CustomListObjectElementCollection();

            // parse response to PB table
            JsonTextReader reader = new JsonTextReader(new StringReader(response));
            JObject jObject = JObject.Parse(response); 
            bool start = false;
            string lastValue = "";
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.PropertyName) lastValue = reader.Value.ToString();
                else if (reader.TokenType == JsonToken.StartArray && lastValue == "value") start = true;
                else if (start && reader.TokenType == JsonToken.StartObject)
                {
                    var item = new CustomListObjectElement();
                    ItemsWalkThroughObject(reader, "root", item, jObject);
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
                        ItemsWalkThroughObject(reader, "root", item, jObject);
                        items.Add(item);
                    }
                }
            }

            return items;
        }

        private void InitializeGraph(CustomListData data)
        {
            // get refresh token from parameter
            string refreshToken = data.Parameter.Split(';')[7];

            // check if refresh token is available
            if (string.IsNullOrEmpty(refreshToken))
            {
                // if refresh token isn't available -> user did not authenticate
                throw new NullReferenceException("Refresh token not initialized");
            }
            else
            {
                // get parameter for azure app
                string clientId = data.Parameter.Split(';')[0];
                string tenantId = data.Parameter.Split(';')[1];
                string permissions = data.Parameter.Split(';')[2];
                
                // if available initialize by refresh token (in runtime)
                var task = GraphHelper.InitGraphWithRefreshToken(refreshToken, clientId, tenantId, permissions);
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
        
        private void ItemsWalkThroughObject(JsonReader reader, string objPrefix, CustomListObjectElement item, JObject obj)
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
                    ItemsWalkThroughObject(reader, $"{objPrefix}-{lastName}", item, obj);
                }
                else if (reader.TokenType == JsonToken.StartArray)
                {
                    //var arr = WalkThroughArray(reader);
                    SkipArray(reader);
                    item.Add($"{objPrefix}-{lastName}-Array", $"{obj.SelectToken(reader.Path)}");
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
        
        private string WalkThroughArray(JsonReader reader)
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
                    arr += $"\"{lastname}\": \"{reader.Value}\", ";
                }
                else if (reader.TokenType == JsonToken.StartArray)
                {
                    WalkThroughArray(reader);
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


        public void UpdateRefreshToken(string token, CustomListData data)
        {
            // replace refresh token in parameter if renewed
            var values = data.Parameter.Split(';');
            values[7] = token;
            string result = values[0];
            
            for(int i = 1; i < values.Length; i++)
            {
                result += $";{values[i]}";
            }

            data.Parameter = result;
        }

        
    }
}