using Newtonsoft.Json;
using System.Collections.Generic;


namespace ProGlove.Models
{
    public class Node
    {
        [JsonProperty("address")]
        public Address Address { get; set; }

        [JsonProperty("deleted")]
        public bool Deleted { get; set; }

        [JsonProperty("depth")]
        public int Depth { get; set; }

        [JsonProperty("entity_type")]
        public string EntityType { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("path")]
        public List<string> Path { get; set; }

        [JsonProperty("policy")]
        public Policy Policy { get; set; }

        [JsonProperty("parent_id")]
        public string ParentId { get; set; }

        [JsonProperty("actual_config")]
        public string ActualConfig { get; set; }

        [JsonProperty("ap_ipv4_address")]
        public string ApIpv4Address { get; set; }

        [JsonProperty("station")]
        public string Station { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("usecase")]
        public string UseCase { get; set; }
    }
}
