using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Identity.Client;
using Peakboard.ExtensionKit;

namespace MicrosoftGraph
{
    [ExtensionIcon("MicrosoftGraph.MicrosoftGraph.png")]
    public class MicrosoftGraphExtension : ExtensionBase
    {
        public MicrosoftGraphExtension() : base() { }
        public MicrosoftGraphExtension(IExtensionHost host) : base(host) { }

        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "MicrosoftGraph",
                Name = "Microsoft Graph Extension",
                Description = "Connect to the Microsoft Graph API to read data from your Microsoft 365 / Entra ID tenant.",
                Version = "1.3",
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
                new MicrosoftGraphUsersCustomList(),
                new MicrosoftGraphGroupsCustomList(),
                new MicrosoftGraphPlannerPlansCustomList(),
                new MicrosoftGraphPlannerBucketsCustomList(),
                new MicrosoftGraphPlannerTasksCustomList(),
            };
        }

        /// <summary>
        /// Acquires an app-only access token for Microsoft Graph using the client
        /// credentials flow (TenantId + ClientId + ClientSecret). The token is scoped
        /// to https://graph.microsoft.com/.default, which means the app gets every
        /// application permission the tenant admin has consented to.
        /// </summary>
        public static string AcquireGraphToken(CustomListData data)
        {
            var tenantId = data.Properties["TenantId"];
            var clientId = data.Properties["ClientId"];
            var clientSecret = data.Properties["ClientSecret"];

            var app = ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithClientSecret(clientSecret)
                .WithAuthority(new Uri($"https://login.microsoftonline.com/{tenantId}"))
                .Build();

            var scopes = new[] { "https://graph.microsoft.com/.default" };

            try
            {
                var result = app.AcquireTokenForClient(scopes).ExecuteAsync().GetAwaiter().GetResult();
                return result.AccessToken;
            }
            catch (MsalServiceException ex)
            {
                throw new InvalidOperationException(
                    $"Could not authenticate against Microsoft Graph. {ex.Message} " +
                    $"Verify TenantId, ClientId, ClientSecret and that admin consent has been granted for the required application permissions.",
                    ex);
            }
        }

        /// <summary>
        /// Returns an HttpClient pre-configured with a bearer token for Microsoft Graph.
        /// Caller is responsible for disposing the returned client.
        /// </summary>
        public static HttpClient CreateGraphClient(CustomListData data)
        {
            var token = AcquireGraphToken(data);
            var http = new HttpClient
            {
                BaseAddress = new Uri("https://graph.microsoft.com/v1.0/")
            };
            http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return http;
        }
    }
}
