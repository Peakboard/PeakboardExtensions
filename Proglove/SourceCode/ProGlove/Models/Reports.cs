using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProGlove.Models
{
    public class Reports
    {
        [JsonProperty("items")]
        public List<Report> Items { get; set; }

        [JsonProperty("links")]
        public Links Links { get; set; }

        [JsonProperty("metadata")]
        public Metadata Metadata { get; set; }
    }
}
