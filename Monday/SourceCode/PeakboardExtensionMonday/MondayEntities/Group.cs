using Newtonsoft.Json;
using System.Collections.Generic;

namespace PeakboardExtensionMonday.MondayEntities
{
    public class Group
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        [JsonProperty(PropertyName = "items")]
        public IList<Item> Items { get; set; }
    }
}
