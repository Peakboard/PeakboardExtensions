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
            {"Calendar", "/calendarview"},
            {"Contacts", "/contacts"},
            {"Mail", "/messages"},
            {"People", "/people"}
        };

        private GraphHelperUserAuth _graphHelper;

        private List<string> _selectAttributes;
        private List<string> _orderByAttributes;
        private string _customEntities = "";
        
        private string _chosenRequest = "";
        private string[] _chosenAttributes = { "" };
        private string[] _chosenOrder = { "" };
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
            
            // remove commas at the end
            if (select.Length > 1)
            {
                select = select.Remove(select.Length - 1);
            }
            
            if (orderBy.Length > 1)
            {
                orderBy = orderBy.Remove(orderBy.Length - 1);
            }

            if (CustomCallCheckBox.IsChecked == true)
            {
                customCall = CustomCallTextBox.Text;
            }

            return $"{ClientId.Text};{TenantId.Text};{Permissions.Text};{data};{select};{orderBy};{Filter.Text};{(ConsistencyBox.IsChecked == true ? "true" : "false")};" +
                   $"{Top.Text};{Skip.Text};{RefreshToken.Text};{_customEntities};{customCall}";
        }

        protected override void SetParameterOverride(string parameter)
        {

            if (String.IsNullOrEmpty(parameter)) return;
            
            var paramArr = parameter.Split(';');
            ClientId.Text = paramArr[0];
            TenantId.Text = paramArr[1];
            Permissions.Text = paramArr[2];
            
            if(!_uiInitialized)
            {
                _chosenRequest = paramArr[3];
                _chosenAttributes = paramArr[4].Split(',');
                _chosenOrder = paramArr[5].Split(',');

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
            }
            
            Filter.Text = paramArr[6];
            ConsistencyBox.IsChecked = (paramArr[7] == "true");
            Top.Text = paramArr[8];
            Skip.Text = paramArr[9];
            RefreshToken.Text = paramArr[10];
            _customEntities = paramArr[11];
            CustomCallTextBox.Text = paramArr[12];
        }

        private async void btnAuth_Click(object sender, RoutedEventArgs routedEventArgs)
        {
            await InitializeUi();
        }

        private async Task InitializeUi()
        {
            if (RefreshToken.Text != "")
            {
                // initialize with refresh token if possible
                try
                {
                    _graphHelper = new GraphHelperUserAuth(ClientId.Text, TenantId.Text, Permissions.Text);
                    await _graphHelper.InitGraphWithRefreshToken(RefreshToken.Text);
                }
                catch (Exception ex)
                {
                    // todo reset refresh token if not in ui anymore
                    MessageBox.Show(ex.Message);
                    return;
                }
            }
            else
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
                
            }
            this.RefreshToken.Text = _graphHelper.GetRefreshToken();

            string[] entities = _customEntities.Split(' ');

            // add saved custom entities into dictionary so they are added to the Request dropdown
            foreach (var entity in entities)
            {
                if (entity != "" && !_options.Values.Contains(entity))
                {
                    _options.Add(entity.Split(',')[0], entity.Split(',')[1]);
                }
            }
            
            if (RequestBox.Items.Count == 0)
            {
                // initialize combo boxes for graph calls & restore saved ui settings
                InitComboBoxes();
            }

            // enable UI components
            RequestBox.IsEnabled = true;
            CustomEntityText.IsEnabled = true;
            CustomEntityButton.IsEnabled = true;
            SelectList.IsEnabled = true;
            OrderList.IsEnabled = true;
            OrderByMode.IsEnabled = true;
            Top.IsEnabled = true;
            Skip.IsEnabled = true;
            CustomCallCheckBox.IsEnabled = true;
            Filter.IsEnabled = true;
            ConsistencyBox.IsEnabled = true;
            
            _uiInitialized = true;
        }

        private void RequestBox_SelectionChanged(object sender, SelectionChangedEventArgs selectionChangedEventArgs)
        {
            UpdateLists(((ComboBoxItem)this.RequestBox.SelectedItem).Tag.ToString());
        }

        private async void UpdateLists(string data)
        {
            // lock dropdowns
            SelectList.IsEnabled = false;
            OrderList.IsEnabled = false;
            RequestBox.IsEnabled = false;
            
            try
            {
                // make a graph call and update select & order by combo boxes
                var response = await _graphHelper.MakeGraphCall(data, new RequestParameters()
                {
                    Top = 1
                });
                // catch potential exception caused by graph call error
                // TODO: Add select all / none ?
                UpdateSelectList(response);
                UpdateOrderByList(response);
            }
            catch (Exception e)
            {
                // reset UI
                MessageBox.Show(e.Message);
                RequestBox.IsEnabled = true;
            }
            
            // unlock dropdowns
            SelectList.IsEnabled = true; 
            OrderList.IsEnabled = true;
            RequestBox.IsEnabled = true;
            
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
            
            // clear combo box and append every entry from the list into the combo box
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
            // Add every Dictionary entry to Request Combobox
            RequestBox.Items.Clear(); 

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
            
        }
        
        private void CustomCallCheckBox_Click(object sender, RoutedEventArgs e)
        {
            // checkbox to enable / disable custom api call
            if (CustomCallCheckBox.IsChecked == true)
            {
                CustomCallTextBox.IsEnabled = true;
                CustomCallCheckButton.IsEnabled = true;
                
                RequestBox.IsEnabled = false;
                SelectList.IsEnabled = false;
                OrderList.IsEnabled = false;
                Filter.IsEnabled = false;
                ConsistencyBox.IsEnabled = false;
                CustomEntityText.IsEnabled = false;
                CustomEntityButton.IsEnabled = false;
                OrderByMode.IsEnabled = false;
                Top.IsEnabled = false;
                Skip.IsEnabled = false;
            }
            else
            {
                CustomCallTextBox.IsEnabled = false;
                CustomCallCheckButton.IsEnabled = false;
                
                RequestBox.IsEnabled = true;
                Filter.IsEnabled = true;
                ConsistencyBox.IsEnabled = true;
                CustomEntityText.IsEnabled = true;
                CustomEntityButton.IsEnabled = true;
                OrderByMode.IsEnabled = true;
                Top.IsEnabled = true;
                Skip.IsEnabled = true;
            }
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
                MessageBox.Show($"Invalid call: {ex.Message}");
                return;
            }

            MessageBox.Show("Everything seems to be fine...");
        }

        private async void CustomEntityButton_OnClick(object sender, RoutedEventArgs e)
        {
            if(CustomEntityText.Text != "")
            {
                if (CustomEntityText.Text.Split(' ').Length != 2)
                {
                    MessageBox.Show("Invalid input.\n Expected:\"<Name> <Url-Suffix>\"");
                    return;
                }
                
                string name;
                string url;
                // check if entity exists ins Ms Graph
                try
                {
                    name = CustomEntityText.Text.Split(' ')[0];
                    url = CustomEntityText.Text.Split(' ')[1];
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

        private void AddEntity(string name, string url)
        {
            // check if entity already exists
            if (_options.ContainsKey(name))
            {
                MessageBox.Show("Name already exists");
            }
            else if (_options.ContainsValue(url))
            {
                MessageBox.Show("Entity already exists");
            }
            else
            {
                // add entity to Request Dropdown
                RequestBox.Items.Add(new ComboBoxItem()
                {
                    Content = name,
                    Tag = url,
                    IsSelected = true
                });
                _options.Add(name, url);
                _customEntities += $"{name},{url}";
                CustomEntityText.Text = "";
            }
        }
    }
    
}