using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PeakboardExtensionGraph
{
    public abstract class GraphHelperBase
    {

        protected RequestBuilder Builder = null;
        protected HttpClient HttpClient = null;
        
        protected string AccessToken;
        protected string TokenLifetime;
        protected long Millis;
        
        protected const string TokenEndpointUrl = "https://login.microsoftonline.com/{0}/oauth2/v2.0/token";

        protected string ClientId; 
        protected string TenantId;
        

        public async Task<string> MakeGraphCall(string key = null, RequestParameters parameters = null)
        {
            // build request
            var request = this.Builder.GetRequest(out var url, key, parameters);
            
            // call graph api
            var response = await HttpClient.SendAsync(request);
            
            // convert to string and return
            string jsonString = await response.Content.ReadAsStringAsync();

            if (response.StatusCode != HttpStatusCode.OK)
            { 
                DeserializeError(jsonString);
            }

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