using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Test
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var response = PostToApi("https://dbc-c26bc7d1-a977.cloud.databricks.com/api/2.0/sql/statements", "dapi2c07fc6eeb620ad5bce0da0f1b1b6369", "{\"warehouse_id\": \"54bd69de3846a5b4\",\"statement\": \"SELECT * from prod.logistics.base_order_excellence limit 5\"}").Result;
            var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(response);

        }

        public static async Task<string> PostToApi(string url, string BearerToken, string Body)
        {
            using (var httpClient = new HttpClient())
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", BearerToken);

                var postData = new { param = Body };
                var json = JsonConvert.SerializeObject(postData);
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
            public string Type_Text { get; set; }
        }

        public class Result
        {
            public string[][] Data_Array { get; set; }
        }
    }
}
