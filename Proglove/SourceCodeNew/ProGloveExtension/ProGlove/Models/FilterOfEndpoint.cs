using Newtonsoft.Json;

namespace ProGlove.Models
{
    public class FilterOfEndpoint
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("value")]
        public long Value { get; set; }
    }
}
