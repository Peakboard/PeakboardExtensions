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


namespace PeakboardExtensionGraph
{
    public partial class GraphUiControl : CustomListUserControlBase
    {
        private readonly Dictionary<string, string> _options = new Dictionary<string, string>
        {
            {"Calendar", "/calendarview"},
            {"Contacts", "/contacts"},
            {"Mail", "/messages"},
            {"People", "/people"}
        };

        private List<string> _selectAttributes;
        private List<string> _orderByAttributes;

        private string _chosenRequest = "";
        private string[] _chosenAttributes = { "" };
        private string[] _chosenOrder = { "" };
        private bool _uiInitialized = false;
        public GraphUiControl()
        {
            InitializeComponent();
        }

        protected override string GetParameterOverride()
        {
            string data;
            // request data is either custom link or selected dropdown entry
            if (CustomCallCheckBox.IsChecked == true)
            {
                data = CustomCallTextBox.Text;
            }
            else
            {
                data = ((ComboBoxItem)this.RequestBox.SelectedItem).Tag.ToString();
            }
            
            string select = "";
            string orderBy = "";
            
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

            return $"{ClientId.Text};{TenantId.Text};{Permissions.Text};{data};{select};{orderBy};{Filter.Text};{(ConsistencyBox.IsChecked == true ? "true" : "false")};" +
                   $"{Top.Text};{Skip.Text};{RefreshToken.Text}";
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
                // TODO
                _chosenRequest = paramArr[3];
                _chosenAttributes = paramArr[4].Split(',');
                _chosenOrder = paramArr[5].Split(',');
                _uiInitialized = true;
            }
            
            Filter.Text = paramArr[6];
            ConsistencyBox.IsChecked = (paramArr[7] == "true");
            Top.Text = paramArr[8];
            Skip.Text = paramArr[9];
            RefreshToken.Text = paramArr[10];
        }

