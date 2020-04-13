using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Peakboard.ExtensionKit;
using System.Net;
using System.Globalization;
using System.Windows;
using Newtonsoft.Json;

namespace PeakboardExtensionAirportConditions
{
    [CustomListIcon("PeakboardExtensionAirportConditions.airplane.png")]
    [Serializable]
    class AirportConditionCustomList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            // Create a list definition
            return new CustomListDefinition
            {
                ID = $"AirportConditionCustomList",
                Name = "Aiport Weather",
                Description = "Returns basic weather data for a given airport",
                PropertyInputPossible = true,
            };
        }

        protected override FrameworkElement GetControlOverride()
        {
            // return an instance of the UI user control
            return new AirportConditionUIControl();
        }

        protected override void CheckDataOverride(CustomListData data)
        {
            // doing several checks with the Parameter that contains all out properties as JSon string

            if (string.IsNullOrWhiteSpace(data.Parameter))
            {
                throw new InvalidOperationException("Please use the map editor to select an airport");
            }

            Airport myAirport = JsonConvert.DeserializeObject<Airport>(data.Parameter);

            if (string.IsNullOrWhiteSpace(myAirport.AirportCode) || myAirport.AirportCode.Length != 4)
            {
                throw new InvalidOperationException("There's something with wrong with the airport code");
            }
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            // create a static collection of columns 

            return new CustomListColumnCollection
            {
                new CustomListColumn("AirportCode", CustomListColumnTypes.String),
                new CustomListColumn("ObservationTime", CustomListColumnTypes.String),
                new CustomListColumn("Temperature", CustomListColumnTypes.Number),
                new CustomListColumn("WindDirection", CustomListColumnTypes.Number),
                new CustomListColumn("WindSpeed", CustomListColumnTypes.Number),
                new CustomListColumn("AirPressure", CustomListColumnTypes.Number),
            };
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            // Downloading the weather data for the airport code
            Airport myAirport = JsonConvert.DeserializeObject<Airport>(data.Parameter);
            var xml = (new WebClient()).DownloadString(string.Format("https://www.aviationweather.gov/adds/dataserver_current/httpparam?dataSource=metars&requestType=retrieve&format=xml&hoursBeforeNow=3&mostRecent=true&stationString={0}", myAirport.AirportCode));
            
            var items = new CustomListObjectElementCollection();

            string ObservationTime = GetValueFromXML(xml, "observation_time");
            double Temperature = Convert.ToDouble(GetValueFromXML(xml, "temp_c"), CultureInfo.InvariantCulture);
            int WindDirection = Convert.ToInt32(GetValueFromXML(xml, "wind_dir_degrees"));
            int WindSpeed = Convert.ToInt32(GetValueFromXML(xml, "wind_speed_kt"));
            double AirPressure = Convert.ToDouble(GetValueFromXML(xml, "altim_in_hg"), CultureInfo.InvariantCulture);

            // Adding one single row to the output data set
            items.Add(new CustomListObjectElement { {"AirportCode", myAirport.AirportCode}, {"ObservationTime", ObservationTime}, {"Temperature", Temperature}, 
                { "WindDirection", WindDirection }, {"WindSpeed", WindSpeed},   {"AirPressure", AirPressure},});

            this.Log?.Info(string.Format("Airport condition extension fetched {0} rows.", items.Count));
            
            return items;
        }

        // Just a helper function to get a value from within an XML string
        public static string GetValueFromXML(string xml, string tag)
        {
            if (!xml.Contains("<" + tag + ">") || !xml.Contains("</" + tag + ">"))
                return "";

            string ret = xml.Substring(xml.IndexOf(tag) + 1 + tag.Length);
            ret = ret.Substring(0, ret.IndexOf(tag) - 2);
            return ret;
        }
    }
}
