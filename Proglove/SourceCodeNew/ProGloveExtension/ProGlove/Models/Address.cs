using Newtonsoft.Json;

namespace ProGlove.Models
{
    public class Address
    {
        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("district")]
        public string District { get; set; }

        [JsonProperty("latitude")]
        public double? Latitude { get; set; }

        [JsonProperty("longitude")]
        public double? Longitude { get; set; }

        [JsonProperty("postal_code")]
        public string PostalCode { get; set; }

        [JsonProperty("premise")]
        public string Premise { get; set; }

        [JsonProperty("region")]
        public string Region { get; set; }

        [JsonProperty("street")]
        public string Street { get; set; }
    }
}