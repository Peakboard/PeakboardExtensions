using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PeakboardExtensionGraph
{
    public class GraphHelper
    {
        
        private static HttpClient _httpClient;
        public static RequestBuilder Builder;

        private static string _accessToken;
        private static string _refreshToken;
        private static string _tokenLifetime;
        private static long _millis;

        private const string AUTHORIZATION_URL = "https://login.microsoftonline.com/{0}/oauth2/v2.0/devicecode";
        private const string TOKEN_ENDPOINT_URL = "https://login.microsoftonline.com/{0}/oauth2/v2.0/token";

        private static string _allScopeAuthorizations = "user.read offline_access";
        private static string _clientId = "067207ed-41a4-4402-b97f-b977babe0ec9"; 
        private static string _tenantId = "b4ff9807-402f-42b8-a89d-428363c55de7";
        
        public static async Task<bool> InitGraph(string clientId, string tenantId, string permissions, Func<string, string, Task> prompt)
        {
            _httpClient = new HttpClient();
            _clientId = clientId;
            _tenantId = tenantId;
            _allScopeAuthorizations = permissions;
            
            // authorize
            string deviceCode = await AuthorizeAsync(prompt);
            _ = deviceCode ?? throw new Exception("Authorization failed");
            
            // get tokens
            bool success = false;
            int requestAttempts = 0;
            Thread.Sleep(3000);
            while (!success && requestAttempts < 20)
            {
                // try to receive tokens 20 times
                success = await GetTokensAsync(deviceCode);
                Thread.Sleep(3000);
                requestAttempts++;
            }
            // abort if no success
            if (requestAttempts == 20) throw new Exception("Failed to receive tokens 20 times. Aborting...");

            // init request builder
            Builder = new RequestBuilder(_accessToken);

            return true;
        }

        public static async Task InitGraphWithRefreshToken(string token, string clientId, string tenantId, string permissions)
        {
            // Initialize attributes
            _clientId = clientId;
            _tenantId = tenantId;
            _allScopeAuthorizations = permissions;
            
            // Initialize via refresh token (in runtime)
            _refreshToken = token;
            _httpClient = new HttpClient();
            await RefreshTokensAsync();
            Builder = new RequestBuilder(_accessToken);
        }

        private static async Task<string> AuthorizeAsync(Func<string, string, Task> prompt)
        {
            // generate url for http request
            string url = string.Format(AUTHORIZATION_URL, _tenantId);
            
            // generate body for http request
            var values = new Dictionary<string, string>
            {
                {"client_id", _clientId},
                {"scope", _allScopeAuthorizations}
            };
            
            FormUrlEncodedContent data = new FormUrlEncodedContent(values);
            
            // make http request to get device code for authentication
            HttpResponseMessage response = await _httpClient.PostAsync(url, data);
            string jsonString = await response.Content.ReadAsStringAsync();
            Console.WriteLine(jsonString);
            var authorizationResponse = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);
            
            // get device code and authentication message
            string deviceCode = null;
            string uri = null;
            string userCode = null;
            if (authorizationResponse != null)
            {
                authorizationResponse.TryGetValue("device_code", out deviceCode);
                authorizationResponse.TryGetValue("verification_uri", out uri);
                authorizationResponse.TryGetValue("user_code", out userCode);
            }
            
            // execute prompt
            await prompt(userCode, uri);
            
            // return device code for token request
            return deviceCode;
        }

        private static async Task<bool> GetTokensAsync(string deviceCode)
        {
            // generate url for http request
            string url = string.Format(TOKEN_ENDPOINT_URL, _tenantId);
            
            // generate body for http request
            var values = new Dictionary<string, string>
            {
                { "grant_type", "urn:ietf:params:oauth:grant-type:device_code" },
                { "client_id", _clientId },
                { "device_code", deviceCode }
            };
            
            FormUrlEncodedContent data = new FormUrlEncodedContent(values);
            
            // make http request to get access token and refresh token
            HttpResponseMessage response = await _httpClient.PostAsync(url, data);
            string jsonString = await response.Content.ReadAsStringAsync();
            
            // catch error if user didn't authenticate (yet)
            if (jsonString.Contains("error")) return false;

            try
            {
                var tokenResponse = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);

                if (tokenResponse != null)
                {
                    // store token values
                    tokenResponse.TryGetValue("refresh_token", out _refreshToken);
                    tokenResponse.TryGetValue("access_token", out _accessToken);
                    tokenResponse.TryGetValue("expires_in", out _tokenLifetime);
                }
                else return false;

                _millis = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static async Task RefreshTokensAsync()
        {
            // generate url for http request
            string url = String.Format(TOKEN_ENDPOINT_URL, _tenantId);
            
            // generate body for http requestd
            var values = new Dictionary<string, string>
            {
                { "client_id", _clientId },
                { "grant_type", "refresh_token" },
                { "scope", _allScopeAuthorizations },
                { "refresh_token", _refreshToken }
            };

            var data = new FormUrlEncodedContent(values);
            
            // make http request to get new tokens
            HttpResponseMessage response = await _httpClient.PostAsync(url, data);
            string jsonString = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);

            if (tokenResponse != null)
            {
                tokenResponse.TryGetValue("access_token", out _accessToken);
                tokenResponse.TryGetValue("refresh_token", out _refreshToken);
                tokenResponse.TryGetValue("expires_in", out _tokenLifetime);
            }

            Builder?.RefreshToken(_accessToken);

            _millis = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        public static async Task<bool> CheckIfTokenExpiredAsync()
        {
            // check if token expired
            long temp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            if ((temp - _millis) / 1000 > Int32.Parse(_tokenLifetime))
            {
                // refresh tokens
                await RefreshTokensAsync();
                return true;
            }

            return false;
        }

        public static async Task<string> MakeGraphCall(string key = null, RequestParameters parameters = null)
        {   
            // build request
            var request = Builder.GetRequest(key, parameters);
            
            // call graph api
            var response = await _httpClient.SendAsync(request);
            
            // convert to string and return
            string jsonString = await response.Content.ReadAsStringAsync();

            return jsonString;
        }

        public static string GetRefreshToken()
        {
            if (_refreshToken != null) return _refreshToken;
            throw new NullReferenceException("Refresh-Token not initialized yet");
        }

    }
}