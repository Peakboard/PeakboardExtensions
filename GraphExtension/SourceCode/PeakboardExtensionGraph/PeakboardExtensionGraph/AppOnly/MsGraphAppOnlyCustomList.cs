using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Peakboard.ExtensionKit;
using PeakboardExtensionGraph.Settings;

namespace PeakboardExtensionGraph.AppOnly
{
    [CustomListIcon("PeakboardExtensionGraph.graph_clean.png")]
    [Serializable]
    public class MsGraphAppOnlyCustomList : CustomListBase
    {

        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = $"MsGraphAppOnlyCustomList",
                Name = "Microsoft Graph App-Only Access",
                Description = "Returns data from MS-Graph API",
                PropertyInputPossible = true,
            };
        }
        
        protected override FrameworkElement GetControlOverride()
        {
            // return an instance of the UI user control
            return new GraphAppOnlyUiControl();
        }

        protected override void CheckDataOverride(CustomListData data)
        {

            if (String.IsNullOrEmpty(data.Parameter))
            {
                throw new InvalidOperationException("Settings for Graph Connection not found");
            }

            AppOnlySettings settings;
            try
            {
                //string json = data.Parameter;
                //settings = JsonConvert.DeserializeObject<AppOnlySettings>(json);
                settings = AppOnlySettings.GetSettingsFromParameterString(data.Parameter);
                if (settings == null) throw new InvalidOperationException("Invalid parameter format");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Error getting settings for graph: {ex.Message}");
            }

            if (settings.Parameters == null && string.IsNullOrEmpty(settings.CustomCall))
            {
                this.Log?.Verbose("No Query Parameters available. Extracting entire objects");
            }
            if (String.IsNullOrEmpty(settings.ClientId))
            {
                throw new InvalidOperationException("Client ID is missing");
            }
            if (String.IsNullOrEmpty(settings.TenantId))
            {
                throw new InvalidOperationException("Tenant ID is missing");
            }
            if (String.IsNullOrEmpty(settings.Secret))
            {
                throw new InvalidOperationException("Secret is missing");
            }
            if (String.IsNullOrEmpty(settings.EndpointUri) && String.IsNullOrEmpty(settings.CustomCall))
            {
                throw new InvalidOperationException("Query is missing");
            }
            

        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            AppOnlySettings settings;
            try
            {
                settings = AppOnlySettings.GetSettingsFromParameterString(data.Parameter);
                if (settings == null) throw new InvalidOperationException("Settings are missing.");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Error getting settings for graph: {ex.Message}");
            }
            
            // Initialize GraphHelper
            var helper = GetGraphHelper(settings);

            // make graph call
            string request = settings.EndpointUri; //data.Parameter.Split(';')[3];
            string customCall = settings.CustomCall; //data.Parameter.Split(';')[10];
            GraphResponse response;

            if (customCall != "") request = customCall;

            try
            {
                
                if (customCall == "")
                {
                    response = helper.ExtractAsync(request, settings.Parameters/*BuildRequestParameters(data)*/).Result;
                }
                else
                {
                    response = helper.ExtractAsync(customCall, settings.RequestBody).Result;
                }
                //task.Wait();
                //response = task.Result;
            }
            catch (AggregateException aex)
            {
                if(aex.InnerException is MsGraphException mex)
                {
                    throw new InvalidOperationException(
                        $"Microsoft Graph Error\n Code: {mex.ErrorCode}\nMessage: {mex.Message}");
                }
                else
                {
                    throw new InvalidOperationException($"Error receiving response from Graph: {aex.InnerException?.Message ?? aex.Message}");
                }
                
            }

            // get columns
            var cols = new CustomListColumnCollection();

            if(response.Type == GraphContentType.Json)
            {
                // parse json to PB Columns
                JsonTextReader reader = PreparedReader(response.Content);

                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.StartObject)
                    {
                        JsonHelper.ColumnsWalkThroughObject(reader, "root", cols);
                        break;
                    }
                }
            }
            else if (response.Type == GraphContentType.OctetStream)
            {
                var reader = new StringReader(response.Content);
                string[] colNames = reader.ReadLine()?.Split(',');

                if (colNames == null)
                {
                    throw new InvalidOperationException("Response is empty");
                }

                foreach (var colName in colNames)
                {
                    cols.Add(new CustomListColumn()
                    {
                        Name = colName,
                        Type = CustomListColumnTypes.String
                    });
                }
            }
            
            return cols;
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            AppOnlySettings settings;
            try{
                settings = AppOnlySettings.GetSettingsFromParameterString(data.Parameter);
                if (settings == null) throw new InvalidOperationException("Settings are missing.");
            }
            catch (JsonException)
            {
                throw new InvalidOperationException("Parameter string in old format. Update it by refreshing the datasource in the designer");
            }
            
            // create an item with empty values
            var expectedKeys = GetColumnsOverride(data);
            var emptyItem = new CustomListObjectElement();
            SetKeys(emptyItem, expectedKeys);
            
            // Initialize GraphHelper
            var helper = GetGraphHelper(settings);

            // make graph call
            string request = settings.EndpointUri; //data.Parameter.Split(';')[3];
            string customCall = settings.CustomCall; //data.Parameter.Split(';')[10];
            GraphResponse response;
            

            if (customCall != "") request = customCall;

            try
            {
                
                if (customCall == "")
                {
                    response = helper.ExtractAsync(request, settings.Parameters/*BuildRequestParameters(data)*/).Result;
                }
                else
                {
                    response = helper.ExtractAsync(customCall, settings.RequestBody).Result;
                }
                //task.Wait();
                //response = task.Result;
            }
            catch (AggregateException aex)
            {
                if(aex.InnerException is MsGraphException mex)
                {
                    throw new InvalidOperationException(
                        $"Microsoft Graph Error\n Code: {mex.ErrorCode}\nMessage: {mex.Message}");
                }
                else
                {
                    throw new InvalidOperationException($"Error receiving response from Graph: {aex.InnerException?.Message ?? aex.Message}");
                }
                
            }

            // get items
            var items = new CustomListObjectElementCollection();

            if(response.Type == GraphContentType.Json){
                // parse json to PB Columns
                JsonTextReader reader = PreparedReader(response.Content);
                JObject jObject = JObject.Parse(response.Content);

                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.StartObject)
                    {
                        var item = CloneItem(emptyItem);
                        JsonHelper.ItemsWalkThroughObject(reader, "root", item, jObject);
                        items.Add(item);
                    }
                }
            }
            else if (response.Type == GraphContentType.OctetStream)
            {
                var reader = new StringReader(response.Content);
                string[] colNames = reader.ReadLine()?.Split(',');

                if (colNames == null)
                {
                    throw new InvalidOperationException("Response is empty");
                }

                string row = reader.ReadLine();
                while(row != null)
                {
                    string[] values = row.Split(',');
                    var item = new CustomListObjectElement();
                    for (int i = 0; i < values.Length; i++)
                    {
                        item.Add(colNames[i], values[i]);
                    }
                    items.Add(item);
                    row = reader.ReadLine();
                }
            }

            return items;
        }

        #region HelperMethods
        
        private GraphHelperAppOnly GetGraphHelper(AppOnlySettings settings)
        {
            //string[] paramArr = data.Parameter.Split(';');
            
            // get a graph helper instance
            //var helper = new GraphHelperAppOnly(paramArr[0], paramArr[1], paramArr[2]);
            var helper = new GraphHelperAppOnly(settings.ClientId, settings.TenantId, settings.Secret);
            var task = helper.InitGraph();
            task.Wait();

            return helper;
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

        /*private RequestParameters BuildRequestParameters(CustomListData data)
        {
            string[] paramArr = data.Parameter.Split(';');

            if (paramArr[10] != "")
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
                10  =>  custom call
            
        }*/

        private void SetKeys(CustomListObjectElement item, CustomListColumnCollection columns)
        {
            foreach (var column in columns)
            {
                var key = column.Name;
                
                switch (column.Type)
                {
                    case CustomListColumnTypes.Boolean:
                        item.Add(key, false); 
                        break;
                    case CustomListColumnTypes.Number:
                        item.Add(key, -1); 
                        break;
                    case CustomListColumnTypes.String:
                        item.Add(key, "");
                        break;
                }
                
            }
        }

        private CustomListObjectElement CloneItem(CustomListObjectElement item)
        {
            var newItem = new CustomListObjectElement();

            foreach (var key in item.Keys)
            {
                newItem.Add(key, item[key]);
            }

            return newItem;
        }
        
        #endregion
    }
}