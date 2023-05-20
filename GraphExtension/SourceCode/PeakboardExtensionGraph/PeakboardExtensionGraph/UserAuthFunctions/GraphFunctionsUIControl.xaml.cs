using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Peakboard.ExtensionKit;
using PeakboardExtensionGraph.UserAuth;

namespace PeakboardExtensionGraph.UserAuthFunctions
{
    public partial class GraphFunctionsUiControl : CustomListUserControlBase
    {
        private GraphHelperUserAuth _graphHelper;
        private Dictionary<string, Tuple<string,string>> _functions = new Dictionary<string, Tuple<string,string>>();

        public GraphFunctionsUiControl()
        {
            InitializeComponent();
        }

        protected override string GetParameterOverride()
        {
            string funcNames = "", funcUrls = "", funcBodies = "";
            
            // put func names urls and bodies into single string
            foreach (var key in _functions.Keys)
            {
                funcNames += $"{key}|";
                funcUrls += $"{_functions[key].Item1}|";
                funcBodies += $"{_functions[key].Item2}|";
            }

            if (funcNames.Length > 0) funcNames = funcNames.Remove(funcNames.Length - 1);
            if (funcUrls.Length > 0) funcUrls = funcUrls.Remove(funcUrls.Length - 1);
            if (funcBodies.Length > 0) funcBodies = funcBodies.Remove(funcBodies.Length - 1);

            return
                // Azure App information: 0 - 6
                $"{ClientId.Text};{TenantId.Text};{Permissions.Text};{_graphHelper.GetAccessToken()};" +
                $"{_graphHelper.GetExpirationTime()};{_graphHelper.GetMillis()};{_graphHelper.GetRefreshToken()};" +

                // Functions: 7 - 9
                $"{funcNames};{funcUrls};{funcBodies}";
        }

        protected override void SetParameterOverride(string parameter)
        {
            ToggleUiComponents(false);

            if (String.IsNullOrEmpty(parameter))
            {
                // clear UI if new instance is created
                _functions = new Dictionary<string, Tuple<string, string>>();
                FuncName.Text = "";
                FuncBody.Text = "";
                FuncUrl.Text = "";
                ErrorMessage.Visibility = Visibility.Hidden;
                UpdateListBox();
                return;
            }

            // get azure app information
            string[] paramArr = parameter.Split(';');
            ClientId.Text = paramArr[0];
            TenantId.Text = paramArr[1];
            Permissions.Text = paramArr[2];
            
            // split name, url & body substrings and put them into a dictionary
            string[] funcNames = paramArr[7].Split('|');
            string[] funcUrls = paramArr[8].Split('|');
            string[] jsonObjects = paramArr[9].Split('|');
            
            _functions = new Dictionary<string, Tuple<string,string>>();
            if (jsonObjects.Length == funcNames.Length && funcNames.Length == funcUrls.Length && !String.IsNullOrWhiteSpace(paramArr[7]))
            {
                for (int i = 0; i < funcNames.Length; i++)
                {
                    _functions.Add(funcNames[i], new Tuple<string, string>(funcUrls[i], jsonObjects[i]));
                }
            }
            else if(!String.IsNullOrWhiteSpace(paramArr[7]))
            {
                this.Log?.Verbose("Function arrays lengths vary. Parameter string might be corrupted. -> Removed all functions");
            }

            // try to restore graph connection
            RestoreGraphConnection(paramArr[0], paramArr[1], paramArr[2], paramArr[6]);
            UpdateListBox();
        }

        #region EventListener
        
        private void btnAuth_OnClick(object sender, RoutedEventArgs e)
        {
            Authenticate();
        }
        
        private void AddFunc_OnClick(object sender, RoutedEventArgs e)
        {
            string func = FuncName.Text;
            string url = FuncUrl.Text;
            string json = FuncBody.Text;

            // check if every input field is completed
            if (String.IsNullOrWhiteSpace(func) || String.IsNullOrWhiteSpace(url) || String.IsNullOrWhiteSpace(json))
            {
                ErrorMessage.Text = "Please complete all input fields";
                ErrorMessage.Visibility = Visibility.Visible;
                return;
            }
            ErrorMessage.Visibility = Visibility.Hidden;
            
            // replace forbidden characters in url
            url = url.Replace(';', '_');
            url = url.Replace('|', '_');

            if (!Regex.IsMatch(func, @"^[a-zA-Z0-9_]+$"))
            {
                ErrorMessage.Text = "Invalid character in Name field. Please only use letters, numbers and '_'";
                ErrorMessage.Visibility = Visibility.Visible;
                return;
            }
            
            // check if input only contains url suffix
            if (!url.StartsWith("https://graph.microsoft.com/v1.0/me"))
            {
                url = "https://graph.microsoft.com/v1.0/me" + url;
            }
            
            // remove function if already existed
            _functions.Remove(func);
            
            // add (newly created) function
            _functions.Add(func, new Tuple<string, string>(url, json));
            
            UpdateListBox();
        }

        private void ShowFunc_OnClick(object sender, RoutedEventArgs e)
        {
            // show the selected function
            
            if (Functions.SelectedItem == null) return;
            
            string func = ((ListBoxItem)Functions.SelectedItem).Content.ToString();

            FuncName.Text = func;
            FuncUrl.Text = _functions[func].Item1;
            FuncBody.Text = _functions[func].Item2;

            UiComponents.SelectedIndex = 1;
        }

        private void RemoveFunc_OnClick(object sender, RoutedEventArgs e)
        {
            // remove the selected function
            
            if (Functions.SelectedItem == null) return;
            
            string func = ((ListBoxItem)Functions.SelectedItem).Content.ToString() ?? "";

            _functions.Remove(func);
            UpdateListBox();
        }
        
        private void ClearButton_OnClick(object sender, RoutedEventArgs e)
        {
            ErrorMessage.Visibility = Visibility.Hidden;
            
            FuncUrl.Text = "";
            FuncName.Text = "";
            FuncBody.Text = "";
        }
        
        #endregion
        
        #region HelperMethods
        
        private async void RestoreGraphConnection(string clientId, string tenantId, string scope, string token)
        {
            _graphHelper = new GraphHelperUserAuth(clientId, tenantId, scope);
            
            // try to restore graph access if refresh token is not expired
            try
            {
                await _graphHelper.InitGraphWithRefreshTokenAsync(token);
                ToggleUiComponents(true);
            }
            catch (Exception)
            {
                ToggleUiComponents(false);
            }
        }
        
        private async void Authenticate()
        {
            try
            {
                _graphHelper = new GraphHelperUserAuth(ClientId.Text, TenantId.Text, Permissions.Text);
                await _graphHelper.InitGraphAsync((code, url) =>
                {
                    Clipboard.SetText(code);
                    
                    MessageBox.Show($"User code {code} copied to clipboard.");
                    
                    // open web browser
                    Process.Start(url);
                    return Task.FromResult(0);
                });
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return;
            }

            ToggleUiComponents(true);
        }

        private void UpdateListBox()
        {
            Functions.Items.Clear();

            foreach (var function in _functions.Keys)
            {
                Functions.Items.Add(new ListBoxItem()
                {
                    Content = function
                });
            }
        }

        private void ToggleUiComponents(bool state)
        {
            UiComponents.IsEnabled = state;
        }
        
        #endregion
        
    }
}