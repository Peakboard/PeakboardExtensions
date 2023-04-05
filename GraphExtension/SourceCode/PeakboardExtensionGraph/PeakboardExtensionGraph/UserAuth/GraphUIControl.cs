using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using Peakboard.ExtensionKit;

namespace PeakboardExtensionGraph.UserAuth
{
    public partial class GraphUiControl : CustomListUserControlBase
    {
        private readonly Dictionary<string, string> _options = new Dictionary<string, string>
        {
            {"Me", ""},
            {"Calendar", "/events"},
            {"Contacts", "/contacts"},
            {"Mail", "/messages"},
            {"People", "/people"}
        };

        private Dictionary<string, string> _customEntities = new Dictionary<string, string>();

        private GraphHelperUserAuth _graphHelper;

        private List<string> _selectAttributes;
        private List<string> _orderByAttributes;

        private string _chosenRequest = "";
        private string[] _chosenAttributes = { "" };
        private string[] _chosenOrder = { "" };

        private string _refreshToken = "";

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

            return 
                // Azure App Information: 0 - 6
                $"{ClientId.Text};{TenantId.Text};{Permissions.Text};{_graphHelper.GetAccessToken()};" +
                $"{_graphHelper.GetExpirationTime()};{_graphHelper.GetMillis()};{_graphHelper.GetRefreshToken()};" +
                
                // Query Information: 7 - 16
                $"{data};{select};{orderBy};{Filter.Text};{(ConsistencyBox.IsChecked == true ? "true" : "false")};" +
                $"{Top.Text};{Skip.Text};{customCall};{PostRequestUrl.Text};{PostRequestBody.Text};" +
                
                // Only relevant for: UI 17
                $"{customEntities}";
        }

        protected override void SetParameterOverride(string parameter)
        {
            if (String.IsNullOrEmpty(parameter))
            {
                // called when new instance of data source is created

                _graphHelper = null;
                
                _chosenRequest = "";
                _chosenAttributes = new [] { "" };
                _chosenOrder = new [] { "" };
                _customEntities = new Dictionary<string, string>();

                Filter.Text = "";
                ConsistencyBox.IsChecked = false;
                CustomCallCheckBox.IsChecked = false;
                Top.Text = "";
                Skip.Text = "";
                CustomCallTextBox.Text = "";
                PostRequestUrl.Text = "";
                PostRequestBody.Text = "";
                
                ToggleUiComponents(false);
            }
            else
            {
                // called when instance is created already and saves need to be restored
                var paramArr = parameter.Split(';');
                
                // init graph helper
                _graphHelper = new GraphHelperUserAuth(paramArr[0], paramArr[1], paramArr[2]);

                ClientId.Text = paramArr[0];
                TenantId.Text = paramArr[1];
                Permissions.Text = paramArr[2];

                _chosenRequest = paramArr[7];
                _chosenAttributes = paramArr[8].Split(',');
                _chosenOrder = paramArr[9].Split(',');

                Filter.Text = paramArr[10];
                ConsistencyBox.IsChecked = (paramArr[11] == "true");
                Top.Text = paramArr[12];
                Skip.Text = paramArr[13];
                CustomCallCheckBox.IsChecked = (paramArr[14] != "");
                CustomCallTextBox.Text = paramArr[14];
                PostRequestUrl.Text = paramArr[15];
                PostRequestBody.Text = paramArr[16];
                
                var customEntities = paramArr[17];

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

            RestoreUi(parameter);
        }

        #region EventListener
        private async void btnAuth_Click(object sender, RoutedEventArgs routedEventArgs)
        {
            await InitializeUi();
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
                // catch potential exception caused by graph call error
                MessageBox.Show(ex.Message);
                if (RequestBox != null) RequestBox.IsEnabled = true;
            }
        }
        
        private void CustomCallCheckBox_Click(object sender, RoutedEventArgs e)
        {
            ToggleCustomCall();
        }

        private async void CustomCallCheckButton_Click(object sender, RoutedEventArgs e)
        {
            // check if custom call works
            try
            {
                await _graphHelper.MakeGraphCall(CustomCallTextBox.Text);
            }
            catch (Exception ex)
            {
                // catch exception and print message if the call contains error
                MessageBox.Show($"Invalid call: {ex.Message}");
                return;
            }

            MessageBox.Show("Everything seems to be fine...");
        }

        private async void CustomEntityButton_Click(object sender, RoutedEventArgs e)
        {
            if(CustomEntityName.Text != "" && CustomEntityUrl.Text != "")
            {
                string name = CustomEntityName.Text;
                string url = CustomEntityUrl.Text;
                // check if entity exists ins Ms Graph
                try
                {
                    await _graphHelper.MakeGraphCall(url);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Invalid Entity: {ex.Message}");
                    return;
                }
                AddEntity(name, url);
            }
        }
        
        private void RemoveEntityButton_OnClickEntityButton_OnClick(object sender, RoutedEventArgs e)
        {
            RemoveEntity();
        }
        
        #endregion

        #region HelperMethods
        private async Task InitializeUi()
        {
            ToggleUiComponents(false);
            
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

            // initialize combo boxes for graph calls & restore saved ui settings
            InitComboBoxes();
            // enable UI components
            ToggleUiComponents(true);
        }
        
        private async void UpdateLists(string data)
        {
            // lock dropdowns
            SelectList.IsEnabled = false;
            OrderList.IsEnabled = false;
            RequestBox.IsEnabled = false;

            // make a graph call and update select & order by combo boxes
            var response = await _graphHelper.MakeGraphCall(data, new RequestParameters()
            {
                Top = 1
            });
            UpdateSelectList(response);
            UpdateOrderByList(response);

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
        
        private void InitComboBoxes()
        {
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
            
            // todo add post request components here and disable listboxes at beginning in xaml
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

        private async void RestoreUi(string parameter)
        {
            // case 1: New datasource is created
            // Do nothing -> there are no parameters that can be restored
            if (String.IsNullOrEmpty(parameter)) return;

            // case 2: Existing datasource is restored & refresh token is still valid
            // Get a new access token via refresh token & restore parameters
            string[] paramArr = parameter.Split(';');
            
            _graphHelper = new GraphHelperUserAuth(paramArr[0], paramArr[1], paramArr[2]);
            _refreshToken = paramArr[6];
            try
            {
                await _graphHelper.InitGraphWithRefreshToken(_refreshToken);
                InitComboBoxes();
                ToggleUiComponents(true);
                ToggleCustomCall();
                _refreshToken = _graphHelper.GetRefreshToken();
            }
            // case 3: Existing datasource is restored & refresh token expired
            // Lock UI -> parameters cant be restored until access is granted
            // Wait for authentication via Authenticate Button
            catch (Exception)
            {
                ToggleUiComponents(false);
            }
        }
        
        #endregion

    }
    
}