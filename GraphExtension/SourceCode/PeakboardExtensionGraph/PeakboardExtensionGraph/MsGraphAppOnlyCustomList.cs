using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Peakboard.ExtensionKit;

namespace PeakboardExtensionGraph
{
    [Serializable]
    public class MsGraphAppOnlyCustomList : CustomListBase
    {
        private bool _initialized = false;
        
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = $"MsGraphAppOnlyCustomList",
                Name = "Microsoft Graph AppOnly List",
                Description = "Returns data from MS-Graph API",
                PropertyInputPossible = true,
                PropertyInputDefaults =
                {
                    new CustomListPropertyDefinition(){Name = "TenantID", Value="b4ff9807-402f-42b8-a89d-428363c55de7"},
                    new CustomListPropertyDefinition(){Name = "ClientID", Value="067207ed-41a4-4402-b97f-b977babe0ec9"},
                    new CustomListPropertyDefinition(){Name = "ClientSecret", Value = "OBy8Q~M0pJQDqXIsV57e_MUKO6x69IRLPgbtIbmC"},
                    new CustomListPropertyDefinition(){Name = "Call", Value = "/users?$select=displayName"}
                }
            };
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            if (!_initialized)
            {
                InitializeGraph(data);
            }

            data.Properties.TryGetValue("Call", StringComparison.OrdinalIgnoreCase, out var request);

            var task = GraphHelperAppOnly.MakeGraphCall(request);
            task.Wait();
            string response = task.Result;
            
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
            if (!_initialized)
            {
                InitializeGraph(data);
            }

            data.Properties.TryGetValue("Call", StringComparison.OrdinalIgnoreCase, out var request);

            var task = GraphHelperAppOnly.MakeGraphCall(request);
            task.Wait();
            string response = task.Result;
            
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
            data.Properties.TryGetValue("ClientID", StringComparison.OrdinalIgnoreCase, out var client);
            data.Properties.TryGetValue("TenantID", StringComparison.OrdinalIgnoreCase, out var tenant);
            data.Properties.TryGetValue("ClientSecret", StringComparison.OrdinalIgnoreCase, out var secret);
            var task =  GraphHelperAppOnly.InitGraph(client, tenant, secret);
            task.Wait();
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
    }
}