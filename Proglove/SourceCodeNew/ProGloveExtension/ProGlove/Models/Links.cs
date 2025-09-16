using Newtonsoft.Json;


namespace ProGlove.Models
{
    public class Links
    {
        [JsonProperty("next")]
        public object Next { get; set; }

        [JsonProperty("previous")]
        public object Previous { get; set; }
    }
}
