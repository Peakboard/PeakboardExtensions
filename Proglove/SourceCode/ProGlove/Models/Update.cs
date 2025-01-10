using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProGlove.Models
{
    public class Update
    {
        [JsonProperty("attaching_path")]
        public List<string> AttachingPath { get; set; }

        [JsonProperty("rule_ref")]
        public string RuleRef { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }
    }
}
