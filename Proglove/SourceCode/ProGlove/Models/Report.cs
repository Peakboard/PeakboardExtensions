using Newtonsoft.Json;

namespace ProGlove.Models
{
    public class Report
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("report_type")]
        public string ReportType { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("time_created")]
        public long TimeCreated { get; set; }

        [JsonProperty("device_serial")]
        public string DeviceSerial { get; set; }

        [JsonProperty("photos_count")]
        public int PhotosCount { get; set; }

        [JsonProperty("path")]
        public string Path { get; set; }

        [JsonProperty("thumbnail")]
        public Thumbnail Thumbnail { get; set; }
    }
}
