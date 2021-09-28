using Peakboard.ExtensionKit;
using System;

namespace PeakboardExtensionMonday
{
    /// <summary>
    /// Interaction logic for MondayQueryingUIControl.xaml
    /// </summary>
    public partial class MondayQueryingUIControl : CustomListUserControlBase
    {
        public MondayQueryingUIControl()
        {
            InitializeComponent();
        }

        protected override string GetParameterOverride()
        {
            return $"{this.url.Text};{this.token.Text};{this.query.Text}";
        }

        protected override void SetParameterOverride(string parameter)
        {
            if (String.IsNullOrEmpty(parameter))
            {
                return;
            }

            this.url.Text = parameter.Split(';')[0];
            this.token.Text = parameter.Split(';')[1];
            this.query.Text = parameter.Split(';')[2];
        }

        protected override void ValidateParameterOverride()
        {

        }


    }
}
