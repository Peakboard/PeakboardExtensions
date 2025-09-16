using Newtonsoft.Json;

namespace ProGlove.Models
{
    //[Serializable]
    public class Filter
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
