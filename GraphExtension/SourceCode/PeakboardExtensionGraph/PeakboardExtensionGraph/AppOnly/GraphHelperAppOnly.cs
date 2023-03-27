using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PeakboardExtensionGraph.AppOnly
{
    public class GraphHelperAppOnly : GraphHelperBase
    {
        private string _clientSecret = "OBy8Q~M0pJQDqXIsV57e_MUKO6x69IRLPgbtIbmC";

        public GraphHelperAppOnly(string clientId, string tenantId, string clientSecret)
        {
            _clientId = clientId;
            _tenantId = tenantId;
            _clientSecret = clientSecret;
        }

        public async Task InitGraph()
        {
            // has to be called after initializing GraphHelper object
            
            // form authorization url
            string url = string.Format(TokenEndpointUrl, _tenantId);
            
            // request body
            Dictionary<string, string> values = new Dictionary<string, string>
            {
                {"client_id", _clientId},
                {"scope", "https://graph.microsoft.com/.default"},
                {"client_secret", _clientSecret},
                {"grant_type", "client_credentials"}
            };
            FormUrlEncodedContent data = new FormUrlEncodedContent(values);

            // post request to get access to graph application
            _httpClient = new HttpClient();
            HttpResponseMessage response = await _httpClient.PostAsync(url, data);

            string jsonString = await response.Content.ReadAsStringAsync();

            // check for error status codes in http response
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new MsGraphException($"Authorization failed.\n Status Code: {response.StatusCode}\n Error: {jsonString}");
            }

            // get values from response 
            var authorizationResponse = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);
            if (authorizationResponse != null)
            {
                authorizationResponse.TryGetValue("access_token", out _accessToken);
                authorizationResponse.TryGetValue("expires_in", out _tokenLifetime);
                _millis = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            }
            else
            {
                throw new Exception($"Authorization failed:\n {jsonString}");
            }

            // init request builder
            _builder = new RequestBuilder(_accessToken, "https://graph.microsoft.com/v1.0");
        }
        
        
        public async Task<bool> CheckIfTokenExpiredAsync()
        {
            // check if token expired
            long temp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            if ((temp - _millis) / 1000 > Int32.Parse(_tokenLifetime))
            {
                // refresh tokens
                await RefreshAccessAsync();
                return true;
            }

            return false;
        }

        private async Task RefreshAccessAsync()
        {
            // from request url
            string url = string.Format(TokenEndpointUrl, _tenantId);
            
            // request body
            Dictionary<string, string> values = new Dictionary<string, string>
            {
                {"client_id", _clientId},
                {"scope", "https://graph.microsoft.com/.default"},
                {"client_secret", _clientSecret},
                {"grant_type", "client_credentials"}
            };
            FormUrlEncodedContent data = new FormUrlEncodedContent(values);
            
            // post request for new access token
            HttpResponseMessage response = await _httpClient.PostAsync(url, data);
            
            string jsonString = await response.Content.ReadAsStringAsync();
            
            // check for error status codes in http response
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new MsGraphException($"Failed to regain access.\n Status Code: {response.StatusCode}\n Error: {jsonString}");
            }
            
            // get values from response
            var authorizationResponse = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);
            if (authorizationResponse != null)
            {
                authorizationResponse.TryGetValue("access_token", out _accessToken);
                authorizationResponse.TryGetValue("expires_in", out _tokenLifetime);
                _millis = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            }
            else
            {
                throw new Exception($"Unable to regain access:\n {jsonString}");
            }
            
            // replace access token in request builder
            _builder.RefreshToken(_accessToken);
        }
        

    }
}