using System;
using System.IO;
using System.Threading.Tasks;
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
            return new GraphUiControl();
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
            string type = data.Parameter.Split(';')[3];         // request type
            string select = data.Parameter.Split(';')[4];       // select   
            string orderBy = data.Parameter.Split(';')[5];      // order by
            string filter = data.Parameter.Split(';')[6];       // filter
            bool eventual = data.Parameter.Split(';')[7] == "true";
            string topString = data.Parameter.Split(';')[8];    // top
            string skipString = data.Parameter.Split(';')[9];   // skip
            string customCall = data.Parameter.Split(';')[12];  // custom call
            
            int top = 0;
            int skip = 0;
            
            try
            {
                top = Int32.Parse(topString);
            }
            catch (Exception)
            {
                // ignored
            }
            
            try
            {
                skip = Int32.Parse(skipString);

            }
            catch (Exception)
            {
                // ignored
            }

            // make graph call
            Task<string> task;
            if(customCall == "")
            {
                task = GraphHelper.MakeGraphCall(type, new RequestParameters()
                {
                    OrderBy = orderBy,
                    Select = select,
                    Top = top,
                    Skip = skip,
                    Filter = filter,
                    ConsistencyLevelEventual = eventual
                });
            }
            else
            {
                task = GraphHelper.MakeGraphCall(customCall);
            }
            task.Wait();
            var response = task.Result;

            var cols = new CustomListColumnCollection();
            
            // parse json to PB Columns
            JsonTextReader reader = PreparedReader(response);

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.StartObject)
                {
                    ColumnsWalkThroughObject(reader, "root", cols);
                    break;
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
            string type = data.Parameter.Split(';')[3];         // request type
            string select = data.Parameter.Split(';')[4];       // select   
            string orderBy = data.Parameter.Split(';')[5];      // order by
            string filter = data.Parameter.Split(';')[6];       // filter
            bool eventual = data.Parameter.Split(';')[7] == "true";
            string topString = data.Parameter.Split(';')[8];    // top
            string skipString = data.Parameter.Split(';')[9];   // skip
            string customCall = data.Parameter.Split(';')[12];   // custom call
            
            int top = 0;
            int skip = 0;
            
            try
            {
                top = Int32.Parse(topString);
            }
            catch (Exception)
            {
                // ignored
            }

            try
            {
                skip = Int32.Parse(skipString);

            }
            catch (Exception)
            {
                // ignored
            }
            
            // make graph call
            Task<string> task;
            if(customCall == "")
            {
                task = GraphHelper.MakeGraphCall(type, new RequestParameters()
                {
                    OrderBy = orderBy,
                    Select = select,
                    Top = top,
                    Skip = skip,
                    Filter = filter,
                    ConsistencyLevelEventual = eventual
                });
            }
            else
            {
                task = GraphHelper.MakeGraphCall(customCall);
            }
            task.Wait();
            var response = task.Result;

            var items = new CustomListObjectElementCollection();

            // parse response to PB table
            JsonTextReader reader = PreparedReader(response);
            JObject jObject = JObject.Parse(response);

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.StartObject)
                {
                    var item = new CustomListObjectElement();
                    ItemsWalkThroughObject(reader, "root", item, jObject);
                    items.Add(item);
                }
            }
            
            return items;
        }

        private void InitializeGraph(CustomListData data)
        {
            // get refresh token from parameter
            string refreshToken = data.Parameter.Split(';')[10];

            // check if refresh token is available
            if (string.IsNullOrEmpty(refreshToken))
            {
                // if refresh token isn't available -> user did not authenticate
                throw new NullReferenceException("Refresh token not initialized: User did not authenticate");
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
                    // store property name and set value true
                    // next token is either the value or a nested object/array
                    lastName = (string)reader.Value ?? "";
                    value = true;
                }
                else if (reader.TokenType == JsonToken.StartObject)
                {
                    // nested object starts -> walk through recursively
                    // object Prefix is passed on to ensure unique designation
                    // terminology is root-nestedObject-nestedObjectInNestedObject-...
                    value = false;
                    ColumnsWalkThroughObject(reader, $"{objPrefix}-{lastName}", cols);
                }
                else if (reader.TokenType == JsonToken.StartArray)
                {
                    // nested array starts -> skip array
                    cols.Add(new CustomListColumn($"{objPrefix}-{lastName}-Array", CustomListColumnTypes.String));
                    JsonHelper.SkipArray(reader);
                    value = false;
                }
                else if (reader.TokenType == JsonToken.EndObject)
                {
                    // nested object ends -> return to upper recursion layer
                    return;
                }
                else if(value && !lastName.Contains("odata"))
                {
                    // primitive property -> add CustomListColumn with corresponding type
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
                    // store property name and set value true
                    // next token is either the value or a nested object/array
                    lastName = (string)reader.Value ?? "";
                    value = true;
                }
                else if (reader.TokenType == JsonToken.StartObject)
                {
                    // nested object starts -> walk through recursively
                    value = false;
                    ItemsWalkThroughObject(reader, $"{objPrefix}-{lastName}", item, obj);
                }
                else if (reader.TokenType == JsonToken.StartArray)
                {
                    // nested array starts -> store entire array json into column and skip the array
                    JsonHelper.SkipArray(reader);
                    item.Add($"{objPrefix}-{lastName}-Array", $"{obj.SelectToken(reader.Path)}");
                }
                else if (reader.TokenType == JsonToken.EndObject)
                {
                    // nested object ends -> return to upper recursion layer
                    return;
                }
                else if(value && !lastName.Contains("odata"))
                {
                    // primitive property -> store the value in corresponding column
                    if (reader.TokenType == JsonToken.Boolean || reader.TokenType == JsonToken.Float ||
                        reader.TokenType == JsonToken.Integer)
                    {
                           item.Add($"{objPrefix}-{lastName}", reader.Value);
                    }
                    else
                    {
                        string objectValue = reader.Value?.ToString();
                        // if value is bigger than 1024 chars -> cut at 1024
                        if (objectValue?.Length - 1024 > 0)
                        { 
                            objectValue = objectValue.Remove(1024);
                        }
                        item.Add($"{objPrefix}-{lastName}", objectValue);
                    }
                }
            }
        }

        private JsonTextReader PreparedReader(string response)
        {
            // prepare reader for recursive walk trough
            var reader = new JsonTextReader(new StringReader(response));
            bool prepared = false;

            while (reader.Read() && !prepared)
            {
                if (reader.TokenType == JsonToken.PropertyName && reader.Value?.ToString() == "value")
                {
                    // if json contains value array -> collection response with several objects
                    // parsing starts after the array starts
                    prepared = true;
                }
                else if (reader.TokenType == JsonToken.PropertyName && reader.Value?.ToString() == "error")
                {
                    // if json contains an error field -> deserialize to Error Object & throw exception
                    GraphHelper.DeserializeError(response);
                }
            }
            if(!prepared)
            {
                // no value array -> response contains single object which starts immediately
                reader = new JsonTextReader(new StringReader(response));
            }

            return reader;
        }
        
        public void UpdateRefreshToken(string token, CustomListData data)
        {
            // replace refresh token in parameter if renewed
            var values = data.Parameter.Split(';');
            values[10] = token;
            string result = values[0];
            
            for(int i = 1; i < values.Length; i++)
            {
                result += $";{values[i]}";
            }

            data.Parameter = result;
        }

        
    }
}