        private async void btnAuth_Click(object sender, RoutedEventArgs routedEventArgs)
        {
            if (RefreshToken.Text != "")
            {
                // initialize with refresh token if possible
                await GraphHelper.InitGraphWithRefreshToken(RefreshToken.Text, ClientId.Text, TenantId.Text,
                    Permissions.Text);
            }
            else
            {
                try
                {
                    await GraphHelper.InitGraph(ClientId.Text, TenantId.Text, Permissions.Text, (code, url) =>
                    {
                        // open webbrowser
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
            this.RefreshToken.Text = GraphHelper.GetRefreshToken();
            
            if (RequestBox.Items.Count == 0)
            {
                // initialize combo boxes for graph calls
                InitComboBoxes();
            }
            
            // enable UI components
            RequestBox.IsEnabled = true;
            SelectList.IsEnabled = true;
            OrderList.IsEnabled = true;
            OrderByMode.IsEnabled = true;
            Top.IsEnabled = true;
            Skip.IsEnabled = true;
            CustomCallCheckBox.IsEnabled = true;
            Filter.IsEnabled = true;
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

            // make a graph call and update select & order by combo boxes
            var response = await GraphHelper.MakeGraphCall(data, new RequestParameters()
            {
                Top = 1
            });

            try
            {
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
                CustomCallCheckBox.IsChecked = false;
                CustomCallTextBox.IsEnabled = false;
                CustomCallCheckButton.IsEnabled = false;
            }
            
            // unlock dropdowns
            SelectList.IsEnabled = true;
            OrderList.IsEnabled = true;
            
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
                    SkipObject(reader);
                }
                else if (reader.TokenType == JsonToken.StartArray)
                {
                    SkipArray(reader);
                }
            }
            _selectAttributes.Sort();
            
            // clear combo box and append every entry from the list into the combo box
            SelectList.Items.Clear();
            foreach (var attr in _selectAttributes)
            {
                var lboi = new ListBoxItem()
                {
                    Content = attr + _chosenAttributes.Contains(attr),
                    // if attributes where saved before mark them as selected
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
                    OrderByWalkThroughObject(reader, lastname);
                }
                else if (reader.TokenType == JsonToken.StartArray)
                {
                    value = false;
                    SkipArray(reader);
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
                    GraphHelper.DeserializeError(response);
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
            RequestBox.Items.Clear(); // TODO: Find out why this is throwing exception

            if (_chosenRequest != "")
            {
                RestoreParameter();
            }
            else
            {
                RequestBox.Items.Add(new ComboBoxItem()
                {
                    Content = "Me",
                    Tag = "",
                    IsSelected = true
                });

                foreach (var option in _options)
                {
                    var boi = new ComboBoxItem()
                    {
                        Content = option.Key,
                        Tag = option.Value,
                    };
                    RequestBox.Items.Add(boi);
                }
            }
            
            _chosenAttributes = new string[]{""};
            _chosenOrder = new string[]{""};
            _chosenRequest = "";
        }

        private void RestoreParameter()
        {
            RequestBox.Items.Add(new ComboBoxItem()
            {
                Content = "Me",
                Tag = "",
                IsSelected = true
            });

            foreach (var option in _options)
            {
                var boi = new ComboBoxItem()
                {
                    Content = option.Key,
                    Tag = option.Value,
                };
                if (option.Value == _chosenRequest)
                {
                    boi.IsSelected = true;
                }
                RequestBox.Items.Add(boi);
            }

            if (!_options.ContainsValue(_chosenRequest))
            {
                CustomCallCheckBox.IsChecked = true;
                CustomCallTextBox.Text = _chosenRequest;
                UpdateLists(_chosenRequest);
                Filter.IsEnabled = true;
                ConsistencyBox.IsEnabled = true;
            }
            UpdateLists(_chosenRequest);
        }
        
        private void OrderByWalkThroughObject(JsonReader reader, string objPrefix)
        {
            // used to get every primitive property of a graph response
            string lastName = "";
            bool value = false;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    // store name of property and set value true
                    lastName = (string)reader.Value ?? "";
                    value = true;
                }
                else if (reader.TokenType == JsonToken.StartObject)
                {
                    // if object starts after value is set true
                    // -> property isn't primitive
                    // value is set false and object gets walked recursively
                    // prefix is modified to ensure correct designation for graph call
                    value = false;
                    OrderByWalkThroughObject(reader, $"{objPrefix}/{lastName}");
                }
                else if (reader.TokenType == JsonToken.StartArray)
                {
                    // if array starts after value is set true
                    // -> property isn't primitive
                    // value is set false and array gets skipped
                    value = false;
                    SkipArray(reader);
                }
                else if (reader.TokenType == JsonToken.EndObject)
                {
                    // nested object ends -> return to upper recursion layer
                    return;
                }
                else if (value)
                {
                    // if no array or object starts after value is set
                    // -> property is primitive
                    // property gets designated correctly and added to _orderByAttributes list
                    _orderByAttributes.Add($"{objPrefix}/{lastName}");
                    value = false;
                }
            }
        }
        
        private void SkipArray(JsonReader reader)
        {
            // skip nested array
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.StartArray)
                {
                    // nested arrays in nested array get skipped separately
                    SkipArray(reader);
                }
                else if (reader.TokenType == JsonToken.EndArray)
                {
                    // return to upper recursion layer
                    return;
                }
            }
        }

        private void SkipObject(JsonReader reader)
        {
            // skip nested object
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.StartObject)
                {
                    // nested objects in nested object get skipped separately
                    SkipObject(reader);
                }

                if (reader.TokenType == JsonToken.StartArray)
                {
                    // nested arrays in nested object get skipped separately
                    SkipArray(reader);
                }
                else if (reader.TokenType == JsonToken.EndObject)
                {
                    // return to upper recursion layer
                    return;
                }
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
                
                // disable until check button is clicked to prevent errors from choosing attributes of a previous request 
                SelectList.IsEnabled = false;
                OrderList.IsEnabled = false;
                Filter.IsEnabled = false;
                ConsistencyBox.IsEnabled = false;
            }
            else
            {
                CustomCallTextBox.IsEnabled = false;
                CustomCallCheckButton.IsEnabled = false;
                RequestBox.IsEnabled = true;
                Filter.IsEnabled = true;
                ConsistencyBox.IsEnabled = true;
                UpdateLists(((ComboBoxItem)this.RequestBox.SelectedItem).Tag.ToString());
            }
        }

        private void CustomCallCheckButton_Click(object sender, RoutedEventArgs e)
        {
            // button to update select & request combo boxes for custom api call
            UpdateLists(CustomCallTextBox.Text);
            Filter.IsEnabled = true;
            ConsistencyBox.IsEnabled = true;
        }
        
    }
    
}