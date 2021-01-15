using Peakboard.ExtensionKit;
using System;
using System.Windows;

namespace PeakboardExtensionHue
{
    public partial class HueUIControl : CustomListUserControlBase
    {
        public HueUIControl()
        {
            InitializeComponent();
        }

        protected override string GetParameterOverride()
        {
            HueLightsCustomListStorage mystorage = new HueLightsCustomListStorage();
            mystorage.BridgeIP = this.HueBridgeIP.Text;
            mystorage.UserName = this.HueUserName.Text;
            return mystorage.GetParameterString();
        }

        protected override void SetParameterOverride(string parameter)
        {
            HueLightsCustomListStorage mystorage = HueLightsCustomListStorage.GetFromParameterString(parameter);
            this.HueBridgeIP.Text = mystorage.BridgeIP;
            this.HueUserName.Text = mystorage.UserName;
        }

        protected override void ValidateParameterOverride()
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.HueUserName.Text = HueHelper.GetHueUser(this.HueBridgeIP.Text);
            }
            catch (Exception e1)
            {
                MessageBox.Show(e1.Message);
            }
        }
    }
}
