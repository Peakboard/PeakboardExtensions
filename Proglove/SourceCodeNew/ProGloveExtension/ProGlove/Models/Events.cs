using Newtonsoft.Json;
using System.Collections.Generic;

namespace ProGlove.Models
{
    public class Events
    {
        [JsonProperty("items")]
        public List<Event> Items { get; set; }

        [JsonProperty("links")]
        public Links Links { get; set; }

        [JsonProperty("metadata")]
        public Metadata Metadata { get; set; }
    }
}
