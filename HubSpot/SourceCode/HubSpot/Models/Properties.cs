using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HubSpot.Models
{
    public class Properties
    {
        [JsonProperty("content")]
        public string Content { get; set; }
        [JsonProperty("createdate")]
        public DateTime Createdate { get; set; }
        [JsonProperty("hs_lastmodifieddate")]
        public DateTime Lastmodifieddate { get; set; }
        [JsonProperty("hs_object_id")]
        public long ObjectId { get; set; }
        [JsonProperty("hs_pipeline")]
        public int Pipeline { get; set; }
        [JsonProperty("hs_pipeline_stage")]
        public int PipekineStage { get; set; }
        [JsonProperty("hs_ticket_category")]
        public string TicketCategory { get; set; }
        [JsonProperty("hs_ticket_priority")]
        public string TicketPriority { get; set; }
        [JsonProperty("subject")]
        public string Subject { get; set; }
    }
}
