using Newtonsoft.Json;

namespace ProGlove.Models
{
    public class Flags
    {
        [JsonProperty("config_names")]
        public bool ConfigNames { get; set; }

        [JsonProperty("inherited_policies")]
        public bool InheritedPolicies { get; set; }
    }
}
