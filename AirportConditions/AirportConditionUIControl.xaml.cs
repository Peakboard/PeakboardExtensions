using Newtonsoft.Json;
using Peakboard.ExtensionKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PeakboardExtensionAirportConditions
{
    public partial class AirportConditionUIControl : CustomListUserControlBase
    {
        public AirportConditionUIControl()
        {
            InitializeComponent();
        }

        

        protected override string GetParameterOverride()
        {
            Airport myAirport = new Airport();
            if (radEDDK.IsChecked ?? false)
                myAirport.AirportCode = "EDDK";
            else if (radEDDB.IsChecked ?? false)
                myAirport.AirportCode = "EDDB";
            else if (radEDDF.IsChecked ?? false)
                myAirport.AirportCode = "EDDF";
            else if (radEDDM.IsChecked ?? false)
                myAirport.AirportCode = "EDDM";
            else if (radEDDH.IsChecked ?? false)
                myAirport.AirportCode = "EDDH";
            else 
                myAirport.AirportCode = "EDDS";

            return JsonConvert.SerializeObject(myAirport);
        }

        protected override void SetParameterOverride(string parameter)
        {
            Airport myAirport = new Airport();
            if (parameter != null)
                myAirport = JsonConvert.DeserializeObject<Airport>(parameter);

            if (myAirport.AirportCode.Equals("EDDK"))
                this.radEDDK.IsChecked = true;
            else if (myAirport.AirportCode.Equals("EDDB"))
                this.radEDDB.IsChecked = true;
            else if (myAirport.AirportCode.Equals("EDDF"))
                this.radEDDF.IsChecked = true;
            else if (myAirport.AirportCode.Equals("EDDM"))
                this.radEDDM.IsChecked = true;
            else if (myAirport.AirportCode.Equals("EDDH"))
                this.radEDDH.IsChecked = true;
            else
                this.radEDDS.IsChecked = true;
        }
    }
}
