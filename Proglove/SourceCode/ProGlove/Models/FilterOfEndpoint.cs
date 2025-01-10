using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProGlove.Models
{
    public class FilterOfEndpoint
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("value")]
        public long Value { get; set; }
    }
}
