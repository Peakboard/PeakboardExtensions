using Newtonsoft.Json.Linq;
using Peakboard.ExtensionKit;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using static PeakboardExtensionHue.HueHelper;

namespace HueFunctionPlaygroundConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var resp = GetColumnsOverrideStatic("https://api.smartsheet.com/2.0/sheets/6952350955923332", "79vCC7kxcoUWQlicBCzxymEdeuc0AJpvNbjID");
        }

        public static CustomListColumnCollection GetColumnsOverrideStatic(string url, string token)
        {
            var cols = new CustomListColumnCollection();
            var jsonResponse = GetJsonResponseStatic(url, token).Result;

            var columns = jsonResponse["columns"];
            foreach (var column in columns)
            {
                var columnName = column["title"].ToString();
                var dataType = column["type"].ToString();
                var listColumnType = GetCustomListColumnTypeStatic(dataType);
                cols.Add(new CustomListColumn(columnName, listColumnType));
            }

            return cols;
        }

        public static async Task<JObject> GetJsonResponseStatic(string apiUrl, string authToken)
        {
            using (var client = new HttpClient())
            {
                if (!string.IsNullOrEmpty(authToken))
                {
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authToken);
                }

                HttpResponseMessage response = await client.GetAsync(apiUrl);
                if (!response.IsSuccessStatusCode)
                {
                    throw new InvalidOperationException($"API call failed with status code: {response.StatusCode}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                return JObject.Parse(responseContent);
            }
        }

        private static CustomListColumnTypes GetCustomListColumnTypeStatic(string type)
        {
            switch (type.ToLower())
            {
                case "text_number":
                case "string":
                case "date":
                    return CustomListColumnTypes.String;
                case "checkbox":
                case "boolean":
                    return CustomListColumnTypes.Boolean;
                case "number":
                case "integer":
                case "float":
                case "double":
                case "decimal":
                    return CustomListColumnTypes.Number;
                default:
                    return CustomListColumnTypes.String;
            }
        }
    }
}
