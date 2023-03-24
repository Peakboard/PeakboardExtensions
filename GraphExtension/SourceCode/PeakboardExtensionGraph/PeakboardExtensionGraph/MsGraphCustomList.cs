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
                ID = $"MsGraphUserAuthCustomList",
                Name = "Microsoft Graph UserAuth List",
                Description = "Returns data from MS-Graph API",
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
            
            try { top = Int32.Parse(topString); } catch (Exception) { /*ignored*/ }
            try { skip = Int32.Parse(skipString); } catch (Exception) { /*ignored*/ }

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
                task = GraphHelper.MakeGraphCall(customCall, new RequestParameters()
                {
                    ConsistencyLevelEventual = eventual
                });
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
                    JsonHelper.ColumnsWalkThroughObject(reader, "root", cols);
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
            
            try { top = Int32.Parse(topString); } catch (Exception) { /*ignored*/ }
            try { skip = Int32.Parse(skipString); } catch (Exception) { /*ignored*/ }
            
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
                task = GraphHelper.MakeGraphCall(customCall, new RequestParameters()
                {
                    ConsistencyLevelEventual = eventual
                });
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
                    JsonHelper.ItemsWalkThroughObject(reader, "root", item, jObject);
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