using Newtonsoft.Json;

namespace PeakboardExtensionMonday.MondayEntities
{
    public class ItemGroup
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
    }
}
