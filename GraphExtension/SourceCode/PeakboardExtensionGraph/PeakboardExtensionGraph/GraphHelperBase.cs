using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PeakboardExtensionGraph
{
    public abstract class GraphHelperBase
    {
        // TODO: Make everything nonstatic?
        
        protected static RequestBuilder _builder = null;
        protected static HttpClient _httpClient = null;
        
        protected static string _accessToken;
        protected static string _tokenLifetime;
        protected static long _millis;
        
        protected const string TokenEndpointUrl = "https://login.microsoftonline.com/{0}/oauth2/v2.0/token";

        protected static string _clientId; 
        protected static string _tenantId;
        

        public static async Task<string> MakeGraphCall(string key = null, RequestParameters parameters = null)
        {
            // build request
            var request = _builder.GetRequest(out var url, key, parameters);
            
            // call graph api
            var response = await _httpClient.SendAsync(request);
            
            // convert to string and return
            string jsonString = await response.Content.ReadAsStringAsync();

            JsonHelper.FindGraphError(jsonString);

            return jsonString;
        }
        
        public static void DeserializeError(string json)
        {
            var error = JsonConvert.DeserializeObject<RootMsGraphError>(json)?.Error;

            if (error?.Message == null || error.Code == null)
            {
                throw new MsGraphException($"Unknown Microsoft Graph Error: {json}");
            }
            throw new MsGraphException($"Microsoft Graph Error: {error.Code}: {error.Message}");

        }
    }
}