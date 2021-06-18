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
            string logicalName = "";
            string displayName = "";
            string chooseEntityOrView = "";
            string entityOrViewName = "";
            
            if(rbEntity.IsChecked==true)
            {
                chooseEntityOrView = "Entity";

                if (this.cboTable.Items.Contains((ComboBoxItem)this.cboTable.SelectedItem))
                {
                    entityOrViewName = ((ComboBoxItem)this.cboTable.SelectedItem).Tag.ToString();
                }
                else
                {
                    throw new InvalidOperationException("Please provide an Entity");
                }

                foreach (CheckBox item in this.columns.Items)
                {
                    if (item.IsChecked == true)
                    {
                        logicalName += item.Tag.ToString() + ",";
                        displayName += item.Content.ToString() + ",";
                    }
                }
                if (logicalName.Length != 0)
                {
                    logicalName = logicalName.Substring(0, logicalName.Length - 1);
                    displayName = displayName.Substring(0, displayName.Length - 1);
                }
                else
                {
                    throw new InvalidOperationException("Please select some Attributes/Columns!");
                }
            }
            else if (rbView.IsChecked == true)
            {
                chooseEntityOrView = "View";

                if(this.cboView.Items.Contains((ComboBoxItem)this.cboView.SelectedItem))
                {
                    entityOrViewName = ((ComboBoxItem)this.cboView.SelectedItem).Tag.ToString();
                }
                else
                {
                    throw new InvalidOperationException("Please provide a View");
                }

            }
            else
            {
                throw new InvalidOperationException("You must select an Entity or a View");
            }

            return $"{this.link.Text};{this.username.Text};{this.password.Password};{this.maxRows.Text};{entityOrViewName};{displayName};{logicalName};{chooseEntityOrView}";
        }

        protected override void SetParameterOverride(string parameter)
        {
            if(String.IsNullOrEmpty(parameter))
            {
                return;
            }

            this.link.Text = parameter.Split(';')[0];
            this.username.Text = parameter.Split(';')[1];
            this.password.Password = parameter.Split(';')[2];
            this.maxRows.Text = parameter.Split(';')[3];
        }

        protected override void ValidateParameterOverride()
        {

        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            this.cboTable.Items.Clear();
            this.cboView.Items.Clear();


            List<CrmName> tableList = new List<CrmName>();

            tableList = CrmHelper.GetTablesName(this.link.Text, this.username.Text, this.password.Password);

            if(tableList==null || tableList.Count == 0)
            {
                return;
            }
            else
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

            viewList = CrmHelper.GetViewsName(this.link.Text, this.username.Text, this.password.Password);

            if (viewList == null || viewList.Count == 0)
            {
                return;
            }
            else
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
        }

        private void rbEntity_Checked(object sender, RoutedEventArgs e)
        {
            cboTable.IsEnabled = true;
            btnTable.IsEnabled = true;
            columns.IsEnabled = true;
            cboView.IsEnabled = false;
        }

        private void rbView_Checked(object sender, RoutedEventArgs e)
        {
            cboView.IsEnabled = true;
            cboTable.IsEnabled = false;
            btnTable.IsEnabled = false;
            columns.IsEnabled = false;
        }

        private void btnTable_Click(object sender, RoutedEventArgs e)
        {
            this.columns.Items.Clear();

            List<CrmName> columns = CrmHelper.GetTableColumns(this.link.Text, this.username.Text, this.password.Password, ((ComboBoxItem)this.cboTable.SelectedItem).Tag.ToString());
            foreach (CrmName c in columns)
            {
                CheckBox cb = new CheckBox();
                cb.Content = c.displayName;
                cb.Tag = c.logicalName;
                this.columns.Items.Add(cb);
            }
        }
    }
}
