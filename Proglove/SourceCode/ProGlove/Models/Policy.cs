using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProGlove.Models
{
    public class Policy
    {
        [JsonProperty("config")]
        public Config Config { get; set; }

        [JsonProperty("flags")]
        public Flags Flags { get; set; }

        [JsonProperty("geo_data")]
        public GeoData GeoData { get; set; }

        [JsonProperty("update")]
        public Update Update { get; set; }
    }
}
