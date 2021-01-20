using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace PeakboardExtensionHue
{
    class HueHelper
    {
        public static string GetHueUser(string bridgeIp)
        {
            WebClient client = new WebClient();
            string resp = client.UploadString(string.Format("http://{0}/api", bridgeIp), "{\"devicetype\":\"Peakboard\"}");
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

        public static List<HueLight> GetLights(string bridgeIp, string userName)
        {
            WebClient client = new WebClient() { Encoding = Encoding.UTF8 };
            string resp = client.DownloadString(string.Format("http://{0}/api/{1}/lights", bridgeIp, userName));

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

                    if (mylight["state"]["bri"] != null)
                    {
                        hueLight.Brightness = Convert.ToInt32(mylight["state"]["bri"].ToString());
                    }
                    else
                    {
                        hueLight.Brightness = -1;
                    }

                    mylist.Add(hueLight);
                }
            }

            return mylist;
        }

        public static void SwitchLight(string bridgeIp, string userName, string lightName, bool switchOn)
        {
            List<HueLight> mylights = GetLights(bridgeIp, userName);
            string LightId = null;
            foreach (HueLight light in mylights)
            {
                if (light.Name.Equals(lightName))
                {
                    LightId = light.Id;
                }
            }

            if (LightId is null)
            {
                throw new InvalidOperationException(string.Format("Unknown light name '{0}'", lightName));
            }

            string url = string.Format("http://{0}/api/{1}/lights/{2}/state", bridgeIp, userName, LightId);
            string data = "{\"on\":" + (switchOn ? "true" : "false") + "}";
            WebClient client = new WebClient();
            string resp = client.UploadString(url, "Put", data);

            JObject dynobj = (JObject)JArray.Parse(resp)[0];
            if (dynobj.ContainsKey("error"))
            {
                throw new InvalidOperationException("Hue Bridge returned: " + dynobj["error"]["description"]);
            }

            Console.WriteLine(resp);
        }

        public static void SetLightBrightness(string bridgeIp, string userName, string lightName, int brightness)
        {
            List<HueLight> mylights = GetLights(bridgeIp, userName);
            string LightId = null;
            foreach (HueLight light in mylights)
            {
                if (light.Name.Equals(lightName))
                {
                    LightId = light.Id;
                }
            }

            if (LightId is null)
            {
                throw new InvalidOperationException(string.Format("Unknown light name '{0}'", lightName));
            }

            string url = string.Format("http://{0}/api/{1}/lights/{2}/state", bridgeIp, userName, LightId);
            string data;
            if (brightness > 0 && brightness <= 254)
            {
                data = "{\"on\":true, \"bri\":" + brightness.ToString() + "}";
            }
            else if (brightness == 0)
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

        public static void SetLightColor(string bridgeIp, string userName, string lightName, int color)
        {
            List<HueLight> mylights = GetLights(bridgeIp, userName);
            string LightId = null;
            foreach (HueLight light in mylights)
            {
                if (light.Name.Equals(lightName))
                {
                    LightId = light.Id;
                }
            }

            if (LightId is null)
            {
                throw new InvalidOperationException(string.Format("Unknown light name '{0}'", lightName));
            }

            string url = string.Format("http://{0}/api/{1}/lights/{2}/state", bridgeIp, userName, LightId);
            string data;
            if (color > 0 && color <= 65535)
            {
                data = "{\"on\":true, \"hue\":" + color.ToString() + "}";
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

        public static void Alert(string bridgeIp, string userName, string lightName)
        {
            List<HueLight> mylights = GetLights(bridgeIp, userName);
            string LightId = null;
            foreach (HueLight light in mylights)
            {
                if (light.Name.Equals(lightName))
                {
                    LightId = light.Id;
                }
            }

            if (LightId is null)
            {
                throw new InvalidOperationException(string.Format("Unknown light name '{0}'", lightName));
            }

            string url = string.Format("http://{0}/api/{1}/lights/{2}/state", bridgeIp, userName, LightId);
            string data = "{\"on\":true, \"alert\":\"lselect\"}";
            
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
