using Newtonsoft.Json;
using System.Collections.Generic;

namespace ProGlove.Models
{
    public class MetadataOfEndpoint
    {
        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("size")]
        public int Size { get; set; }

        [JsonProperty("filters")]
        public List<FilterOfEndpoint> Filters { get; set; }
    }
}
