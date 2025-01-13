using Newtonsoft.Json;
namespace ProGlove.Models
{
    public class Thumbnail
    {
        [JsonProperty("url")]
        public string Url { get; set; }

        [JsonProperty("attachment_sort_key")]
        public int AttachmentSortKey { get; set; }
    }
}
