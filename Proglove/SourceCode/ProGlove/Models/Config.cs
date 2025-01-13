using Newtonsoft.Json;

namespace ProGlove.Models
{
    public class Config
    {
        [JsonProperty("status")]
        public string Status { get; set; }
    }
}
