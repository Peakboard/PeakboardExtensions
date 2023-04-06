using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using Peakboard.ExtensionKit;


namespace PeakboardExtensionGraph.AppOnly
{
    public partial class GraphAppOnlyUiControl : CustomListUserControlBase
    {

        private readonly Dictionary<string, string> _options = new Dictionary<string, string>
        {
            { "Users", "/users" }
        };

        private Dictionary<string, string> _customEntities = new Dictionary<string, string>();
        
        private GraphHelperAppOnly _helper;
        
        private List<string> _orderByAttributes;
        private List<string> _selectAttributes;

        private string _chosenRequest = "/users";
        private string[] _chosenAttributes = { "" };
        private string[] _chosenOrder = { "" };
        

        public GraphAppOnlyUiControl()
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
            
            return 
                // Azure app information
                $"{ClientId.Text};{TenantId.Text};{Secret.Text};" +
                
                // Query information
                $"{data};{select};{orderBy};{Filter.Text};{(ConsistencyBox.IsChecked == true ? "true" : "false")};" +
                $"{Top.Text};{Skip.Text};{customCall};" +
                
                // only relevant for UI
                $"{customEntities}";
        }

        protected override void SetParameterOverride(string parameter)
        {
            if (String.IsNullOrEmpty(parameter))
            {
                // called when new instance of data source is created
                _chosenRequest = "/users";
                _chosenAttributes = new [] { "" };
                _chosenOrder = new [] { "" };
                _customEntities = new Dictionary<string, string>();

                Filter.Text = "";
                ConsistencyBox.IsChecked = false;
                CustomCallCheckBox.IsChecked = false;
                Top.Text = "";
                Skip.Text = "";
                CustomCallTextBox.Text = "";
                
                ToggleUiComponents(false);
            }
            else
            {
                string[] paramArr = parameter.Split(';');

                ClientId.Text = paramArr[0];
                TenantId.Text = paramArr[1];
                Secret.Text = paramArr[2];

                _chosenRequest = paramArr[3];
                _chosenAttributes = paramArr[4].Split(',');
                _chosenOrder = paramArr[5].Split(',');

                Filter.Text = paramArr[6];
                ConsistencyBox.IsChecked = (paramArr[7] == "true");
                Top.Text = paramArr[8];
                Skip.Text = paramArr[9];
                CustomCallCheckBox.IsChecked = (paramArr[10] != "");
                CustomCallTextBox.Text = paramArr[10];
                
                var customEntities = paramArr[11];

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
                _customEntities = new Dictionary<string, string>();

                if(customEntities != ""){
                    string[] enitities = customEntities.Split(' ');
                    foreach (var entity in enitities)
                    {
                        _customEntities.Add(entity.Split(',')[0], entity.Split(',')[1]);
                    }
                }
            }

            RestoreGraphConnection(parameter);
        }
        
        #region EventListener
        private async void ConnectButton_OnClick(object sender, RoutedEventArgs e)
        {
            var client = ClientId.Text;
            var tenant = TenantId.Text;
            var secret = Secret.Text;

            try
            {
                _helper = new GraphHelperAppOnly(client, tenant, secret);
                await _helper.InitGraph();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to connect to Graph: {ex.Message}");
                return;
            }

            InitializeRequestDropdown();
            // enable UI components
            ToggleUiComponents(true);
        }

        private void RequestBox_SelectionChanged(object sender, SelectionChangedEventArgs selectionChangedEventArgs)
        {
            try
            {
                if(RequestBox?.SelectedItem != null)
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
                await _helper.GetAsync(CustomCallTextBox.Text);
            }
            catch (Exception ex)
            {
                // catch exception and print message if the call contains error
                MessageBox.Show($"Invalid call: {ex.Message}");
                return;
            }

            MessageBox.Show("Everything seems to be fine...");
        }

        private async void CustomEntityButton_OnClick(object sender, RoutedEventArgs e)
        {
            if(CustomEntityName.Text != "" && CustomEntityUrl.Text != "")
            {
                
                string name = CustomEntityName.Text;
                string url = CustomEntityUrl.Text;
                // check if entity exists in Ms Graph
                try
                {
                    await _helper.GetAsync(url);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Invalid Entity: {ex.Message}");
                    return;
                }
                AddEntity(name, url);
            }
        }
        
