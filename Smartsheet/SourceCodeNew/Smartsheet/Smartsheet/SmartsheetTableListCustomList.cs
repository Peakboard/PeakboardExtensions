using System;
using System.Net.Http;
using System.Collections.Generic;
using Peakboard.ExtensionKit;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Net;
using Newtonsoft.Json;

namespace Smartsheet
{
    [Serializable]
    [CustomListIcon("Smartsheet.Smartsheet.png")]
    
    class SmartsheetTableListCustomList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "SmartsheetTableList",
                Name = "SmartsheetTableList",
                Description = "Fetches data from Smartsheet API",
                PropertyInputPossible = true,
                PropertyInputDefaults = {
                new CustomListPropertyDefinition() { Name = "Token", Value = "XXX" },

                    },
                Functions = new CustomListFunctionDefinitionCollection
                {
                    
                }     
            };
        }

        protected override void CheckDataOverride(CustomListData data)
        {
            if (string.IsNullOrWhiteSpace(data.Properties["Token"]))
            {
                throw new InvalidOperationException("Invalid Token");
            }
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            var columns = new CustomListColumnCollection();
            columns.Add(new CustomListColumn("ID", CustomListColumnTypes.String));
            columns.Add(new CustomListColumn("Name", CustomListColumnTypes.String));
            return columns;
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            var items = new CustomListObjectElementCollection();

            using (HttpClient client = new HttpClient())
            {

                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + data.Properties["Token"]);

                HttpResponseMessage response = client.GetAsync("https://api.smartsheet.com/2.0/sheets").Result;
                var responseBody = response.Content.ReadAsStringAsync().Result;

                if (response.IsSuccessStatusCode)
                {
                    var dyn = JsonConvert.DeserializeObject<dynamic>(responseBody);
                    foreach (var row in dyn.data)
                    {
                        var item = new CustomListObjectElement();
                        item.Add("ID", Convert.ToString( row["id"]));
                        item.Add("Name", Convert.ToString(row["name"]));
                        items.Add(item);
                    }
                }
                else
                {
                    throw new Exception( "Error during call -> " + response.StatusCode + response.ReasonPhrase);
                }

            }
            return items;
        }

    }
}
