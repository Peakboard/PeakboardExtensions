
using Newtonsoft.Json;
namespace ProGlove.Models
{
    public class Organisation
    {
        [JsonProperty("node")]
        public Node Node { get; set; }
    }
}
