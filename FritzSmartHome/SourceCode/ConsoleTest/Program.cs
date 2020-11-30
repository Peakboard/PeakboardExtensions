using AVMFritz;
using System.Globalization;
using System.Linq;

namespace ConsoleTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var fh = new FritzHelper();
            var thermostats = fh.GetThermostats("fritz.box", string.Empty, "XXXXXX");
            fh.SetThermostatTemperature("fritz.box", string.Empty, "XXXXXX", thermostats.First(t => t.Name == "Büro"), 20.5);
        }
    }
}