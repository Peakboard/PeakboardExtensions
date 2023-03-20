using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json;
using Peakboard.ExtensionKit;
using OpenQA.Selenium;
using OpenQA.Selenium.Edge;

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
        public GraphUiControl()
        {
            InitializeComponent();
        }

        protected override string GetParameterOverride()
        {
            string data;
            // request data is either custom link or selected dropdown entry
            if (CustomCheckBox.IsChecked == true)
            {
                data = CustomCall.Text;
            }
            else
            {
                data = ((ComboBoxItem)this.RequestBox.SelectedItem).Tag.ToString();
            }
            
            string select = "";
            string orderBy = "";
            
            // put each selected field into one comma separated string
            foreach (var item in SelectBox.Items)
            {
                var cboi = (CheckBox)item;
                if (cboi.IsChecked == true)
                {
                    select += $"{cboi.Content},";
                }
            }
            
            // put each orderBy field into one comma separated string
            foreach (var orderItem in OrderByBox.Items)
            {
                var cboi = (CheckBox)orderItem;
                if (cboi.IsChecked == true)
                {
                    // add 'desc' if descending order is selected
                    if ((string)OrderButton.Content == "Desc")
                        orderBy += $"{cboi.Content} desc,";
                    else
                        orderBy += $"{cboi.Content},";
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

            return $"{ClientId.Text};{TenantId.Text};{Permissions.Text};{data};{select};{orderBy};{Top.Text};{RefreshToken.Text}";
        }

        protected override void SetParameterOverride(string parameter)
        {
            if (String.IsNullOrEmpty(parameter)) return;
            
            var paramArr = parameter.Split(';');
            ClientId.Text = paramArr[0];
            TenantId.Text = paramArr[1];
            Permissions.Text = paramArr[2];
            //RequestData.Text = paramArr[3];
            //Select.Text = paramArr[4];
            //Orderby.Text = paramArr[5];
            Top.Text = paramArr[6];
            RefreshToken.Text = paramArr[7];
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
                // initialize with user code if no refresh token available
                var paths = new[]
                {
                    @"C:\\Users\\Yannis\\Documents\\Peakboard\\Edge_Driver\\edgedriver_win64",
                    @"C:\Users\YannisHartmann\Documents\Graph\MS_Graph\Edge_Driver\edgedriver_win64"
                };
                EdgeDriver driver =
                    new EdgeDriver(paths[0]);
                await GraphHelper.InitGraph(ClientId.Text, TenantId.Text, Permissions.Text, (code, url) =>
                {
                    // open webbrowser
                    NavigateBrowser(driver, code, url);
                    return Task.FromResult(0);
                });
            }
            this.RefreshToken.Text = GraphHelper.GetRefreshToken();
            
            if (RequestBox.Items.Count == 0)
            {
                // initialize combo boxes for graph calls
                InitComboBoxes();
            }
            
            // enable UI components
            RequestBox.IsEnabled = true;
            SelectBox.IsEnabled = true;
            OrderByBox.IsEnabled = true;
            OrderButton.IsEnabled = true;
            Top.IsEnabled = true;
            CustomCheckBox.IsEnabled = true;
        }

        private async void RequestBox_SelectionChanged(object sender, SelectionChangedEventArgs selectionChangedEventArgs)
        {
            // make a graph call and update select & order by combo boxes
            string data = ((ComboBoxItem)this.RequestBox.SelectedItem).Tag.ToString();
            var response = await GraphHelper.MakeGraphCall(data, new RequestParameters()
            {
                Top = 1
            });
            
            UpdateSelectBox(response);
            UpdateOrderByBox(response);
        }

        private void UpdateSelectBox(string response)
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
            SelectBox.Items.Clear();
            foreach (var attr in _selectAttributes)
            {
                var cboi = new CheckBox()
                {
                    Content = attr,
                    IsChecked = false
                };
                SelectBox.Items.Add(cboi);
            }
        }

        private void UpdateOrderByBox(string response)
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
            OrderByBox.Items.Clear();
            foreach (var attr in _orderByAttributes)
            {
                var cboi = new CheckBox()
                {
                    Content = attr,
                    IsChecked = false
                };
                OrderByBox.Items.Add(cboi);
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
                    reader.Read();
                    reader.Read();
                    prepared = true;
                }
            }
            if(!prepared)
            {
                reader = new JsonTextReader(new StringReader(response));
                reader.Read();
            }

            return reader;
        }

        private void NavigateBrowser(WebDriver driver, string code, string url)
        {
            // navigate to microsoft graph website
            driver.Navigate().GoToUrl(url);
    
            // input authentication code
            IWebElement textfield = driver.FindElement(By.Id("otc"));
            textfield.SendKeys(code);
        }

        private void InitComboBoxes()
        {
            // Add every Dictionary entry to Request Combobox
            RequestBox.Items.Clear();
            
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

        private void OrderButton_Click(object sender, RoutedEventArgs e)
        {
            // button to switch between ascending and descending sorting order
            if (OrderButton.IsChecked == true)
            {
                OrderButton.Content = "Desc";
            }
            else
            {
                OrderButton.Content = "Asc";
            }
        }

        private void RequestButton_Click(object sender, RoutedEventArgs e)
        {
            // checkbox to enable / disable custom api call
            if (CustomCheckBox.IsChecked == true)
            {
                CustomCall.IsEnabled = true;
                CustomCheckButton.IsEnabled = true;
                RequestBox.IsEnabled = false;
            }
            else
            {
                CustomCall.IsEnabled = false;
                CustomCheckButton.IsEnabled = false;
                RequestBox.IsEnabled = true;
            }
        }

        private async void CheckButton_Click(object sender, RoutedEventArgs e)
        {
            // button to update select & request combo boxes for custom api call
            string data = CustomCall.Text;
            var response = await GraphHelper.MakeGraphCall(data, new RequestParameters()
            {
                Top = 1
            });
            
            UpdateSelectBox(response);
            UpdateOrderByBox(response);
        }
    }
    
}