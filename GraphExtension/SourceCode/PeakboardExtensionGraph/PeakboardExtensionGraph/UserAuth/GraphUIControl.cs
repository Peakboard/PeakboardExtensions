﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using Peakboard.ExtensionKit;
using PeakboardExtensionGraph.Settings;

namespace PeakboardExtensionGraph.UserAuth
{
    public partial class GraphUiControl : CustomListUserControlBase
    {
        private readonly Dictionary<string, string> _options = new Dictionary<string, string>
        {
            {"Me", "https://graph.microsoft.com/v1.0/me"},
            {"Events", "https://graph.microsoft.com/v1.0/me/events"},
            {"Contacts", "https://graph.microsoft.com/v1.0/me/contacts"},
            {"Messages", "https://graph.microsoft.com/v1.0/me/messages"},
            {"People", "https://graph.microsoft.com/v1.0/me/people"},
            {"TodoLists", "https://graph.microsoft.com/v1.0/me/todo/lists"}
        };

        private Dictionary<string, string> _customEntities = new Dictionary<string, string>();

        private GraphHelperUserAuth _graphHelper;

        private List<string> _selectAttributes;
        private List<string> _orderByAttributes;

        private string _chosenRequest = "https://graph.microsoft.com/v1.0/me";
        private string[] _chosenAttributes = { "" };
        private string[] _chosenOrder = { "" };

        private string _refreshToken = "";

        private bool _uiInitialized;

        public GraphUiControl()
        {
            InitializeComponent();
        }

        protected override string GetParameterOverride()
        {
            string data = ((ComboBoxItem)this.RequestBox.SelectedItem).Tag.ToString();

            string select = "";
            string orderBy = "";
            string customCall = "";
            string customEntities = "";

            // put each selected field into one comma separated string
            foreach (var item in SelectList.Items)
            {
                var lboi = (ListBoxItem)item;
                if (lboi.IsSelected)
                {
                    select += $"{lboi.Content},";
                }
            }
            
            // put each orderBy field into one comma separated string
            foreach (var orderItem in OrderList.Items)
            {
                var lboi = (ListBoxItem)orderItem;
                if (lboi.IsSelected)
                {
                    // add 'desc' if descending order is selected
                    if ((string)((ComboBoxItem)OrderByMode.SelectedItem).Content == "Desc")
                        orderBy += $"{lboi.Content} desc,";
                    else
                        orderBy += $"{lboi.Content},";
                }
            }
            
            // put each custom entity into one space separated string
            foreach (var entity in _customEntities)
            {
                customEntities += $"{entity.Key},{entity.Value} ";
            }

            // remove commas at the end
            if (select.Length > 1)
            {
                select = select.Remove(select.Length - 1);
            }
            
            if (orderBy.Length > 1)
            {
                orderBy = orderBy.Remove(orderBy.Length - 1);
            }
            
            if (customEntities.Length > 1)
            {
                customEntities = customEntities.Remove(customEntities.Length - 1);
            }

            if (CustomCallCheckBox.IsChecked == true)
            {
                // add custom call only if its checkbox is checked
                customCall = CustomCallTextBox.Text;
            }

            Int32.TryParse(Top.Text, out var top);
            Int32.TryParse(Skip.Text, out var skip);

            var settings = new UserAuthSettings()
            {
                ClientId = this.ClientId.Text,
                TenantId = this.TenantId.Text,
                Scope = this.Permissions.Text,

                RefreshToken = _graphHelper.GetRefreshToken() ?? "",
                AccessToken = _graphHelper.GetAccessToken() ?? "",
                ExpirationTime = _graphHelper.GetExpirationTime() ?? "0",
                Millis = _graphHelper.GetMillis(),
                
                EndpointUri = data,
                CustomCall = customCall,
                Parameters = new RequestParameters()
                {
                    Select = select,
                    OrderBy = orderBy,
                    Filter = this.Filter.Text,
                    ConsistencyLevelEventual = ConsistencyBox.IsChecked == true,
                    Top = top,
                    Skip = skip
                },
                CustomEntities = _customEntities
            };

            if (CustomCallCheckBox.IsChecked == true)
            {
                settings.RequestBody = this.RequestBodyTextBox.Text;
            }

            //var json = JsonConvert.SerializeObject(settings);
            var parameter = settings.GetParameterStringFromSettings();

            return parameter;
            /* Azure App Information: 0 - 6
            $"{ClientId.Text};{TenantId.Text};{Permissions.Text};{_graphHelper.GetAccessToken() ?? ""};" +
            $"{_graphHelper.GetExpirationTime() ?? "0"};{_graphHelper.GetMillis()};{_graphHelper.GetRefreshToken() ?? ""};" +
            
            // Query Information: 7 - 14
            $"{data};{select};{orderBy};{Filter.Text};{(ConsistencyBox.IsChecked == true ? "true" : "false")};" +
            $"{Top.Text};{Skip.Text};{customCall};" +
            
            // Only relevant for: UI 15
            $"{customEntities}";*/
        }

