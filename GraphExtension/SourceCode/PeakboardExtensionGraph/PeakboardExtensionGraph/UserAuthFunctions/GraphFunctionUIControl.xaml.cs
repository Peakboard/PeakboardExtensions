using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using Peakboard.ExtensionKit;
using PeakboardExtensionGraph.UserAuth;

namespace PeakboardExtensionGraph.UserAuthFunctions
{
    public partial class GraphFunctionUiControl : CustomListUserControlBase
    {
        private GraphHelperUserAuth _graphHelper;
        private string _refreshToken;
        
        public GraphFunctionUiControl()
        {
            InitializeComponent();
        }

        protected override string GetParameterOverride()
        {
            return 
                // Azure App information
                $"{ClientId.Text};{TenantId.Text};{Permissions.Text};{_graphHelper.GetAccessToken()};" +
                $"{_graphHelper.GetExpirationTime()};{_graphHelper.GetMillis()};{_graphHelper.GetRefreshToken()}";
        }

        private void btnAuth_Click(object sender, RoutedEventArgs e)
        {
            Authenticate();
        }

        private async void Authenticate()
        {
            try
            {
                _graphHelper = new GraphHelperUserAuth(ClientId.Text, TenantId.Text, Permissions.Text);
                await _graphHelper.InitGraph((code, url) =>
                {
                    // open web browser
                    Process.Start(url);
                    Clipboard.SetText(code);
                    return Task.FromResult(0);
                });
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return;
            }
            _refreshToken = _graphHelper.GetRefreshToken();
        }
    }
}