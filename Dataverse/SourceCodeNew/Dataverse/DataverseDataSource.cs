using System;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using Peakboard.ExtensionKit;
using System.Net.NetworkInformation;
using Microsoft.PowerPlatform.Dataverse.Client;

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
                new DataverseFetchXMLCustomList(),
            };
        }

        private static string MyLocalToken = string.Empty;


        public static ServiceClient GetConnection(CustomListData data)
        {
            var dataverseUrl = data.Properties["DataverseURL"];
            var clientId = data.Properties["ClientId"];
            var clientSecret = data.Properties["ClientSecret"];
            var tenantId = data.Properties["TenantId"];

            var connectionString = $@"
                AuthType=ClientSecret;
                Url={dataverseUrl};
                ClientId={clientId};
                ClientSecret={clientSecret};
                Authority=https://login.microsoftonline.com/{tenantId};
                RequireNewInstance=true";

                var serviceClient = new ServiceClient(connectionString);

                if (serviceClient.IsReady)
                {
                    return serviceClient;
                }
                else
                {
                    throw new InvalidOperationException("Could not connect to Dataverse: " );  

                }
        }


    }
}
