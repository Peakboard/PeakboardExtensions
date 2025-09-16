using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Peakboard.ExtensionKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace MPDV
{
    [Serializable]
    [CustomListIcon("MPDV.logo.png")]
    class MPDVCustomList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "MPDV",
                Name = "MPDV",
                Description = "Fetches data from MPDV API",
                PropertyInputPossible = true,
                PropertyInputDefaults = {
                    new CustomListPropertyDefinition() { Name = "URL", Value = "" },
                    new CustomListPropertyDefinition() { Name = "User", Value = "" },
                    new CustomListPropertyDefinition() { Name = "Password", Value = "", Masked = true },
                    new CustomListPropertyDefinition() { Name = "XAccessId", Value = "", Masked = true },
                    new CustomListPropertyDefinition() { Name = "Body", Value = "", MultiLine = true }
                }
            };
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            return GetColumns(data);
        }

        private CustomListColumnCollection GetColumns(CustomListData data, string response = null)
        {
            if (response == null)
            {
                response = PostToApi(data).Result;
            }

            var columns = new CustomListColumnCollection();

            // Get the meta-data for column names
            var apiResponse = JsonConvert.DeserializeObject(response) as JArray;
            var metaData = JsonConvert.DeserializeObject<MetaData>(apiResponse[0].ToString());

            foreach (var col in metaData.data)
            {
                columns.Add(new CustomListColumn(GetColumnName(col.name, columns.Select(c => c.Name).ToArray()), GetColumnType(col.type)));
            }

            return columns;
        }


        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            var response = PostToApi(data).Result;
            var apiResponse = JsonConvert.DeserializeObject<List<object>>(response);

            // Get the meta-data for column names
            var cols = GetColumns(data, response);

            var items = new CustomListObjectElementCollection();
            for (int i = 1; i < apiResponse.Count; i++) // Start from 1 as the first element contains meta-data
            {
                var dataInfo = JsonConvert.DeserializeObject<DataInfo>(apiResponse[i].ToString());
                var item = new CustomListObjectElement();

                for (int j = 0; j < cols.Count; j++)
                {
                    var columnName = cols[j].Name;
                    var val = dataInfo.data[j];

                    if (val == null)
                    {
                        val = cols[j].Type == CustomListColumnTypes.String ? string.Empty : val;
                        val = cols[j].Type == CustomListColumnTypes.Boolean ? false : val;
                        val = cols[j].Type == CustomListColumnTypes.Number ? 0 : val;
                    }

                    item.Add(columnName, val);
                }
                items.Add(item);
            }

            return items;
        }

        public static async Task<string> PostToApi(CustomListData data)
        {
            using (var httpClient = new HttpClient())
            {
                var byteArray = Encoding.ASCII.GetBytes($"{data.Properties["User"]}:{data.Properties["Password"]}");
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                httpClient.DefaultRequestHeaders.Add("X-Access-Id", data.Properties["XAccessId"]);

                var content = new StringContent(data.Properties["Body"], Encoding.UTF8, "application/json");

                HttpResponseMessage response = await httpClient.PostAsync(data.Properties["URL"], content);

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
            var upperTypeText = typeText.ToUpper();

            switch (upperTypeText)
            {
                case "STRING":
                    return CustomListColumnTypes.String;
                case "INTEGER":
                    return CustomListColumnTypes.Number;
                case "BOOLEAN":
                    return CustomListColumnTypes.Boolean;
                default:
                    return CustomListColumnTypes.String;
            }
        }

        private string GetColumnName(string rawName, string[] columns)
        {
            var newName = rawName.Split('.').Last();
            var cnt = 0;

            while(true)
            {
                if ((columns.Contains(newName) && cnt == 0)
                    || (columns.Contains(newName + cnt) && cnt > 0))
                {
                    cnt++;
                }
                else
                {
                    if (cnt == 0)
                    {
                        return newName;
                    }
                    else
                    {
                        return newName + cnt;
                    }
                }
            }
        }

        public class MetaData
        {
            public string __rowType { get; set; }
            public string __type { get; set; }
            public List<ColumnInfo> data { get; set; }
        }

        public class ColumnInfo
        {
            public string name { get; set; }
            public int index { get; set; }
            public string type { get; set; }
        }

        public class DataInfo
        {
            public string __rowType { get; set; }
            public string __type { get; set; }
            public List<object> data { get; set; }
        }
    }
}