        protected override void SetParameterOverride(string parameter)
        {
            ToggleUiComponents(false);
            _uiInitialized = false;

            UserAuthSettings settings;
            
            try
            {
                settings = JsonConvert.DeserializeObject<UserAuthSettings>(parameter);
            }
            catch (JsonException)
            {
                settings = UserAuthSettings.GetSettingsFromParameterString(parameter);
            }
            catch (Exception)
            {
                settings = null;
            }

            if (String.IsNullOrEmpty(parameter) || settings == null)
            {
                // called when new instance of data source is created

                _graphHelper = null;
                
                _chosenRequest = "https://graph.microsoft.com/v1.0/me";
                _chosenAttributes = new [] { "" };
                _chosenOrder = new [] { "" };
                _customEntities = new Dictionary<string, string>();

                Filter.Text = "";
                ConsistencyBox.IsChecked = false;
                CustomCallCheckBox.IsChecked = false;
                Top.Text = "";
                Skip.Text = "";
                CustomCallTextBox.Text = "";
                RequestBodyTextBox.Text = "";
                
                return;
            }

            // called when instance is created already and saves need to be restored
            var paramArr = parameter.Split(';');
            settings.Validate();
            // init graph helper
            _graphHelper = new GraphHelperUserAuth(settings.ClientId, settings.TenantId, settings.Scope); 
            //new GraphHelperUserAuth(paramArr[0], paramArr[1], paramArr[2]);

            ClientId.Text = settings.ClientId; //paramArr[0];
            TenantId.Text = settings.TenantId; //paramArr[1];
            Permissions.Text = settings.Scope; //paramArr[2];

            _chosenRequest = settings.EndpointUri; //paramArr[7];
            _chosenAttributes = settings.Parameters.Select.Split(','); //paramArr[8].Split(',');
            _chosenOrder = settings.Parameters.OrderBy.Split(','); //paramArr[9].Split(',');

            Filter.Text = settings.Parameters.Filter; //paramArr[10];
            ConsistencyBox.IsChecked = settings.Parameters.ConsistencyLevelEventual; //(paramArr[11] == "true");
            Top.Text = settings.Parameters.Top.ToString(); //paramArr[12];
            Skip.Text = settings.Parameters.Skip.ToString(); //paramArr[13];
            CustomCallCheckBox.IsChecked = (settings.CustomCall != ""); //(paramArr[14] != "");
            CustomCallTextBox.Text = settings.CustomCall; //paramArr[14];
            RequestBodyTextBox.Text = settings.RequestBody ?? "";

            //var customEntities = paramArr[15];

            if (_chosenOrder.Length > 0 && !_chosenOrder[0].EndsWith("desc"))
            {
                // set sorting order to ascending
                ((ComboBoxItem)OrderByMode.Items[1]).IsSelected = true;
            }
            else
            {
                // remove ' desc' from orderBy attributes
                for (int i = 0; i < _chosenOrder.Length; i++)
                {
                    _chosenOrder[i] = _chosenOrder[i].Remove(_chosenOrder[i].Length - 5);
                }
            }
                
            // init custom entities dictionary
            _customEntities = settings.CustomEntities; //new Dictionary<string, string>();

            /*if(customEntities != ""){
                    string[] enitities = customEntities.Split(' ');
                    foreach (var entity in enitities)
                    {
                        _customEntities.Add(entity.Split(',')[0], entity.Split(',')[1]);
                    }
                }*/

            var restoreGraphConnection = RestoreGraphConnection(settings);
        }

        #region EventListener
        private void btnAuth_OnClick(object sender, RoutedEventArgs routedEventArgs)
        {
            var authTask = Authenticate();
        }
        
