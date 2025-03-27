using Newtonsoft.Json;
using ProGlove.Models;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ProGlove
{
    public class ProGloveClient : IDisposable
    {
        private readonly string _basedUrl;
        private readonly string _clientId;
        private readonly HttpClient _httpClient;

        public ProGloveClient(string basedUrl, string clientId)
        {
            _basedUrl = basedUrl ?? throw new ArgumentNullException(nameof(basedUrl));
            _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));

            if (_basedUrl.EndsWith("/"))
            {
                _basedUrl = _basedUrl.TrimEnd('/');
            }

            _httpClient = new HttpClient();
        }

        public async Task<UserPoolClient> GetUserPoolClientIdAsync()
        {
            var response = await _httpClient.GetAsync($"{_basedUrl}/auth-information?id={_clientId}");
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to get user pool client ID. Status code: {response.StatusCode}");
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            var config = JsonConvert.DeserializeObject<UserPoolClient>(responseBody);
            if (config == null)
            {
                throw new InvalidOperationException("Received empty or invalid user pool client data.");
            }

            return config;
        }

        public async Task<AuthenticationResponse> GetAuthenticationResponseAsync(string username, string password)
        {
            var poolClient = await GetUserPoolClientIdAsync();

            _httpClient.DefaultRequestHeaders.Remove("X-Amz-Target");
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("X-Amz-Target", "AWSCognitoIdentityProviderService.InitiateAuth");

            string url = $"https://cognito-idp.{poolClient.Region}.amazonaws.com/login";

            var requestData = new
            {
                AuthFlow = "USER_PASSWORD_AUTH",
                ClientId = poolClient.UserPoolClientId,
                AuthParameters = new
                {
                    USERNAME = username,
                    PASSWORD = password
                }
            };

            string json = JsonConvert.SerializeObject(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/x-amz-json-1.1");
            var response = await _httpClient.PostAsync(url, content);

            if (!response.IsSuccessStatusCode)
            {
                throw new UnauthorizedAccessException($"Authentication failed: INVALID USERNAME OR PASSWORD!. Status code: {response.StatusCode}");
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<AuthenticationResponse>(responseBody);

            if (result == null)
            {
                throw new InvalidOperationException("Received empty or invalid authentication response.");
            }

            return result;
        }

        public async Task<Endpoints> GetEndpointsAsync(string token)
        {
            string url = $"{_basedUrl}/{_clientId}/devices";

            SetAuthorizationHeader(token);

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to get endpoints. Status code: {response.StatusCode}");
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            var endpoints = JsonConvert.DeserializeObject<Endpoints>(responseBody);
            if (endpoints == null)
            {
                throw new InvalidOperationException("Received empty or invalid endpoints response.");
            }

            return endpoints;
        }

        public async Task<GatewaysOrganisation> GetGatewaysOrganisationAsync(string token)
        {
            string url = $"{_basedUrl}/{_clientId}/gateways/organisation";

            SetAuthorizationHeader(token);

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to get gateways organisation. Status code: {response.StatusCode}");
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<GatewaysOrganisation>(responseBody);

            if (result == null)
            {
                throw new InvalidOperationException("Received empty or invalid gateways organisation data.");
            }

            return result;
        }

        public async Task<Reports> GetReportsAsync(string token, string levelid)
        {
            string url = $"{_basedUrl}/{_clientId}/reports?level_id={levelid}";

            SetAuthorizationHeader(token);

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to get reports. Status code: {response.StatusCode}");
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<Reports>(responseBody);

            if (result == null)
            {
                throw new InvalidOperationException("Received empty or invalid reports data.");
            }

            return result;
        }

        public async Task<Events> GetEventsAsync(string token)
        {
            string url = $"{_basedUrl}/{_clientId}/events";

            SetAuthorizationHeader(token);

            var response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Failed to get events. Status code: {response.StatusCode}");
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<Events>(responseBody);

            if (result == null)
            {
                throw new InvalidOperationException("Received empty or invalid events data.");
            }

            return result;
        }

        private void SetAuthorizationHeader(string token)
        {
            _httpClient.DefaultRequestHeaders.Remove("Authorization");
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", token);
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
