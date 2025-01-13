using System.Collections.Generic;
using Newtonsoft.Json;

namespace ProGlove.Models
{
    public class GatewaysOrganisation
    {
        [JsonProperty("items")]
        public List<Organisation> Items { get; set; }
    }
}
