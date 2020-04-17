using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PeakboardExtensionHue
{
    class HueHelper
    {
        public static string GetHueUser(string BridgeIp)
        {
            WebClient client = new WebClient();
            string resp = client.UploadString(string.Format("http://{0}/api", BridgeIp), "{\"devicetype\":\"Peakboard\"}");
            resp = resp.Substring(1);
            resp = resp.Substring(0, resp.Length - 1);

            JObject dynobj = JObject.Parse(resp);
            if (dynobj.ContainsKey("error"))
            {
                throw new InvalidOperationException("Hue Bridge returned: " + dynobj["error"]["description"]);
            }
            else if (dynobj.ContainsKey("success") && (dynobj["success"]["username"] != null))
            {
                return dynobj["success"]["username"].ToString();
            }
            else
            {
                throw new InvalidOperationException("Unknown answer from HUE: " + resp);
            }
        }

        public static List<HueLight> GetLights(string BridgeIp, string UserName)
        {
            WebClient client = new WebClient();
            string resp = client.DownloadString(string.Format("http://{0}/api/{1}/lights", BridgeIp, UserName));

            JObject dynobj = JObject.Parse(resp);
            List<HueLight> mylist = new List<HueLight>();

            for (int i = 1; i <= dynobj.Count; i++)
            {
                if (dynobj.ContainsKey(i.ToString()))
                {
                    JToken mylight = dynobj[i.ToString()];
                    HueLight hueLight = new HueLight();
                    hueLight.Id = i.ToString();
                    hueLight.Name = mylight["name"].ToString();
                    hueLight.Type = mylight["type"].ToString();
                    hueLight.ProductName = mylight["productname"].ToString();
                    hueLight.SwitchedOn = mylight["state"]["on"].ToString().Equals("True");
                    hueLight.Brightness = Convert.ToInt32(mylight["state"]["bri"].ToString());
                    mylist.Add(hueLight);
                }
            }

            return mylist;
        }

        public static void SwitchLight(string BridgeIp, string UserName, string LightName, bool SwitchOn)
        {
            List<HueLight> mylights = GetLights(BridgeIp, UserName);
            string LightId = null;
            foreach (HueLight light in mylights)
            {
                if (light.Name.Equals(LightName))
                {
                    LightId = light.Id;
                }
            }

            if (LightId is null)
            {
                throw new InvalidOperationException(string.Format("Unknown light name '{0}'", LightName));
            }

            string url = string.Format("http://{0}/api/{1}/lights/{2}/state", BridgeIp, UserName, LightId);
            string data = "{\"on\":" + (SwitchOn ? "true" : "false") + "}";
            WebClient client = new WebClient();
            string resp = client.UploadString(url, "Put", data);

            JObject dynobj = (JObject)JArray.Parse(resp)[0];
            if (dynobj.ContainsKey("error"))
            {
                throw new InvalidOperationException("Hue Bridge returned: " + dynobj["error"]["description"]);
            }

            Console.WriteLine(resp);
        }

        public static void SetLightsBrightness(string BridgeIp, string UserName, string LightName, int Brightness)
        {
            List<HueLight> mylights = GetLights(BridgeIp, UserName);
            string LightId = null;
            foreach (HueLight light in mylights)
            {
                if (light.Name.Equals(LightName))
                {
                    LightId = light.Id;
                }
            }

            if (LightId is null)
            {
                throw new InvalidOperationException(string.Format("Unknown light name '{0}'", LightName));
            }

            string url = string.Format("http://{0}/api/{1}/lights/{2}/state", BridgeIp, UserName, LightId);
            string data;
            if (Brightness > 0 && Brightness <= 254)
            {
                data = "{\"on\":true, \"bri\":" + Brightness.ToString() + "}";
            }
            else if (Brightness == 0)
            {
                data = "{\"on\":off, \"bri\":0}";
            }
            else
                throw new InvalidOperationException("invalid value for Brightness");
            WebClient client = new WebClient();
            string resp = client.UploadString(url, "Put", data);

            JObject dynobj = (JObject)JArray.Parse(resp)[0];
            if (dynobj.ContainsKey("error"))
            {
                throw new InvalidOperationException("Hue Bridge returned: " + dynobj["error"]["description"]);
            }
            
            Console.WriteLine(resp);
        }

        public class HueLight
        {
            public string Id;
            public string Name;
            public string Type;
            public bool SwitchedOn;
            public int Brightness;
            public string ProductName;
        }
    }
}
