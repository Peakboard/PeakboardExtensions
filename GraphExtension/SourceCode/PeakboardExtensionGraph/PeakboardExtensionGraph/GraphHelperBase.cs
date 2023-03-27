using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PeakboardExtensionGraph
{
    public abstract class GraphHelperBase
    {

        protected RequestBuilder _builder = null;
        protected HttpClient _httpClient = null;
        
        protected string _accessToken;
        protected string _tokenLifetime;
        protected long _millis;
        
        protected const string TokenEndpointUrl = "https://login.microsoftonline.com/{0}/oauth2/v2.0/token";

        protected string _clientId; 
        protected string _tenantId;
        

        public async Task<string> MakeGraphCall(string key = null, RequestParameters parameters = null)
        {
            // build request
            var request = this._builder.GetRequest(out var url, key, parameters);
            
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