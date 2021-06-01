using Newtonsoft.Json;
using Peakboard.ExtensionKit;
using System;
using System.Windows;

namespace PeakboardExtensionHue
{
    public partial class HueUIControl : CustomListUserControlBase
    {
        public HueUIControl(ExtensionBase ext)
        {
            InitializeComponent();
        }

        protected override string GetParameterOverride()
        {
            return $"{this.HueBridgeIP.Text};{this.HueUserName.Text}";
        }

        protected override void SetParameterOverride(string parameter)
        {
            if (string.IsNullOrEmpty(parameter) || parameter.Split(';').Length != 2)
            {
                return;
            }

            this.HueBridgeIP.Text = parameter.Split(';')[0];
            this.HueUserName.Text = parameter.Split(';')[1];
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
