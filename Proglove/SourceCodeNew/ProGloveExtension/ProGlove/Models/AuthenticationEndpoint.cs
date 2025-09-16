using Newtonsoft.Json;

namespace ProGlove.Models
{
    public class AuthenticationEndpoint
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("url")]
        public string Url { get; set; }
    }
}
