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
            return $"{clientID.Text};{tenantID.Text};{requestData.Text};{select.Text};{orderby.Text};{top.Text};{refreshToken.Text}";
        }

        protected override void SetParameterOverride(string parameter)
        {
            if (String.IsNullOrEmpty(parameter)) return;
            
            var paramArr = parameter.Split(';');
            clientID.Text = paramArr[0];
            tenantID.Text = paramArr[1];
            requestData.Text = paramArr[2];
            select.Text = paramArr[3];
            orderby.Text = paramArr[4];
            top.Text = paramArr[5];
            refreshToken.Text = paramArr[6];
        }

        private async void btnAuth_Click(object sender, RoutedEventArgs routedEventArgs)
        {
            var path = @"C:\Users\YannisHartmann\Documents\queries.json";
            EdgeDriver driver =
                new EdgeDriver(@"C:\Users\YannisHartmann\Documents\Graph\MS_Graph\Edge_Driver\edgedriver_win64");
            await GraphHelper.InitGraph(path, (code, url) =>
            {
                NavigateBrowser(driver, code, url);
                return Task.FromResult(0);
            });
            this.refreshToken.Text = GraphHelper.GetRefreshToken();
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