using System;
using System.IO;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Peakboard.ExtensionKit;
using PeakboardExtensionGraph.UserAuth;

namespace PeakboardExtensionGraph.AppOnly
{
    [Serializable]
    public class MsGraphAppOnlyCustomList : CustomListBase
    {
        private bool _initialized = false;
        private GraphHelperAppOnly _graphHelper;
        
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = $"MsGraphAppOnlyCustomList",
                Name = "Microsoft Graph AppOnly List",
                Description = "Returns data from MS-Graph API",
                PropertyInputPossible = true,
            };
        }
        
        protected override FrameworkElement GetControlOverride()
        {
            // return an instance of the UI user control
            return new GraphAppOnlyUiControl();
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            // check if graph helper is initialized
            if (!_initialized)
            {
                InitializeGraph(data);
            }

            // check if token expired
            var expiredTask =_graphHelper.CheckIfTokenExpiredAsync();
            expiredTask.Wait();
            
            // make graph call
            string request = data.Parameter.Split(';')[3];
            string customCall = data.Parameter.Split(';')[11];

            if (customCall != "") request = customCall;
            
            var task = _graphHelper.MakeGraphCall(request, BuildParameter(data));
            task.Wait();
            string response = task.Result;
            
            // get columns
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
            // check if graph is initialized
            if (!_initialized)
            {
                InitializeGraph(data);
            }
            
            // check if token expired
            var expiredTask =_graphHelper.CheckIfTokenExpiredAsync();
            expiredTask.Wait();
            
            // make graph call
            string request = data.Parameter.Split(';')[3];
            string customCall = data.Parameter.Split(';')[11];

            if (customCall != "") request = customCall;
            
            var task = _graphHelper.MakeGraphCall(request, BuildParameter(data));
            task.Wait();
            string response = task.Result;
            
            // get items
            var items = new CustomListObjectElementCollection();
            
            // parse json to PB Columns
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
            string[] paramArr = data.Parameter.Split(';');
            
            // init connection
            _graphHelper = new GraphHelperAppOnly(paramArr[0], paramArr[1], paramArr[2]);
            var task = _graphHelper.InitGraph();
            task.Wait();
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
                    GraphHelperBase.DeserializeError(response);
                }
            }
            if(!prepared)
            {
                // no value array -> response contains single object which starts immediately
                reader = new JsonTextReader(new StringReader(response));
            }

            return reader;
        }

        private RequestParameters BuildParameter(CustomListData data)
        {
            string[] paramArr = data.Parameter.Split(';');

            if (paramArr[11] != "")
            {
                // custom call -> no request parameter
                return new RequestParameters()
                {
                    ConsistencyLevelEventual = paramArr[7] == "true"
                };
            }
            
            int top, skip;

            // try parse strings to int
            try { top = Int32.Parse(paramArr[8]); } catch (Exception) { top = 0; }
            try { skip = Int32.Parse(paramArr[9]); } catch (Exception) { skip = 0; }

            return new RequestParameters()
            {
                Select = paramArr[4],
                OrderBy = paramArr[5],
                Filter = paramArr[6],
                ConsistencyLevelEventual = paramArr[7] == "true",
                Top = top,
                Skip = skip
            };
            
            /*
                4   =>  select
                5   =>  order by
                6   =>  filter
                7   =>  consistency level (header)(for filter)
                8   =>  top
                9   =>  skip
                10  =>  custom entities (not used here)
                11  =>  custom call
            */
        }
    }
}