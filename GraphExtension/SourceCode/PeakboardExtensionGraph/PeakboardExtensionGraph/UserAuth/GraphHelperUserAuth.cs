using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Newtonsoft.Json;

namespace PeakboardExtensionGraph.UserAuth
{
    public class GraphHelperUserAuth : GraphHelperBase
    {
        
        private string _refreshToken;
        private const string AuthorizationUrl = "https://login.microsoftonline.com/{0}/oauth2/v2.0/devicecode";
        private string _allScopeAuthorizations;
        
        public GraphHelperUserAuth(string clientId, string tenantId, string scope)
        {
            ClientId = clientId;
            TenantId = tenantId;
            _allScopeAuthorizations = scope;
            
            HttpClient = new HttpClient();
        }
        
        public async Task InitGraph(Func<string, string, Task> prompt)
        {
            /* has to be called after initializing GraphHelper object */
            
            // authorize
            string deviceCode = await AuthorizeAsync(prompt);
            _ = deviceCode ?? throw new Exception("Authorization failed");
            
            // get tokens
            bool success = false;
            int requestAttempts = 0;
            Thread.Sleep(3000);
            while (!success && requestAttempts < 40)
            {
                // try to receive tokens 20 times
                success = await GetTokensAsync(deviceCode);
                Thread.Sleep(3000);
                requestAttempts++;
            }
            // abort if no success
            if (requestAttempts == 40) throw new Exception("Failed to receive tokens 40 times. Aborting...");

            // init request builder
            Builder = new RequestBuilder(AccessToken, "https://graph.microsoft.com/v1.0/me");
            
        }

        public async Task InitGraphWithRefreshToken(string token)
        {
            // Initialize via refresh token (in runtime)
            _refreshToken = token;
            HttpClient = new HttpClient();
            await RefreshAccessAsync();
            Builder = new RequestBuilder(AccessToken, "https://graph.microsoft.com/v1.0/me");
        }

        public void InitGraphWithAccessToken(string accessToken, string expiresIn, long millis, string refreshToken)
        {
            this.AccessToken = accessToken;
            this.TokenLifetime = expiresIn;
            this.Millis = millis;
            this._refreshToken = refreshToken;

            Builder = new RequestBuilder(AccessToken, "https://graph.microsoft.com/v1.0/me");
        }

        private async Task<string> AuthorizeAsync(Func<string, string, Task> prompt)
        {
            // generate url for http request
            string url = string.Format(AuthorizationUrl, TenantId);
            
            // generate body for http request
            var values = new Dictionary<string, string>
            {
                {"client_id", ClientId},
                {"scope", _allScopeAuthorizations}
            };
            
            FormUrlEncodedContent data = new FormUrlEncodedContent(values);
            
            // make http request to get device code for authentication
            HttpResponseMessage response = await HttpClient.PostAsync(url, data);
            string jsonString = await response.Content.ReadAsStringAsync();

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new MsGraphException($"Authorization failed.\n Status Code: {response.StatusCode}\n Error: {jsonString}");
            }
            
            var authorizationResponse = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);
            
            // get device code and authentication message
            string deviceCode;
            string uri;
            string userCode;
            if (authorizationResponse != null)
            {
                authorizationResponse.TryGetValue("device_code", out deviceCode);
                authorizationResponse.TryGetValue("verification_uri", out uri);
                authorizationResponse.TryGetValue("user_code", out userCode);
            }
            else
            {
                throw new Exception($"Authorization failed:\n {jsonString}");
            }
            
            // execute prompt
            await prompt(userCode, uri);
            
            // return device code for token request
            return deviceCode;
        }

        private async Task<bool> GetTokensAsync(string deviceCode)
        {
            // generate url for http request
            string url = string.Format(TokenEndpointUrl, TenantId);
            
            // generate body for http request
            var values = new Dictionary<string, string>
            {
                { "grant_type", "urn:ietf:params:oauth:grant-type:device_code" },
                { "client_id", ClientId },
                { "device_code", deviceCode }
            };
            
            FormUrlEncodedContent data = new FormUrlEncodedContent(values);
            
            // make http request to get access token and refresh token
            HttpResponseMessage response = await HttpClient.PostAsync(url, data);
            string jsonString = await response.Content.ReadAsStringAsync();
            
            // catch error if user didn't authenticate (yet)
            if (response.StatusCode != HttpStatusCode.OK) return false;

            try
            {
                var tokenResponse = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);

                if (tokenResponse != null)
                {
                    // store token values
                    tokenResponse.TryGetValue("refresh_token", out _refreshToken);
                    tokenResponse.TryGetValue("access_token", out AccessToken);
                    tokenResponse.TryGetValue("expires_in", out TokenLifetime);
                }
                else return false;

                Millis = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async Task RefreshAccessAsync()
        {
            // generate url for http request
            string url = String.Format(TokenEndpointUrl, TenantId);
            
            // generate body for http requestd
            var values = new Dictionary<string, string>
            {
                { "client_id", ClientId },
                { "grant_type", "refresh_token" },
                { "scope", _allScopeAuthorizations },
                { "refresh_token", _refreshToken }
            };

            var data = new FormUrlEncodedContent(values);
            
            // make http request to get new tokens
            HttpResponseMessage response = await HttpClient.PostAsync(url, data);
            string jsonString = await response.Content.ReadAsStringAsync();
            
            // check response for error codes
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new MsGraphException($"Failed to regain access.\n Status Code: {response.StatusCode}\n Error: {jsonString}");
            }
            
            var tokenResponse = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);

            // extract tokens from json response
            if (tokenResponse != null)
            {
                tokenResponse.TryGetValue("access_token", out AccessToken);
                tokenResponse.TryGetValue("refresh_token", out _refreshToken);
                tokenResponse.TryGetValue("expires_in", out TokenLifetime);
            }
            else
            {
                throw new Exception($"Failed to regain access:\n {jsonString}");
            }

            Builder?.RefreshToken(AccessToken);

            Millis = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        public async Task<bool> CheckIfTokenExpiredAsync()
        {
            // check if token expired
            long temp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            if ((temp - Millis) / 1000 > Int32.Parse(TokenLifetime))
            {
                // refresh tokens
                await RefreshAccessAsync();
                return true;
            }

            return false;
        }

        public async Task<bool> PostAsync(string url, string json)
        {
            // build request
            var request = Builder.PostRequest(url, json);
            
            // post request
            var response = await HttpClient.SendAsync(request);
            
            // return if post succeeded
            if (response.StatusCode != HttpStatusCode.Accepted) return false;
            else return true;
        }

        public string GetRefreshToken()
        {
            if (_refreshToken != null) return _refreshToken;
            throw new NullReferenceException("Refresh-Token not initialized yet");
        }

    }
}