        private void RequestBox_SelectionChanged(object sender, SelectionChangedEventArgs selectionChangedEventArgs)
        {
            try
            {
                if(RequestBox?.SelectedItem != null && _uiInitialized)
                {
                    UpdateLists(((ComboBoxItem)this.RequestBox.SelectedItem).Tag.ToString());
                }
            }
            catch (Exception ex)
            {
                // catch potential exception caused by combobox
                MessageBox.Show(ex.Message);
                if (RequestBox != null) RequestBox.IsEnabled = true;
            }
        }
        
        private void CustomCallCheckBox_OnClick(object sender, RoutedEventArgs e)
        {
            ToggleCustomCall();
        }

        private async void CustomCallCheckButton_OnClick(object sender, RoutedEventArgs e)
        {
            // check if custom call works
            try
            {
                await _graphHelper.ExtractAsync(CustomCallTextBox.Text, RequestBodyTextBox.Text);
            }
            catch (Exception ex)
            {
                // catch exception and print message if the call contains error
                MessageBox.Show($"Invalid Request: {ex.Message}");
                return;
            }

            MessageBox.Show("Request is valid.");
        }

        private async void CustomEndpointButton_OnClick(object sender, RoutedEventArgs e)
        {
            if(CustomEnpointName.Text != "" && CustomEndpointUrl.Text != "")
            {
                string name = CustomEnpointName.Text;
                string url = CustomEndpointUrl.Text;

                // check if input only contains url suffix
                if (!url.StartsWith("https://graph.microsoft.com"))
                {
                    url = "https://graph.microsoft.com" + url;
                }
                
                // check if endpoint exists in Ms Graph api
                try
                {
                    await _graphHelper.ExtractAsync(url);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Invalid endpoint: {ex.Message}");
                    return;
                }
                AddEndpoint(name, url);
            }
        }
        
        private void RemoveEndpointButton_OnClick(object sender, RoutedEventArgs e)
        {
            RemoveEndpoint();
        }
        
        #endregion

        #region HelperMethods
        private async Task Authenticate()
        {
            ToggleUiComponents(false);
            _uiInitialized = false;
            
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
                
                // initialize combo boxes for graph calls & restore saved ui settings
                InitializeRequestDropdown();
                var response = await _graphHelper.ExtractAsync(_chosenRequest, new RequestParameters() { Top = 1 });
                UpdateSelectList(response);
                UpdateOrderByList(response);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                return;
            }
            _refreshToken = _graphHelper.GetRefreshToken();

            // enable UI components
            ToggleUiComponents(true);
            ToggleCustomCall();
            _uiInitialized = true;
        }
        
        private async void UpdateLists(string data)
        {
            // lock dropdowns
            TabControl.IsEnabled = false;
            RequestBox.IsEnabled = false;

            // make a graph call and update select & order by combo boxes
            try {
                var response = await _graphHelper.ExtractAsync(data, new RequestParameters()
                {
                    Top = 1
                });
                UpdateSelectList(response);
                UpdateOrderByList(response);
            }
            catch (Exception ex)
            {
                // catch potential exception caused by graph call error
                MessageBox.Show($"Error extracting Object fields: {ex.Message}");
                RequestBox.IsEnabled = true;
                
                // clear saved selections after error
                _chosenRequest = "";
                _chosenAttributes = new [] { "" };
                _chosenOrder = new [] { "" };
                return;
            }

            // unlock dropdowns
            
            TabControl.IsEnabled = true; 
            RequestBox.IsEnabled = true;

            // clear saved selections after they are set
            _chosenRequest = "";
            _chosenAttributes = new [] { "" };
            _chosenOrder = new [] { "" };
        }

        private void UpdateSelectList(GraphResponse response)
        {
            if (response.Type != GraphContentType.Json)
            {
                // return empty listbox if type is not json (e.g. CSV)
                SelectList.Items.Clear();
                return;
            }
            
            var reader = PreparedReader(response.Content);
            // delete old entries
            _selectAttributes = new List<string>();
            
            // read through json response and store every highest layer nested object into _selectAttributes
            // skip every nested object / array
            while (reader.Read())
            {
                if (reader.Value != null && reader.TokenType == JsonToken.PropertyName && !reader.Value.ToString().Contains("@odata"))
                {
                    _selectAttributes.Add(reader.Value.ToString());
                }
                else if (reader.TokenType == JsonToken.StartObject)
                {
                    JsonHelper.SkipObject(reader);
                }
                else if (reader.TokenType == JsonToken.StartArray)
                {
                    JsonHelper.SkipArray(reader);
                }
            }
            _selectAttributes.Sort();
            
            // clear combobox and append every entry from the list into the combobox
            SelectList.Items.Clear();
           
            foreach (var attr in _selectAttributes)
            {
                var lboi = new ListBoxItem()
                {
                    Content = attr,
                    // if attributes were saved before mark them as selected
                    IsSelected = _chosenAttributes.Contains(attr)
                };
                SelectList.Items.Add(lboi);
            }
        }

