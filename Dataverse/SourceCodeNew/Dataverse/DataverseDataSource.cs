using System;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using Peakboard.ExtensionKit;
using System.Net.NetworkInformation;

namespace Dataverse
{
    [ExtensionIcon("Dataverse.Dataverse.png")]
    public class DataverseExtension : ExtensionBase
    {
        public DataverseExtension(IExtensionHost host) : base(host) 
        { 
        
        }
        
        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "Dataverse",
                Name = "Dataverse Extension",
                Description = "COnnect to MS Dataverse to read data from your tables.",
                Version = "2.0",
                MinVersion = "1.0",
                Author = "Patrick",
                Company = "Peakboard GmbH",
                Copyright = "Copyright © Peakboard GmbH",
            };
        }

        protected override CustomListCollection GetCustomListsOverride()
        {
            return new CustomListCollection
            {
                new DataverseEntityCustomList(),
            };
        }

        private static string MyLocalToken = string.Empty;


        public static void AuthenticateClient(HttpClient client, string BaseURL, string UserName, string Password) 
        {
            if (!string.IsNullOrEmpty(MyLocalToken) && !client.DefaultRequestHeaders.Contains("Authorization"))
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + MyLocalToken);
                return;
            }
            string json = $"{{ \"email\": \"{UserName}\", \"password\": \"{Password}\" }}";
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            HttpResponseMessage response = client.PostAsync(BaseURL + "api/public/authentication", content).Result;
            var responseBody = response.Content.ReadAsStringAsync().Result;

            if (response.IsSuccessStatusCode)
            {
                var rawdata = JObject.Parse(responseBody);
                MyLocalToken = rawdata["token"]?.ToString();
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + MyLocalToken);
            }
            else
            {
                throw new Exception("Error during authentification\r\n" + responseBody.ToString() + "\r\nOriginal Request: " + json);
            }
        }


    }
}
