using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static PeakboardExtensionHue.HueHelper;

namespace HueFunctionPlaygroundConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            string IP = "192.168.0.178";
            // string User = GetHueUser("192.168.0.178");
            string User = "YVkVIpvQsh3HjW7adwoIP8VUYYkMYvOfI7hYrdQI";
            //List<HueLight> mylights = GetLights(IP, User);
            //SwitchLight(IP, User, "Strahler", true);
            SetLightsBrightness(IP, User, "Strahler", 100);
            Console.WriteLine(User);
            Console.ReadLine();
        }
    }
}
