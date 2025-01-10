using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProGlove.Models
{
    public class Metadata
    {
        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("size")]
        public int Size { get; set; }

        [JsonProperty("filters")]
        public List<Filter> Filters { get; set; }

        [JsonProperty("search")]
        public List<object> Search { get; set; }

        [JsonProperty("sort")]
        public List<Sort> Sort { get; set; }
    }
}
