using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Policy;
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
        

        public async Task<GraphResponse> ExtractAsync(string requestUri = null, RequestParameters parameters = null)
        {
            // build request
            var request = this.Builder.GetRequest(out var url, requestUri, parameters);
            
            // call graph api
            var response = await HttpClient.SendAsync(request);
            
            // convert to string
            /*
             * Large Object Heap: code that allocates a lot of memory in LOH
             * Allocated object type: String
             * Last observation: 12.04.2023 13:51 GraphFunctionTestConsole.exe
             * Allocated size: 105,8 MB
             * TODO??
             */
            string content = await response.Content.ReadAsStringAsync();

            // check response status code: Status code not 200 OK => ERROR
            if (!response.IsSuccessStatusCode)
            { 
                DeserializeError(content, url);
            }
            

            // check response for content type
            if (response.Content.Headers.ContentType.MediaType.Contains("application/json"))
            {
                return new GraphResponse()
                {
                    Content = content,
                    Type = GraphContentType.Json
                };
            }
            if(response.Content.Headers.ContentType.MediaType.Contains("application/octet-stream"))
            {
                return new GraphResponse()
                {
                    Content = content,
                    Type = GraphContentType.OctetStream
                };
            }
            else
            {
                throw new MsGraphException($"Unsupported Content Type with call {url}");
            }
        }

        public async Task<GraphResponse> ExtractAsync(string url, string body)
        {
            // Extract method for custom call which allows post requests aswell
            HttpRequestMessage request;
            if (!String.IsNullOrEmpty(body))
            {
                request = this.Builder.PostRequest(url, body);
            }
            else
            {
                request = this.Builder.GetRequest(out var uri, url);
            }


            var response = await HttpClient.SendAsync(request);
            
            string content = await response.Content.ReadAsStringAsync();

            // check response status code: Status code not 200 OK => ERROR
            if (!response.IsSuccessStatusCode)
            { 
                DeserializeError(content, url);
            }
            

            // check response for content type
            if (response.Content.Headers.ContentType.MediaType.Contains("application/json"))
            {
                return new GraphResponse()
                {
                    Content = content,
                    Type = GraphContentType.Json
                };
            }
            if(response.Content.Headers.ContentType.MediaType.Contains("application/octet-stream"))
            {
                return new GraphResponse()
                {
                    Content = content,
                    Type = GraphContentType.OctetStream
                };
            }
            else
            {
                throw new MsGraphException($"Unsupported Content Type with call {url}");
            }
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