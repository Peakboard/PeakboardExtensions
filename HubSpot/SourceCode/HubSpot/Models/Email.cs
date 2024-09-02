using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HubSpot.Models
{
    public class Email
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("properties")]
        public Properties Poperties { get; set; }
        [JsonProperty("createdAt")]
        public DateTime CreatedAt { get; set; }
        [JsonProperty("updatedAt")]
        public DateTime UpdatedAt { get; set; }
        [JsonProperty("archived")]
        public bool Archived { get; set; }
    }
}
