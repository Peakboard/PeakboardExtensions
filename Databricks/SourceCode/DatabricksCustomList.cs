using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using Peakboard.ExtensionKit;

namespace Databricks
{
    [Serializable]
    [CustomListIcon("Databricks.databricks.png")]
    
    class DatabricksCustomList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "Databricks",
                Name = "Databricks",
                Description = "Fetches data from Databricks API",
                PropertyInputPossible = true,
                PropertyInputDefaults = {
                new CustomListPropertyDefinition() { Name = "URL", Value = "" },
                new CustomListPropertyDefinition() { Name = "BearerToken", Value = "" },
                new CustomListPropertyDefinition() { Name = "Body", Value = "", MultiLine = true }
            }
            };
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            var response = PostToApi(data.Properties["URL"], data.Properties["BearerToken"], data.Properties["Body"]).Result;
            var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(response);

            var columns = new CustomListColumnCollection();
            foreach (var column in apiResponse.Manifest.Schema.Columns)
            {
                columns.Add(new CustomListColumn(column.Name, GetColumnType(column.TypeText)));
            }

            return columns;
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            var response = PostToApi(data.Properties["URL"], data.Properties["BearerToken"], data.Properties["Body"]).Result;
            var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(response);

            var items = new CustomListObjectElementCollection();
            foreach (var row in apiResponse.Result.DataArray)
            {
                var item = new CustomListObjectElement();
                for (int i = 0; i < row.Length; i++)
                {
                    item.Add(apiResponse.Manifest.Schema.Columns[i].Name, row[i]);
                }
                items.Add(item);
            }

            return items;
        }

        public static async Task<string> PostToApi(string url, string BearerToken, string Body)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", BearerToken);

                var postData = new { param = Body };
                var content = new StringContent(Body, System.Text.Encoding.UTF8, "application/json");

                HttpResponseMessage response = await httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
                else
                {
                    throw new InvalidOperationException($"API call failed with status code: {response.StatusCode}");
                }
            }
        }

        private CustomListColumnTypes GetColumnType(string typeText)
        {
            string upperTypeText = typeText.ToUpper();

            switch (upperTypeText)
            {
                case "STRING":
                    return CustomListColumnTypes.String;
                case "BIGINT":
                case "INT":
                    return CustomListColumnTypes.Number;
                case "BOOLEAN":
                    return CustomListColumnTypes.Boolean;
                default:
                    return CustomListColumnTypes.String;
            }
        }

        public class ApiResponse
        {
            public Manifest Manifest { get; set; }
            public Result Result { get; set; }
        }

        public class Manifest
        {
            public Schema Schema { get; set; }
        }

        public class Schema
        {
            public List<Column> Columns { get; set; }
        }

        public class Column
        {
            public string Name { get; set; }
            [JsonProperty("type_text")]
            public string TypeText { get; set; }
        }

        public class Result
        {
            [JsonProperty("data_array")]
            public string[][] DataArray { get; set; }
        }
    }
}
