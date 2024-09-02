using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HubSpot.Models
{
    public class Result
    {
        [JsonProperty("results")]
        public Email[] Results { get; set; }
    }
}
