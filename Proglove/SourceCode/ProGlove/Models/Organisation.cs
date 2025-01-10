using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
namespace ProGlove.Models
{
    public class Organisation
    {
        [JsonProperty("node")]
        public Node Node { get; set; }
    }
}
