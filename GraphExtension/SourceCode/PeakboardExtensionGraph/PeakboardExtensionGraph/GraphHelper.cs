using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PeakboardExtensionGraph
{
    public class GraphHelper
    {
        
        private static HttpClient _httpClient;
        public static RequestBuilder _builder;

        private static string accessToken;
        private static string refreshToken;
        private static string tokenLifetime;
        private static long millis;

        private const string AUTHORIZATION_URL = "https://login.microsoftonline.com/{0}/oauth2/v2.0/devicecode";  
        private const string ALL_SCOPE_AUTHORIZATIONS = "user.read offline_access";
        private const string TOKEN_ENDPOINT_URL = "https://login.microsoftonline.com/{0}/oauth2/v2.0/token";

        private const string ClientID = "067207ed-41a4-4402-b97f-b977babe0ec9"; 
        private const string TenantID = "b4ff9807-402f-42b8-a89d-428363c55de7";
        
        public static async Task<bool> InitGraph(Func<string, string, Task> prompt)
        {
            _httpClient = new HttpClient();
            
            // authorize
            string deviceCode = await AuthorizeAsync(prompt);
            _ = deviceCode ?? throw new Exception("Authorization failed");
            
            // get tokens
            bool success = false;
            int requestAttempts = 0;
            //Thread.Sleep(0);
            while (!success && requestAttempts < 20)
            {
                // try to receive tokens 20 times
                success = await GetTokensAsync(deviceCode);
                Thread.Sleep(3000);
                requestAttempts++;
            }
            // abort if no success
            if (requestAttempts == 20) throw new Exception("Failed to receive tokens 20 times. Aborting...");

            // init authentication provider
            
            _builder = new RequestBuilder(accessToken);

            return true;
        }

        public static async Task InitGraphInRuntime(string token)
        {
            // Initialize via refresh token (in runtime)
            refreshToken = token;
            _httpClient = new HttpClient();
            await RefreshTokensAsync();
            _builder = new RequestBuilder(accessToken);
        }

        private static async Task<string> AuthorizeAsync(Func<string, string, Task> prompt)
        {
            // generate url for http request
            string url = string.Format(AUTHORIZATION_URL, TenantID);
            
            // generate body for http request
            Dictionary<string, string> values = new Dictionary<string, string>
            {
                {"client_id", ClientID},
                {"scope", ALL_SCOPE_AUTHORIZATIONS}
            };
            
            FormUrlEncodedContent data = new FormUrlEncodedContent(values);
            
            // make http request to get device code for authentication
            HttpResponseMessage response = await _httpClient.PostAsync(url, data);
            string jsonString = await response.Content.ReadAsStringAsync();
            Console.WriteLine(jsonString);
            var authorizationResponse = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);
            
            // get device code and authentication message
            string deviceCode = null;
            string message = null;
            string uri = null;
            string userCode = null;
            if (authorizationResponse != null)
            {
                authorizationResponse.TryGetValue("device_code", out deviceCode);
                authorizationResponse.TryGetValue("message", out message);
                authorizationResponse.TryGetValue("verification_uri", out uri);
                authorizationResponse.TryGetValue("user_code", out userCode);
            }

            await prompt(userCode, uri);

            Console.WriteLine(message ?? "Error");

            return deviceCode;
        }

        private static async Task<bool> GetTokensAsync(string deviceCode)
        {
            // generate url for http request
            string url = string.Format(TOKEN_ENDPOINT_URL, TenantID);
            
            // generate body for http request
            var values = new Dictionary<string, string>
            {
                { "grant_type", "urn:ietf:params:oauth:grant-type:device_code" },
                { "client_id", ClientID },
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
                    tokenResponse.TryGetValue("refresh_token", out refreshToken);
                    tokenResponse.TryGetValue("access_token", out accessToken);
                    tokenResponse.TryGetValue("expires_in", out tokenLifetime);
                }
                else return false;

                millis = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        private static async Task RefreshTokensAsync()
        {
            // generate url for http request
            string url = String.Format(TOKEN_ENDPOINT_URL, TenantID);
            
            // generate body for http requestd
            var values = new Dictionary<string, string>
            {
                { "client_id", ClientID },
                { "grant_type", "refresh_token" },
                { "scope", ALL_SCOPE_AUTHORIZATIONS },
                { "refresh_token", refreshToken }
            };

            var data = new FormUrlEncodedContent(values);
            
            // make http request to get new tokens
            HttpResponseMessage response = await _httpClient.PostAsync(url, data);
            string jsonString = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);

            if (tokenResponse != null)
            {
                tokenResponse.TryGetValue("access_token", out accessToken);
                tokenResponse.TryGetValue("refresh_token", out refreshToken);
                tokenResponse.TryGetValue("expires_in", out tokenLifetime);
            }

            millis = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            
        }

        public static async Task CheckTokenLifetimeAsync()
        {
            // check if token expired
            long temp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            if ((temp - millis) / 1000 > Int32.Parse(tokenLifetime))
            {
                Console.Write("Refreshing Tokens...");
                // refresh tokens
                await RefreshTokensAsync();
                Console.WriteLine("Done!");
            }
        }

        public static async Task<string> MakeGraphCall(string key = null, RequestParameters parameters = null)
        {   
            // build request
            var request = _builder.GetRequest(key, parameters);
            
            // call graph api
            var response = await _httpClient.SendAsync(request);
            
            // convert to string and return
            string jsonString = await response.Content.ReadAsStringAsync();

            return jsonString;
        }

        public static string GetRefreshToken()
        {
            if (refreshToken != null) return refreshToken;
            throw new NullReferenceException("Refresh-Token not initialized yet");
        }

    }
}