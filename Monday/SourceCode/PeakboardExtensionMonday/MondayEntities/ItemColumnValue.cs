using Newtonsoft.Json;

namespace PeakboardExtensionMonday.MondayEntities
{
    public class ItemColumnValue
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }

        [JsonProperty(PropertyName = "text")]
        public dynamic Text { get; set; }
    }
}
