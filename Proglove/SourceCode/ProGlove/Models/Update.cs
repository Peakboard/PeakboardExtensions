using Newtonsoft.Json;
using System.Collections.Generic;


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
