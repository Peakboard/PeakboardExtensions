using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ProGlove.Models
{
    public class Config
    {
        [JsonProperty("status")]
        public string Status { get; set; }
    }
}
