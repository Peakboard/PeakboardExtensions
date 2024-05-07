using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WheelMe
{
    public static class Extensions
    {
        public static async Task<T> GetRequestAsync<T>(this HttpClient client, string requestUri)
        {
            var response = await client.GetAsync(requestUri);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error while sending request to: {requestUri}, response status code: {response.StatusCode}");
            }
            
            var responseBody = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<T>(responseBody);
            return result;
        }
        
        public static async Task PostRequestAsync(this HttpClient client, string requestUri, object payload)
        {
            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync(requestUri, content);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error while sending request to: {requestUri}, response status code: {response.StatusCode}");
            }
        }
        
        public static async Task<T> PostRequestAsync<T>(this HttpClient client, string requestUri, object payload)
        {
            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            var response = await client.PostAsync(requestUri, content);
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Error while sending request to: {requestUri}, response status code: {response.StatusCode}");
            }
            
            var responseBody = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<T>(responseBody);
            return result;
        }
    }
}