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
        

        public async Task<string> GetAsync(string key = null, RequestParameters parameters = null)
        {
            // build request
            var request = this.Builder.GetRequest(out var url, key, parameters);
            
            // call graph api
            var response = await HttpClient.SendAsync(request);
            
            // convert to string
            /*
             * Large Object Heap: code that allocates a lot of memory in LOH
             * Allocated object type: String
             * Last observation: 12.04.2023 13:51 ConsoleApplication1.exe
             * Allocated size: 105,8 MB
             * TODO??
             */
            string jsonString = await response.Content.ReadAsStringAsync();

            // check response status code: Status code not 200 OK => ERROR
            if (response.StatusCode != HttpStatusCode.OK)
            { 
                DeserializeError(jsonString, url);
            }

            return jsonString;
        }

        public static void DeserializeError(string json, string url = null)
        {
            // try deserializing response into MsGraphError object
            var error = JsonConvert.DeserializeObject<RootMsGraphError>(json)?.Error;

            if (error?.Message == null || error.Code == null)
            {
                throw new MsGraphException($"Unknown Microsoft Graph Error: {json}", url);
            }
            throw new MsGraphException($"Microsoft Graph Error: {error.Code}: {error.Message}", error.Code, url);

        }

        public string GetAccessToken()
        {
            return AccessToken;
        }

        public string GetExpirationTime()
        {
            return TokenLifetime;
        }

        public long GetMillis()
        {
            return Millis;
        }
    }
}