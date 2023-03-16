using System;
using System.Threading.Tasks;
using System.Windows;
using Peakboard.ExtensionKit;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;

namespace PeakboardExtensionGraph
{
    public partial class GraphUIControl : CustomListUserControlBase
    {
        public GraphUIControl()
        {
            InitializeComponent();
        }

        protected override string GetParameterOverride()
        {
            var selection = "";
            foreach (var item in Types.Items)
            {
                
            }
            
            return $"{ClientId.Text};{TenantId.Text};{Permissions.Text};{RequestData.Text};{Select.Text};{Orderby.Text};{Top.Text};{RefreshToken.Text}";
        }

        protected override void SetParameterOverride(string parameter)
        {
            if (String.IsNullOrEmpty(parameter)) return;
            
            var paramArr = parameter.Split(';');
            ClientId.Text = paramArr[0];
            TenantId.Text = paramArr[1];
            Permissions.Text = paramArr[2];
            RequestData.Text = paramArr[3];
            Select.Text = paramArr[4];
            Orderby.Text = paramArr[5];
            Top.Text = paramArr[6];
            RefreshToken.Text = paramArr[7];
        }

        private async void btnAuth_Click(object sender, RoutedEventArgs routedEventArgs)
        {
            EdgeDriver driver =
                new EdgeDriver(@"C:\Users\YannisHartmann\Documents\Graph\MS_Graph\Edge_Driver\edgedriver_win64");
            await GraphHelper.InitGraph(ClientId.Text, TenantId.Text, Permissions.Text, (code, url) =>
            {
                NavigateBrowser(driver, code, url);
                return Task.FromResult(0);
            });
            this.RefreshToken.Text = GraphHelper.GetRefreshToken();
        }

        private void NavigateBrowser(WebDriver driver, string code, string url)
        {
            // navigate to microsoft graph website
            driver.Navigate().GoToUrl(url);
    
            // input authentication code
            IWebElement textfield = driver.FindElement(By.Id("otc"));
            textfield.SendKeys(code);
        }

    }
}