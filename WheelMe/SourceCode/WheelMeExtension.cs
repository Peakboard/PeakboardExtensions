using System;
using System.Net.Http;
using System.Threading.Tasks;
using Peakboard.ExtensionKit;
using WheelMe.DTO;

namespace WheelMe
{
    [ExtensionIcon("WheelMe.WheelMe.png")]
    public class WheelMeExtension : ExtensionBase
    {
        public WheelMeExtension(IExtensionHost host) : base(host) 
        {
        }
        
        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "WheelMe",
                Name = "Wheel.Me Extension",
                Description = "Get your data from Wheel.Me",
                Version = "1.0",
                Author = "Michelle Wu",
                Company = "Peakboard GmbH.",
                Copyright = "Copyright © Peakboard GmbH",
            };
        }

        protected override CustomListCollection GetCustomListsOverride()
        {
            return new CustomListCollection
            {
                new WheelMeFloorsCustomList(),
                new WheelMePositionsCustomList(),
                new WheelMeRobotsCustomList(),
            };
        }

        private static string MyLocalToken = string.Empty;

        public static HttpClient ProduceHttpClient(CustomListData data)
        {
            // NOTE: Base url need to end with '/'
            // https://www.rfc-editor.org/rfc/rfc3986
            var baseUrl = data.Properties["BaseURL"];
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                throw new ArgumentException("Base url cannot be empty", nameof(baseUrl));
            }
            
            var client = new HttpClient()
            {
                BaseAddress = new Uri(baseUrl)
            };

            return client;
        }
        
        public static async Task AuthenticateClientAsync(HttpClient client, string UserName, string Password) 
        {
            if (!string.IsNullOrEmpty(MyLocalToken) && !client.DefaultRequestHeaders.Contains("Authorization"))
            {
                // TODO: Check token expiration here
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + MyLocalToken);
                return;
            }

            var payload = new AuthenticationRequestDto
            {
                Email = UserName,
                Password = Password
            };

            var response = await client.PostRequestAsync<AuthenticationResponseDto>("api/public/authentication", payload);
            
            MyLocalToken = response.Token;
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + MyLocalToken);
        }
    }
}
