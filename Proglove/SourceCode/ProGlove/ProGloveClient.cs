using Newtonsoft.Json;
using ProGlove.Models;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ProGlove
{
    public class ProGloveClient
    {
        private readonly string basedUrl;
        private readonly string clientId;
        public ProGloveClient(string basedUrl, string clientId)
        {
            this.basedUrl = basedUrl;
            this.clientId = clientId;
        }
        public async Task<UserPoolClient> GetUserPoolClientIdAsync()
        {
            using (HttpClient client = new HttpClient())
            {
                var responce = await client.GetAsync(basedUrl + $"/auth-information?id={clientId}");
                if (responce.IsSuccessStatusCode)
                {
                    var responceBody = await responce.Content.ReadAsStringAsync();
                    var config = JsonConvert.DeserializeObject<UserPoolClient>(responceBody);
                    if (config != null) {
                        return config;
                    }
                }
                return null;
            }
        }
        public async Task<AuthenticationResponse> GetAuthenticationResponseAsync(string username, string password)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("X-Amz-Target", "AWSCognitoIdentityProviderService.InitiateAuth");
                var poolClient = await GetUserPoolClientIdAsync();
                if (poolClient != null)
                {
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
                    HttpResponseMessage response = await client.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        var res = JsonConvert.DeserializeObject<AuthenticationResponse>(responseBody);
                        return res;
                    }
                }
                return null;
            }
        }
        public async Task<Endpoints> GetEndpointsAsync(string token)
        {
            
            using (HttpClient client = new HttpClient())
            {
                string url = $"{basedUrl}/{clientId}/devices";
                client.DefaultRequestHeaders.Add("Authorization",token);
                var responce = await client.GetAsync(url);
                if (responce.IsSuccessStatusCode)
                {
                    var responceBody = await responce.Content.ReadAsStringAsync();
                    var items = JsonConvert.DeserializeObject<Endpoints>(responceBody);
                    return items;
                }
            }
            return null;
        }
        public async Task<GatewaysOrganisation> GetGatewaysOrganisationAsync(string token)
        {
            using (HttpClient client = new HttpClient())
            {
                string url = $"{basedUrl}/{clientId}/gateways/organisation";
                client.DefaultRequestHeaders.Add("Authorization", token);
                var responce = await client.GetAsync(url);
                if (responce.IsSuccessStatusCode)
                {
                    var responceBody = await responce.Content.ReadAsStringAsync();
                    var items = JsonConvert.DeserializeObject<GatewaysOrganisation>(responceBody);
                    return items;
                }
                return null;
            }
        }
        public async Task<Reports> GetReportsAsync(string token, string levelid)
        {
            using (HttpClient client = new HttpClient())
            {
                string url = $"{basedUrl}/{clientId}/reports?level_id={levelid}";
                client.DefaultRequestHeaders.Add("Authorization", token);
                var responce = await client.GetAsync(url);
                if (responce.IsSuccessStatusCode)
                {
                    var responceBody = await responce.Content.ReadAsStringAsync();
                    var items = JsonConvert.DeserializeObject<Reports>(responceBody);
                    return items;
                }
                return null;
            }
        }
        public async Task<Events> GetEventsAsync(string token)
        {
            using (HttpClient client = new HttpClient())
            {
                string url = $"{basedUrl}/{clientId}/events";
                client.DefaultRequestHeaders.Add("Authorization", token);
                var responce = await client.GetAsync(url);
                if (responce.IsSuccessStatusCode)
                {
                    var responceBody = await responce.Content.ReadAsStringAsync();
                    var items = JsonConvert.DeserializeObject<Events>(responceBody);
                    return items;
                }
                return null;
            }
        }
    }
}