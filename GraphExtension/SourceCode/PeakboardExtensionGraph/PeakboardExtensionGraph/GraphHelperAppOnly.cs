using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PeakboardExtensionGraph
{
    public class GraphHelperAppOnly
    {
        private static HttpClient _httpClient;
        private static RequestBuilder _builder;
        private static string _accessToken;
        
        private static string _tokenLifetime;
        private static long _millis;
        
        private const string TokenEndpointUrl = "https://login.microsoftonline.com/{0}/oauth2/v2.0/token";
        private static string _clientId = "067207ed-41a4-4402-b97f-b977babe0ec9"; 
        private static string _tenantId = "b4ff9807-402f-42b8-a89d-428363c55de7";
        private static string _clientSecret = "OBy8Q~M0pJQDqXIsV57e_MUKO6x69IRLPgbtIbmC";

        public static async Task InitGraph(string clientId, string tenantId, string clientSecret)
        {
            _clientId = clientId;
            _tenantId = tenantId;
            _clientSecret = clientSecret;
            
            string url = string.Format(TokenEndpointUrl, _tenantId);
            
            Dictionary<string, string> values = new Dictionary<string, string>
            {
                {"client_id", _clientId},
                {"scope", "https://graph.microsoft.com/.default"},
                {"client_secret", _clientSecret},
                {"grant_type", "client_credentials"}
            };
            FormUrlEncodedContent data = new FormUrlEncodedContent(values);

            _httpClient = new HttpClient();
            HttpResponseMessage response = await _httpClient.PostAsync(url, data);

            string jsonString = await response.Content.ReadAsStringAsync();

            var authorizationResponse = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);
            if (authorizationResponse != null)
            {
                authorizationResponse.TryGetValue("access_token", out _accessToken);
                authorizationResponse.TryGetValue("expires_in", out _tokenLifetime);
                _millis = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            }
            else
            {
                throw new Exception("Unable to initialize graph");
                // TODO Log graph errors?
            }

            _builder = new RequestBuilder(_accessToken, "https://graph.microsoft.com/v1.0");
        }
        
        
        public static async Task<bool> CheckIfTokenExpiredAsync()
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

        private static async Task RefreshAccessAsync()
        {
            string url = string.Format(TokenEndpointUrl, _tenantId);
            
            Dictionary<string, string> values = new Dictionary<string, string>
            {
                {"client_id", _clientId},
                {"scope", "https://graph.microsoft.com/.default"},
                {"client_secret", _clientSecret},
                {"grant_type", "client_credentials"}
            };
            FormUrlEncodedContent data = new FormUrlEncodedContent(values);
            
            HttpResponseMessage response = await _httpClient.PostAsync(url, data);
            
            string jsonString = await response.Content.ReadAsStringAsync();
            
            var authorizationResponse = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);
            if (authorizationResponse != null)
            {
                authorizationResponse.TryGetValue("access_token", out _accessToken);
                authorizationResponse.TryGetValue("expires_in", out _tokenLifetime);
                _millis = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            }
            else
            {
                throw new Exception("Unable to regain access");
                // TODO Log graph errors?
            }
            
            _builder.RefreshToken(_accessToken);
        }


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

    }
}