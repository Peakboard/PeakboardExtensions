using Newtonsoft.Json;

namespace ProGlove.Models
{
    //[Serializable]
    public class Sort
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
