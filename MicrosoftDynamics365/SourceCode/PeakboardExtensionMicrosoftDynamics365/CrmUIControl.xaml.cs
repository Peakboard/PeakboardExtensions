using Peakboard.ExtensionKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PeakboardExtensionMicrosoftDynamics365
{
    /// <summary>
    /// Interaction logic for CrmUIControl.xaml
    /// </summary>
    public partial class CrmUIControl : CustomListUserControlBase
    {

        public CrmUIControl()
        {
            InitializeComponent();
        }

        protected override string GetParameterOverride()
        {
            string logicalName = string.Empty;
            string displayName = string.Empty;
            string ExtractionType = string.Empty;
            string ObjectName = string.Empty;
            string username = string.Empty;
            string password = string.Empty;
            string clientid = string.Empty;
            string clientsecret = string.Empty;
            string fetchxml = string.Empty;

            if ((bool)this.rbUserPass.IsChecked)
            {
                username = this.username.Text;
                password = this.password.Password;
            }
            else
            {
                clientid = this.clientid.Text;
                clientsecret = this.secret.Text.Replace(";","SEMIKOLON");
            }


            if (rbEntity.IsChecked == true)
            {
                ExtractionType = "Entity";
                ObjectName = this.cboTable.Text;
            }
            else if (rbView.IsChecked == true)
            {
                ExtractionType = "View";
                ObjectName = this.cboView.Text;
            }

            return $"{this.link.Text};{username};{password};{this.maxRows.Text};{ObjectName};{displayName};{logicalName};{ExtractionType};{clientid};{clientsecret};{fetchxml}";
        }

        protected override void SetParameterOverride(string parameter)
        {
            if(String.IsNullOrEmpty(parameter))
            {
                this.link.Text = string.Empty;
                this.username.Text = string.Empty;
                this.password.Password = string.Empty;
                this.maxRows.Text = "50";
                this.clientid.Text = string.Empty;
                this.secret.Text = string.Empty;
                this.rbUserPass.IsChecked = true;
                this.cboTable.Text = string.Empty;
                this.cboView.Text = string.Empty;

                return;
            }

            var mysplits = parameter.Split(';');
            string URL = string.Empty;
            string username = string.Empty;
            string password = string.Empty;
            string maxRows = string.Empty; ;
            string ObjectName = string.Empty;
            string DisplayNameColumns = string.Empty;
            string LogicalNameColumns = string.Empty;
            string ExtractionType = string.Empty;
            string clientid = string.Empty;
            string clientsecret = string.Empty;
            string fetchxml = string.Empty;

            if (mysplits.Length >= 8)
            {
                this.link.Text = mysplits[0];
                this.username.Text = mysplits[1];
                this.password.Password = mysplits[2];
                this.maxRows.Text = mysplits[3];
                ObjectName = mysplits[4];
                DisplayNameColumns = mysplits[5];
                LogicalNameColumns = mysplits[6];
                ExtractionType = mysplits[7];
            }

            if (mysplits.Length >= 11)
            {
                this.clientid.Text = mysplits[8];
                this.secret.Text = mysplits[9];
                fetchxml = mysplits[10];
            }

            if (ExtractionType == "View")
            {
                this.rbView.IsChecked = true;
                this.cboView.Text = ObjectName;
            }
            else if (ExtractionType == "Entity")
            {
                this.rbEntity.IsChecked = true;
                this.cboTable.Text = ObjectName;
            }

            if (!string.IsNullOrWhiteSpace(this.clientid.Text) || !string.IsNullOrWhiteSpace(this.clientid.Text))
            {
                this.rbClientIDSecret.IsChecked = true;
                rbUserPass_Checked(null, null);
            }
            else
            {
                this.rbUserPass.IsChecked = true;
                rbUserPass_Checked(null, null);
            }
        }

        protected override void ValidateParameterOverride()
        {

        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            this.cboTable.Items.Clear();
            this.cboView.Items.Clear();

            try
            {
                List<CrmName> tableList = new List<CrmName>();

                tableList = CrmHelper.GetTablesName(this.link.Text, this.username.Text, this.password.Password, this.clientid.Text, this.secret.Text);

                if (tableList != null || tableList.Count != 0)
                {
                    foreach (CrmName table in tableList)
                    {
                        ComboBoxItem cboi = new ComboBoxItem
                        {
                            Content = table.displayName,
                            Tag = table.logicalName
                        };
                        this.cboTable.Items.Add(cboi);
                    }
                }
            
                List<CrmName> viewList = new List<CrmName>();

                viewList = CrmHelper.GetViewsName(this.link.Text, this.username.Text, this.password.Password, this.clientid.Text, this.secret.Text);

                if (viewList != null || viewList.Count != 0)
                {
                    foreach (CrmName view in viewList)
                    {
                        ComboBoxItem cboi = new ComboBoxItem
                        {
                            Content = view.displayName,
                            Tag = view.logicalName
                        };
                        this.cboView.Items.Add(cboi);
                    }
                }

                MessageBox.Show("Connection succesful! Select a View or an Entity.");
            }
            catch (Exception exception)
            {
                MessageBox.Show("Connection failed! Try again or change connections properties!\r\n(" + exception.Message +")");
            }
        }

        private void rbEntity_Checked(object sender, RoutedEventArgs e)
        {
            cboTable.IsEnabled = true;
            cboView.IsEnabled = false;
        }

        private void rbView_Checked(object sender, RoutedEventArgs e)
        {
            cboView.IsEnabled = true;
            cboTable.IsEnabled = false;
        }

        private void rbUserPass_Checked(object sender, RoutedEventArgs e)
        {
            try
            { 
                if ((bool)(rbClientIDSecret.IsChecked ?? false))
                {
                    this.clientid.IsEnabled = true;
                    this.secret.IsEnabled = true;
                    this.username.IsEnabled = false;
                    this.password.IsEnabled = false;
                }
                else
                {
                    this.clientid.IsEnabled = false;
                    this.secret.IsEnabled = false;
                    this.username.IsEnabled = true;
                    this.password.IsEnabled = true;

                }
            }
            catch (Exception)
            { }
        }

    }
}
