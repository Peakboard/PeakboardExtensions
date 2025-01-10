using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
