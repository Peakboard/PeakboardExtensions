using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProGlove.Models
{
    public class LinksOfEndpoints
    {
        [JsonProperty("next")]
        public string Next { get; set; }
    }
}