        private void RemoveEntityButton_OnClick(object sender, RoutedEventArgs e)
        {
            RemoveEntity();
        }
        
        #endregion

        #region HelperMethods
        private async void UpdateLists(string data)
        {
            // lock dropdowns
            SelectList.IsEnabled = false;
            OrderList.IsEnabled = false;
            RequestBox.IsEnabled = false;

            // make a graph call and update select & order by combo boxes
            try {
                var response = await _helper.GetAsync(data, new RequestParameters()
                {
                    Top = 1
                });
                UpdateSelectList(response);
                UpdateOrderByList(response);
            }
            catch (Exception ex)
            {
                // catch potential exception caused by graph error
                MessageBox.Show(ex.Message);
                if (RequestBox != null) RequestBox.IsEnabled = true;
                
                // clear saved selections after error
                _chosenRequest = "";
                _chosenAttributes = new [] { "" };
                _chosenOrder = new [] { "" };
                return;
            }

            // unlock dropdowns
            SelectList.IsEnabled = true; 
            OrderList.IsEnabled = true;
            RequestBox.IsEnabled = true;
            
            // clear saved selections after they are set
            _chosenRequest = "";
            _chosenAttributes = new [] { "" };
            _chosenOrder = new [] { "" };
        }

        private void UpdateSelectList(string response)
        {
            var reader = PreparedReader(response);
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

        private void UpdateOrderByList(string response)
        {
            var reader = PreparedReader(response);
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
                
                // disable ui components that are not available for custom call to prevent error
                RequestBox.IsEnabled = false;
                RemoveEntityButton.IsEnabled = false;
                SelectList.IsEnabled = false;
                OrderList.IsEnabled = false;
                Filter.IsEnabled = false;
                ConsistencyBox.IsEnabled = false;
                CustomEntityName.IsEnabled = false;
                CustomEntityUrl.IsEnabled = false;
                CustomEntityButton.IsEnabled = false;
                OrderByMode.IsEnabled = false;
                Top.IsEnabled = false;
                Skip.IsEnabled = false;
            }
            else
            {
                CustomCallTextBox.IsEnabled = false;
                CustomCallCheckButton.IsEnabled = false;
                
                // reenable ui components after custom call is deselected
                RequestBox.IsEnabled = true;
                RemoveEntityButton.IsEnabled = true;
                SelectList.IsEnabled = true;
                OrderList.IsEnabled = true;
                Filter.IsEnabled = true;
                ConsistencyBox.IsEnabled = true;
                CustomEntityName.IsEnabled = true;
                CustomEntityUrl.IsEnabled = true;
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
            CustomEntityName.IsEnabled = state;
            CustomEntityUrl.IsEnabled = state;
            CustomEntityButton.IsEnabled = state;
            SelectList.IsEnabled = state;
            OrderList.IsEnabled = state;
            OrderByMode.IsEnabled = state;
            Top.IsEnabled = state;
            Skip.IsEnabled = state;
            CustomCallCheckBox.IsEnabled = state;
            Filter.IsEnabled = state;
            ConsistencyBox.IsEnabled = state;
        }

        private void AddEntity(string name, string url)
        {
            // check if entity already exists
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
                CustomEntityName.Text = "";
                CustomEntityUrl.Text = "";
            }
        }

        private void RemoveEntity()
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
        
        private async void RestoreGraphConnection(string parameter)
        {
            // Set state of UI depending on state of Graph Connection
            
            // case 1: New datasource is created -> Graph connection never existed
            // Do nothing -> there is nothing that can be restored
            if(String.IsNullOrEmpty(parameter)) return;

            string clientId = parameter.Split(';')[0];
            string tenantId = parameter.Split(';')[1];
            string secret = parameter.Split(';')[2];

            // case 2: Existing datasource is restored & client secret is still valid 
            // -> Initialize GraphHelper & restore ui configuration
            try
            {
                _helper = new GraphHelperAppOnly(clientId, tenantId, secret);
                await _helper.InitGraph();
                InitializeRequestDropdown();
                ToggleUiComponents(true);
                ToggleCustomCall();
            }
            // case 3: Existing datasource is restored & client secret expired
            // Lock UI -> parameters cant be restored until access is granted
            // Wait for connection through connect button
            catch (Exception)
            {
                ToggleUiComponents(false);
            }
        }
        
        #endregion
    }
}