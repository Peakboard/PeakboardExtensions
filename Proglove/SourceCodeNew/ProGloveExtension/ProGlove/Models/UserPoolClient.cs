using Newtonsoft.Json;
using System.Collections.Generic;

namespace ProGlove.Models
{
    public class UserPoolClient
    {
        [JsonProperty("region")]
        public string Region { get; set; }

        [JsonProperty("customer_id")]
        public string CustomerId { get; set; }

        [JsonProperty("user_pool_client_id")]
        public string UserPoolClientId { get; set; }

        [JsonProperty("user_pool_id")]
        public string UserPoolId { get; set; }

        [JsonProperty("websocket_url")]
        public string WebsocketUrl { get; set; }

        [JsonProperty("mqtt_topic_prefix")]
        public string MqttTopicPrefix { get; set; }

        [JsonProperty("profile")]
        public string Profile { get; set; }

        [JsonProperty("authentication_endpoints")]
        public List<AuthenticationEndpoint> AuthenticationEndpoints { get; set; }

        [JsonProperty("customersimulator_url")]
        public string CustomerSimulatorUrl { get; set; }
    }
}
