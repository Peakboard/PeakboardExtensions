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
    public partial class GraphUIControl : CustomListUserControlBase
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
        public GraphUIControl()
        {
            InitializeComponent();
        }

        protected override string GetParameterOverride()
        {
            string data = ((ComboBoxItem)this.RequestBox.SelectedItem).Tag.ToString();
            string select = "";
            string orderBy = "";

            foreach (var item in SelectBox.Items)
            {
                var cboi = (CheckBox)item;
                if (cboi.IsChecked == true)
                {
                    select += $"{cboi.Content},";
                }
            }

            var count = 0;
            
            foreach (var orderItem in OrderByBox.Items)
            {
                var cboi = (CheckBox)orderItem;
                if (cboi.IsChecked == true)
                {
                    count++;
                    orderBy += $"{cboi.Content},";
                }
            }

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
                await GraphHelper.InitGraphWithRefreshToken(RefreshToken.Text, ClientId.Text, TenantId.Text,
                    Permissions.Text);
            }
            else
            {
                var paths = new[]
                {
                    @"C:\\Users\\Yannis\\Documents\\Peakboard\\Edge_Driver\\edgedriver_win64",
                    @"C:\Users\YannisHartmann\Documents\Graph\MS_Graph\Edge_Driver\edgedriver_win64"
                };
                EdgeDriver driver =
                    new EdgeDriver(paths[0]);
                await GraphHelper.InitGraph(ClientId.Text, TenantId.Text, Permissions.Text, (code, url) =>
                {
                    NavigateBrowser(driver, code, url);
                    return Task.FromResult(0);
                });
                this.RefreshToken.Text = GraphHelper.GetRefreshToken();
            }
            InitComboBoxes();
        }

        private async void RequestBox_SelectionChanged(object sender, SelectionChangedEventArgs selectionChangedEventArgs)
        {
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
            _selectAttributes = new List<string>();

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.PropertyName && !reader.Value.ToString().Contains("@odata"))
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
            _orderByAttributes = new List<string>();
            
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
            string lastName = "";
            bool value = false;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    lastName = reader.Value.ToString();
                    value = true;
                }
                else if (reader.TokenType == JsonToken.StartObject)
                {
                    value = false;
                    OrderByWalkThroughObject(reader, $"{objPrefix}/{lastName}");
                }
                else if (reader.TokenType == JsonToken.StartArray)
                {
                    value = false;
                    SkipArray(reader);
                }
                else if (reader.TokenType == JsonToken.EndObject)
                {
                    return;
                }
                else if (value)
                {
                    _orderByAttributes.Add($"{objPrefix}/{lastName}");
                }
            }
        }
        
        private void SkipArray(JsonReader reader)
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.StartArray)
                {
                    SkipArray(reader);
                }
                else if (reader.TokenType == JsonToken.EndArray)
                {
                    return;
                }
            }
        }

        private void SkipObject(JsonReader reader)
        {
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.StartObject)
                {
                    SkipObject(reader);
                }

                if (reader.TokenType == JsonToken.StartArray)
                {
                    SkipArray(reader);
                }
                else if (reader.TokenType == JsonToken.EndObject)
                {
                    return;
                }
            }
        }

    }
    
}