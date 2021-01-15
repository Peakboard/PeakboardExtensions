using Newtonsoft.Json;

namespace PeakboardExtensionHue
{
    class HueLightsCustomListStorage
    {
        public string BridgeIP { get; set; }
        public string UserName { get; set; }

        public static HueLightsCustomListStorage GetFromParameterString(string Parameter)
        {
            if (Parameter != null)
                return JsonConvert.DeserializeObject<HueLightsCustomListStorage>(Parameter);
            else
                return new HueLightsCustomListStorage();
        }
        public string GetParameterString()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