        private void UpdateOrderByList(GraphResponse response)
        {
            if (response.Type != GraphContentType.Json)
            {
                // return empty listbox if type is not json (e.g. CSV)
                OrderList.Items.Clear();
                return;
            }
            
            var reader = PreparedReader(response.Content);
            bool value = false;
            string lastname = "";
            
            // delete old entries
            _orderByAttributes = new List<string>();
            
            // read through json response and store every primitive property into _orderByAttributes
            // skip nested arrays & ignore objects
            // but walk through every nested object to access all their primitive properties
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    lastname = (reader.Value ?? "").ToString();
                    value = true;
                }
                else if (reader.TokenType == JsonToken.StartObject)
                {
                    value = false;
                    JsonHelper.OrderByWalkThroughObject(reader, lastname, _orderByAttributes);
                }
                else if (reader.TokenType == JsonToken.StartArray)
                {
                    value = false;
                    JsonHelper.SkipArray(reader);
                }
                else if (value && !lastname.Contains("@odata") && lastname != "")
                {
                    value = false;
                    _orderByAttributes.Add(lastname);
                }
                    
            }
            _orderByAttributes.Sort();
            
            // clear combo box and append every entry from the list into the combo box
            OrderList.Items.Clear();
            foreach (var attr in _orderByAttributes)
            {
                var lboi = new ListBoxItem()
                {
                    Content = attr,
                    // if attributes where saved before mark them as selected
                    IsSelected = _chosenOrder.Contains(attr)
                };
                OrderList.Items.Add(lboi);
            }
        }

        private JsonReader PreparedReader(string response)
        {
            // prepare reader for recursive walk through
            var reader = new JsonTextReader(new StringReader(response));
            bool prepared = false;

            while (reader.Read() && !prepared)
            {
                if (reader.TokenType == JsonToken.PropertyName && reader.Value?.ToString() == "value")
                {
                    // if json contains value array -> collection response with several objects
                    // parsing starts after the array starts
                    reader.Read();
                    reader.Read();
                    prepared = true;
                }
                else if (reader.TokenType == JsonToken.PropertyName && reader.Value?.ToString() == "error")
                {
                    // if json contains an error field -> deserialize to Error Object & throw exception
                    GraphHelperBase.DeserializeError(response);
                }
            }
            if(!prepared)
            {
                // no value array -> response contains single object which starts immediately
                reader = new JsonTextReader(new StringReader(response));
                reader.Read();
            }

            return reader;
        }
        
        private void InitializeRequestDropdown()
        {
            // Initialize or restore RequestDropdown
            // ListBoxes get their values automatically through SelectionChanged listener
            
            RequestBox.Items.Clear();

            // Add every Dictionary entry to Request Combobox
            foreach (var option in _options)
            {
                var boi = new ComboBoxItem()
                {
                    Content = option.Key,
                    Tag = option.Value,
                    IsSelected = option.Value == _chosenRequest
                };
                RequestBox.Items.Add(boi);
            }
            // add saved custom entities into Request Combobox
            foreach (var entity in _customEntities)
            {
                var boi = new ComboBoxItem()
                {
                    Content = entity.Key,
                    Tag = entity.Value,
                    IsSelected = entity.Value == _chosenRequest
                };
                RequestBox.Items.Add(boi);
            }
        }

        private void ToggleCustomCall()
        {
            // toggle Ui Components when Custom Call gets selected / deselected
            
            // checkbox to enable / disable custom api call
            if (CustomCallCheckBox.IsChecked == true)
            {
                CustomCallTextBox.IsEnabled = true;
                CustomCallCheckButton.IsEnabled = true;
                RequestBodyTextBox.IsEnabled = true;
                
                // disable ui components that are not available for custom call to prevent error
                RequestBox.IsEnabled = false;
                RemoveEntityButton.IsEnabled = false;
                TabControl.IsEnabled = false;
                Filter.IsEnabled = false;
                ConsistencyBox.IsEnabled = false;
                CustomEnpointName.IsEnabled = false;
                CustomEndpointUrl.IsEnabled = false;
                CustomEntityButton.IsEnabled = false;
                OrderByMode.IsEnabled = false;
                Top.IsEnabled = false;
                Skip.IsEnabled = false;
            }
            else
            {
                CustomCallTextBox.IsEnabled = false;
                CustomCallCheckButton.IsEnabled = false;
                RequestBodyTextBox.IsEnabled = false;
                
                // reenable ui components after custom call is deselected
                RequestBox.IsEnabled = true;
                RemoveEntityButton.IsEnabled = true;
                TabControl.IsEnabled = true;
                Filter.IsEnabled = true;
                ConsistencyBox.IsEnabled = true;
                CustomEnpointName.IsEnabled = true;
                CustomEndpointUrl.IsEnabled = true;
                CustomEntityButton.IsEnabled = true;
                OrderByMode.IsEnabled = true;
                Top.IsEnabled = true;
                Skip.IsEnabled = true;
            }
        }

        private void ToggleUiComponents(bool state)
        {
            // enable / disable all UI components except Azure App stuff
            
            RequestBox.IsEnabled = state;
            RemoveEntityButton.IsEnabled = state;
            CustomEnpointName.IsEnabled = state;
            CustomEndpointUrl.IsEnabled = state;
            CustomEntityButton.IsEnabled = state;
            TabControl.IsEnabled = state;
            OrderByMode.IsEnabled = state;
            Top.IsEnabled = state;
            Skip.IsEnabled = state;
            CustomCallCheckBox.IsEnabled = state;
            Filter.IsEnabled = state;
            ConsistencyBox.IsEnabled = state;
            CustomCallTextBox.IsEnabled = state;
            CustomCallCheckButton.IsEnabled = state;
            RequestBodyTextBox.IsEnabled = state;
        }
        
        private void AddEndpoint(string name, string url)
        {
            // check if endpoint already exists
            if (_options.ContainsKey(name) || _customEntities.ContainsKey(name))
            {
                MessageBox.Show("Name already exists");
            }
            else if (_options.ContainsValue(url) || _customEntities.ContainsValue(url))
            {
                MessageBox.Show("Entity already exists");
            }
            else
            {
                // replace prohibited characters
                name = name.Replace(',', '_');
                name = name.Replace(' ', '_');
                name = name.Replace(';', '_');
                
                // add entity to Request Dropdown
                RequestBox.Items.Add(new ComboBoxItem()
                {
                    Content = name,
                    Tag = url,
                    IsSelected = true
                });
                _customEntities.Add(name, url);
                CustomEnpointName.Text = "";
                CustomEndpointUrl.Text = "";
            }
        }

        private void RemoveEndpoint()
        {
            string key = ((ComboBoxItem)RequestBox.SelectedItem).Content.ToString();
            
            // check if item is custom
            if (_customEntities.Remove(key))
            {
                // remove item from combobox
                RequestBox.Items.Remove((ComboBoxItem)RequestBox.SelectedItem);
                ((ComboBoxItem)RequestBox.Items[0]).IsSelected = true;
            }
            
        }

        private async Task RestoreGraphConnection(UserAuthSettings settings)
        {
            // Set state of UI depending on state of Graph Connection

            // case 1: Existing datasource is restored & refresh token is still valid
            // Get a new access token via refresh token & restore ui configuration
            
            //string[] paramArr = parameter.Split(';');
            
            _graphHelper = new GraphHelperUserAuth(settings.ClientId, settings.TenantId, settings.Scope);
            _refreshToken = settings.RefreshToken;
            try
            {
                await _graphHelper.InitGraphWithRefreshTokenAsync(_refreshToken);
                
                // Initialize Dropdown & List boxes 
                InitializeRequestDropdown();
                var response = await _graphHelper.ExtractAsync(_chosenRequest, new RequestParameters() { Top = 1 });
                UpdateSelectList(response);
                UpdateOrderByList(response);
                
                ToggleUiComponents(true);
                ToggleCustomCall();
                
                _refreshToken = _graphHelper.GetRefreshToken();
                _uiInitialized = true;
            }
            // case 2: Existing datasource is restored & refresh token expired
            // Lock UI -> parameters cant be restored until access is granted
            // Wait for authentication through authenticate button
            catch (Exception)
            {
                ToggleUiComponents(false);
            }
            
        }
        
        #endregion

    }
    
}