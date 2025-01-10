using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
