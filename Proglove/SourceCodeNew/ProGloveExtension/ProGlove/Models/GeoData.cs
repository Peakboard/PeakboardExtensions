using Newtonsoft.Json;
using System.Collections.Generic;


namespace ProGlove.Models
{
    public class GeoData
    {
        [JsonProperty("address")]
        public Address Address { get; set; }

        [JsonProperty("attaching_path")]
        public List<string> AttachingPath { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("timezone")]
        public string Timezone { get; set; }
    }
}
