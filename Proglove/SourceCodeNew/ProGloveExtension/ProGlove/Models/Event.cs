using Newtonsoft.Json;
using System.Collections.Generic;

namespace ProGlove.Models
{
    public class Event
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("created")]
        public long Created { get; set; }

        [JsonProperty("device_battery")]
        public int DeviceBattery { get; set; }

        [JsonProperty("gateway_thing_name")]
        public string GatewayThingName { get; set; }

        [JsonProperty("gateway_name")]
        public string GatewayName { get; set; }

        [JsonProperty("gateway_firmware")]
        public string GatewayFirmware { get; set; }

        [JsonProperty("gateway_os_version")]
        public string GatewayOsVersion { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("organisation_names")]
        public List<string> OrganisationNames { get; set; }

        [JsonProperty("gateway_wifi_ssid")]
        public string GatewayWifiSsid { get; set; }

        [JsonProperty("gateway_wifi_bssid")]
        public string GatewayWifiBssid { get; set; }

        [JsonProperty("gateway_wifi_signal_strength")]
        public double GatewayWifiSignalStrength { get; set; }

        [JsonProperty("gateway_wifi_local_ipv4_address")]
        public string GatewayWifiLocalIpv4Address { get; set; }

        [JsonProperty("gateway_wifi_ap_ipv4_address")]
        public string GatewayWifiApIpv4Address { get; set; }

        [JsonProperty("gateway_wifi_local_ipv6_addresses")]
        public List<string> GatewayWifiLocalIpv6Addresses { get; set; }

        [JsonProperty("gateway_serial")]
        public string GatewaySerial { get; set; }
    }
}
