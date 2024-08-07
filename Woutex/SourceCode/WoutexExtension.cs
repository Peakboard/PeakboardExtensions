﻿using System;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using Peakboard.ExtensionKit;

namespace Woutex
{
    [ExtensionIcon("Woutex.Woutex.png")]
    public class WoutexExtension : ExtensionBase
    {
        public WoutexExtension(IExtensionHost host) : base(host) 
        { 
        
        }
        
        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "Woutex",
                Name = "Woutex e-ink display",
                Description = "Get your data from Woutex",
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
                new WoutexCustomList(),
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
