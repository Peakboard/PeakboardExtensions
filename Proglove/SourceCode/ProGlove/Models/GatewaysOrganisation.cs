using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace ProGlove.Models
{
    public class GatewaysOrganisation
    {
        [JsonProperty("items")]
        public List<Organisation> Items { get; set; }
    }
}
