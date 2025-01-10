using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
