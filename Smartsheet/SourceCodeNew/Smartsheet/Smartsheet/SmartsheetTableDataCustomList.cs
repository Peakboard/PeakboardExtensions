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
    
    class SmartsheetTableDataCustomList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "SmartsheetTableData",
                Name = "SmartsheetTableData",
                Description = "Fetches data from Smartsheet API",
                PropertyInputPossible = true,
                PropertyInputDefaults =
                {
                    new CustomListPropertyDefinition() { Name = "Token", Value = "XXX" },
                    new CustomListPropertyDefinition() { Name = "ListID", Value = "6952350955923332" },
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

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + data.Properties["Token"]);

                HttpResponseMessage response = client.GetAsync("https://api.smartsheet.com/2.0/sheets/" + data.Properties["ListID"]).Result;
                var responseBody = response.Content.ReadAsStringAsync().Result;

                if (response.IsSuccessStatusCode)
                {
                    // System.Windows.Forms.MessageBox.Show("jetzt");
                    var dyn = JsonConvert.DeserializeObject<dynamic>(responseBody);

                    if (dyn.columns == null) { throw new Exception("Columns are empty"); }

                    foreach (var row in dyn.columns)
                    {
                        string id = row["id"];
                        string title = row["title"];
                        string type = row["type"];

                        columns.Add(new CustomListColumn(title, CustomListColumnTypes.String));
                    }

                }
                else
                {
                    throw new Exception("Error during call -> " + response.StatusCode + response.ReasonPhrase);
                }
            }


            return columns;
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            var items = new CustomListObjectElementCollection();

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + data.Properties["Token"]);

                HttpResponseMessage response = client.GetAsync("https://api.smartsheet.com/2.0/sheets/" + data.Properties["ListID"]).Result;
                var responseBody = response.Content.ReadAsStringAsync().Result;

                if (response.IsSuccessStatusCode)
                {
                    // System.Windows.Forms.MessageBox.Show("jetzt");
                    var dyn = JsonConvert.DeserializeObject<dynamic>(responseBody);

                    if (dyn.columns == null) { throw new Exception("Columns are empty"); }

                    List<KeyValuePair<string,string>> MyColumns = new List<KeyValuePair<string, string>>();

                    // System.Windows.Forms.MessageBox.Show("now");
                        ;
                    foreach (var col in dyn.columns)
                    {
                        string id = col["id"];
                        string title = col["title"];
                        string type = col["type"];
                        MyColumns.Add(new KeyValuePair<string, string>(id, title));
                        
                    }

                    if (dyn.rows == null) { throw new Exception("Rows are empty"); }

                    foreach (var row in dyn.rows)
                    {
                        if (row.cells == null) { throw new Exception("Cells are empty"); }

                        string rowNumber = row["id"];
                        var item = new CustomListObjectElement();

                        foreach(var metadatacolumn in MyColumns)
                        {
                            string val = string.Empty;

                            foreach (var cell in row.cells)
                            {
                                if (cell.value != null && cell.columnId == metadatacolumn.Key)
                                {
                                    val = Convert.ToString(cell.value); 
                                }
                            }
                            

                            item.Add(metadatacolumn.Value, val);
                        }

                        items.Add(item);
                    }

                }
                else
                {
                    throw new Exception("Error during call -> " + response.StatusCode + response.ReasonPhrase);
                }
            }

            return items;
        }

    }
}
