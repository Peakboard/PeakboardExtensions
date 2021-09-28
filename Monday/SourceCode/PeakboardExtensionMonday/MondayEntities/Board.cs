using Newtonsoft.Json;
using System.Collections.Generic;

namespace PeakboardExtensionMonday.MondayEntities
{
    public class Board
    {
        [JsonProperty(PropertyName = "id")]
        public int Id { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "items")]
        public IList<Item> Items { get; set; }

        [JsonProperty(PropertyName = "groups")]
        public IList<Group> Groups { get; set; }

    }
}
