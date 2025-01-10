using Newtonsoft.Json;
using Peakboard.ExtensionKit;
using ProGlove;
using System;
namespace ProGloveExtension.CustomLists
{
    [Serializable]
    public class ProGloveExtensionEventsList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "CustomListEvents",
                Name = "EventsList",
                Description = "Add Events of Scanner",
                PropertyInputPossible = true,
                PropertyInputDefaults =
                {
                    new CustomListPropertyDefinition(){Name = "ClientId",Value = "7j8j5sl0"},
                    new CustomListPropertyDefinition(){Name = "BasedUrl",Value="https://d6xsb3jcd6.execute-api.us-east-1.amazonaws.com/latest"},
                    new CustomListPropertyDefinition(){Name = "Username",Value = "makhsum.yusupov@peakboard.com"},
                    new CustomListPropertyDefinition(){Name = "Password",Value = "M1020304050k."}
                }
            };
        }
        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            var columnsColl = new CustomListColumnCollection
            {
                new CustomListColumn("id", CustomListColumnTypes.String),
                new CustomListColumn("type", CustomListColumnTypes.String),
                new CustomListColumn("created", CustomListColumnTypes.Number),
                new CustomListColumn("device_battery", CustomListColumnTypes.Number),
                new CustomListColumn("gateway_thing_name", CustomListColumnTypes.String),
                new CustomListColumn("gateway_name", CustomListColumnTypes.String),
                new CustomListColumn("gateway_firmware", CustomListColumnTypes.String),
                new CustomListColumn("gateway_os_version", CustomListColumnTypes.String),
                new CustomListColumn("path", CustomListColumnTypes.String),
                new CustomListColumn("organisation_names", CustomListColumnTypes.String),
                new CustomListColumn("gateway_wifi_ssid", CustomListColumnTypes.String),
                new CustomListColumn("gateway_wifi_bssid", CustomListColumnTypes.String),
                new CustomListColumn("gateway_wifi_signal_strength", CustomListColumnTypes.Number),
                new CustomListColumn("gateway_wifi_local_ipv4_address", CustomListColumnTypes.String),
                new CustomListColumn("gateway_wifi_ap_ipv4_address", CustomListColumnTypes.String),
                new CustomListColumn("gateway_wifi_local_ipv6_addresses", CustomListColumnTypes.String),
                new CustomListColumn("gateway_serial", CustomListColumnTypes.String),
                new CustomListColumn("next", CustomListColumnTypes.String),
                new CustomListColumn("previous", CustomListColumnTypes.String),
                new CustomListColumn("description", CustomListColumnTypes.String),
                new CustomListColumn("size", CustomListColumnTypes.Number),
                new CustomListColumn("filters", CustomListColumnTypes.String),
                new CustomListColumn("search", CustomListColumnTypes.String),
                new CustomListColumn("sort", CustomListColumnTypes.String)
            };
            return columnsColl;
        }   
        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            ProGloveClient proGloveClient = new ProGloveClient(data.Properties["BasedUrl"], data.Properties["ClientId"]);
            var ath =  proGloveClient.GetAuthenticationResponseAsync(data.Properties["Username"], data.Properties["Password"]).Result;
            if (ath== null)
            {
                Log.Error("Incorrect authorization data");
                return new CustomListObjectElementCollection();
            }
            string token = ath.AuthenticationResult.IdToken;
            var events = proGloveClient.GetEventsAsync(ath.AuthenticationResult.IdToken).Result;
            if (events == null)
            {
                Log.Error("Incorrect authorization data");
                return new CustomListObjectElementCollection();
            }
            var items = new CustomListObjectElementCollection();
           
            CustomListObjectElement objectElement = null;
            foreach (var item in events.Items)
            {
                objectElement = new CustomListObjectElement();
                objectElement.Add("id", $"{item.Id}");
                objectElement.Add("type", $"{item.Type}");
                objectElement.Add("created", $"{item.Created}");
                objectElement.Add("device_battery", $"{item.DeviceBattery}");
                objectElement.Add("gateway_thing_name", $"{item.GatewayThingName}");
                objectElement.Add("gateway_name", $"{item.GatewayName}");
                objectElement.Add("gateway_firmware", $"{item.GatewayFirmware}");
                objectElement.Add("gateway_os_version", $"{item.GatewayOsVersion}");
                objectElement.Add("path", $"{item.Path}");
                objectElement.Add("organisation_names", $"{JsonConvert.SerializeObject(item.OrganisationNames)}");
                objectElement.Add("gateway_wifi_ssid", $"{item.GatewayWifiSsid}");
                objectElement.Add("gateway_wifi_bssid", $"{item.GatewayWifiBssid}");
                objectElement.Add("gateway_wifi_signal_strength", $"{item.GatewayWifiSignalStrength}");
                objectElement.Add("gateway_wifi_local_ipv4_address", $"{item.GatewayWifiLocalIpv4Address}");
                objectElement.Add("gateway_wifi_ap_ipv4_address", $"{item.GatewayWifiApIpv4Address}");
                objectElement.Add("gateway_wifi_local_ipv6_addresses", $"{JsonConvert.SerializeObject(item.GatewayWifiLocalIpv6Addresses)}");
                objectElement.Add("gateway_serial", $"{item.GatewaySerial}");
                objectElement.Add("next", events.Links.Next);
                objectElement.Add("previous", events.Links.Previous);
                objectElement.Add("description", events.Metadata.Description);
                objectElement.Add("size", events.Metadata.Size);
                objectElement.Add("filters", JsonConvert.SerializeObject(events.Metadata.Filters));
                objectElement.Add("search", JsonConvert.SerializeObject(events.Metadata.Search));
                objectElement.Add("sort", JsonConvert.SerializeObject(events.Metadata.Sort));
                items.Add(objectElement);
            }
            return items;
        }

    }
}