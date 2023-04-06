using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
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
                // Azure App information
                $"{ClientId.Text};{TenantId.Text};{Permissions.Text};{_graphHelper.GetAccessToken()};" +
                $"{_graphHelper.GetExpirationTime()};{_graphHelper.GetMillis()};{_graphHelper.GetRefreshToken()};" +

                // Functions
                $"{funcNames};{funcUrls};{funcBodies}";
        }

        protected override void SetParameterOverride(string parameter)
        {
            ToggleUiComponents(false);
            
            if(String.IsNullOrEmpty(parameter)) return;

            string[] paramArr = parameter.Split(';');
            ClientId.Text = paramArr[0];
            TenantId.Text = paramArr[1];
            Permissions.Text = paramArr[2];
            
            string[] funcNames = paramArr[7].Split('|');
            string[] funcUrls = paramArr[8].Split('|');
            string[] jsonObjects = paramArr[9].Split('|');

            _functions = new Dictionary<string, Tuple<string,string>>();
            if (jsonObjects.Length == funcNames.Length && funcNames.Length == funcUrls.Length)
            {
                for (int i = 0; i < funcNames.Length; i++)
                {
                    _functions.Add(funcNames[i], new Tuple<string, string>(funcUrls[i], jsonObjects[i]));
                }
            }
            
            ReinitializeGraph(paramArr[0], paramArr[1], paramArr[2], paramArr[6]);
            UpdateListBox();
            ToggleUiComponents(true);
        }

        #region EventListener
        
        private void btnAuth_Click(object sender, RoutedEventArgs e)
        {
            Authenticate();
        }
        
        private void AddFunc_OnClick(object sender, RoutedEventArgs e)
        {
            string func = FuncName.Text;
            string url = FuncUrl.Text;
            string json = FuncBody.Text;

            if (String.IsNullOrEmpty(func) || String.IsNullOrEmpty(url) || String.IsNullOrEmpty(json))
            {
                ErrorMessage.Visibility = Visibility.Visible;
                return;
            }
            ErrorMessage.Visibility = Visibility.Hidden;

            func = func.Replace(';', '_');
            func = func.Replace('|', '_');
            
            url = url.Replace(';', '_');
            url = url.Replace('|', '_');
            
            _functions.Remove(func);
            
            _functions.Add(func, new Tuple<string, string>(url, json));
            
            UpdateListBox();
        }

        private void ShowFunc_OnClick(object sender, RoutedEventArgs e)
        {
            if (Functions.SelectedItem == null) return;
            
            string func = ((ListBoxItem)Functions.SelectedItem).Content.ToString();

            FuncName.Text = func;
            FuncUrl.Text = _functions[func].Item1;
            FuncBody.Text = _functions[func].Item2;

            UiComponents.SelectedIndex = 1;
        }

        private void RemoveFunc_OnClick(object sender, RoutedEventArgs e)
        {
            if (Functions.SelectedItem == null) return;
            
            string func = ((ListBoxItem)Functions.SelectedItem).Content.ToString() ?? "";

            _functions.Remove(func);
            UpdateListBox();
        }
        
        #endregion
        
        #region HelperMethods
        
        private async void ReinitializeGraph(string clientId, string tenantId, string scope, string token)
        {
            _graphHelper = new GraphHelperUserAuth(clientId, tenantId, scope);

            try
            {
                await _graphHelper.InitGraphWithRefreshToken(token);
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