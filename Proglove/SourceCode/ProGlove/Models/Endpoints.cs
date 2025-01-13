using Newtonsoft.Json;
using System.Collections.Generic;

namespace ProGlove.Models
{
    public class Endpoints
    {
        [JsonProperty("items")]
        public List<Endpoint> Endpoint { get; set; }

        [JsonProperty("links")]
        public LinksOfEndpoints Links { get; set; }

        [JsonProperty("metadata")]
        public MetadataOfEndpoint Metadata { get; set; }
    }
}
