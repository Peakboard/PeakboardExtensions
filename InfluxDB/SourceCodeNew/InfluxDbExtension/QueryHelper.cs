using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace InfluxDbExtension
{
    public class QueryHelper
    {
        public static async Task<Tuple<string,bool>> QueryAsync(string url, string token, Content content)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", token);
            client.DefaultRequestHeaders.Add("Accept", "application/csv");
            
            var jsonContent = JsonContent.Create<Content>(content);

            var response = await client.PostAsync(new Uri(url), jsonContent);

            var csv = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                var error = GetError(csv);
                return new Tuple<string, bool>(error, false);
            }

            return new Tuple<string, bool>(csv, true);
        }

        public static async Task<Tuple<string, bool>> WriteAsync(string url, string token, string content)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", token);

            var reqeustContent = new StringContent(content, Encoding.UTF8, "text/plain");

            var response = await client.PostAsync(new Uri(url), reqeustContent);

            if (!response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                var error = GetError(responseBody);
                return new Tuple<string, bool>(error, false);
            }

            return new Tuple<string, bool>("", true);
        }

        private static string GetError(string response)
        {
            JObject obj = JObject.Parse(response);
            return (string)obj.SelectToken("message");
        }
    }
}