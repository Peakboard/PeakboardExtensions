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

namespace PeakboardExtensionODataV4
{
	/// <summary>
	/// Interaction logic for ODataV4DataByEntityUIControl.xaml
	/// </summary>
	public partial class ODataV4EntityControl : CustomListUserControlBase
	{
		public ODataV4EntityControl()
		{
			InitializeComponent();
			cboAuthentication.SelectedIndex = 0;
			cboQueryOptionType.SelectedIndex = 0;
		}

		protected override string GetParameterOverride()
		{
			string entitySet = string.Empty;
			string entityProperties = string.Empty;
            string authentication;
			string queryOptionValue;

			if (this.cboEntity.Items.Contains((ComboBoxItem)this.cboEntity.SelectedItem))
			{
				entitySet = ((ComboBoxItem)this.cboEntity.SelectedItem).Tag.ToString();
			}

			foreach (CheckBox item in this.entityProperties.Items)
			{
				if (item.IsChecked == true)
				{
					entityProperties += item.Tag.ToString() + ",";
				}
			}
			if (entityProperties.Length != 0)
			{
				entityProperties = entityProperties.Substring(0, entityProperties.Length - 1);
			}

			authentication = GetAuthenticationString();
			queryOptionValue = GetQueryOptionString();

			return $"{this.url.Text};{entitySet};{this.maxRows.Text};{entityProperties};{authentication};{queryOptionValue}";
		}

		protected override void SetParameterOverride(string parameter)
		{
			if (String.IsNullOrEmpty(parameter))
			{
				return;
			}

			this.url.Text = parameter.Split(';')[0];
		}

		private void cboAuthentication_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			switch (((ComboBoxItem)this.cboAuthentication.SelectedItem).Tag.ToString())
			{
				case "none":
					this.gridBearerAuth.Visibility = Visibility.Hidden;
					this.gridBasicAuth.Visibility = Visibility.Hidden;
					break;
				case "basic":
					this.gridBearerAuth.Visibility = Visibility.Hidden;
					this.gridBasicAuth.Visibility = Visibility.Visible;
					break;
				case "bearer":
					this.gridBasicAuth.Visibility = Visibility.Hidden;
					this.gridBearerAuth.Visibility = Visibility.Visible;
					break;
				default:
					this.gridBearerAuth.Visibility = Visibility.Hidden;
					this.gridBasicAuth.Visibility = Visibility.Hidden;
					break;
			}
		}

		private void btnConnect_Click(object sender, RoutedEventArgs e)
		{
			this.cboEntity.Items.Clear();
			this.entityProperties.Items.Clear();

			try
			{
				string authentication = GetAuthenticationString();

				if (!String.IsNullOrEmpty(authentication))
				{
					List<Entity> entities = ODataV4Service.GetEntitiesName(this.url.Text, authentication);

					if (entities != null && entities.Count != 0)
					{
						foreach (Entity entity in entities)
						{
							if (entity.kind == "EntitySet")
							{
								ComboBoxItem cboItem = new ComboBoxItem
								{
									Content = entity.name,
									Tag = entity.url
								};
								this.cboEntity.Items.Add(cboItem);
							}
						}
						this.cboEntity.SelectedIndex = 0;
						MessageBox.Show("Connection succesfull ! Select an Entity in the DropDown List down below.");
					}
				}
				else
				{
					MessageBox.Show("Invalid Authentication properties.");
				}
			}
			catch (Exception)
			{
				MessageBox.Show("Connection failed ! Try again or change connections properties.");
			}
		}

		private void cboEntity_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			this.entityProperties.Items.Clear();
			try
			{
				string entityUrl = string.Empty;
                if (this.cboEntity.Items.Contains((ComboBoxItem)this.cboEntity.SelectedItem))
				{
					entityUrl = ((ComboBoxItem)this.cboEntity.SelectedItem).Tag.ToString();
				}

				string authentication = GetAuthenticationString();

				if(!String.IsNullOrEmpty(authentication))
				{
					List<string> entityPropertiesName = ODataV4Service.GetColumnsFromEntity(this.url.Text, entityUrl, authentication);
					foreach (string property in entityPropertiesName)
					{
						CheckBox cb = new CheckBox();
						cb.Content = property;
						cb.Tag = property;
						cb.IsChecked = true;
						this.entityProperties.Items.Add(cb);
					}
				}
				else
				{
					MessageBox.Show("Invalid Authentication properties.");
				}
				
			}
			catch (Exception)
			{
				MessageBox.Show("An error occurred while trying to load Columns for the selected Entity. Please try again.");
			}
		}

		private void cboQueryOptionType_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{

			switch (((ComboBoxItem)this.cboQueryOptionType.SelectedItem).Tag.ToString())
			{
				case "none":
					this.gridQueryOrderBy.Visibility = Visibility.Hidden;
					this.gridQueryFilter.Visibility = Visibility.Hidden;
					this.gridQuerySearch.Visibility = Visibility.Hidden;
					break;
				case "orderby":
					this.gridQueryOrderBy.Visibility = Visibility.Visible;
					this.gridQueryFilter.Visibility = Visibility.Hidden;
					this.gridQuerySearch.Visibility = Visibility.Hidden;
					break;
				case "filter":
					this.gridQueryOrderBy.Visibility = Visibility.Hidden;
					this.gridQueryFilter.Visibility = Visibility.Visible;
					this.gridQuerySearch.Visibility = Visibility.Hidden;
					break;
				case "search":
					this.gridQueryOrderBy.Visibility = Visibility.Hidden;
					this.gridQueryFilter.Visibility = Visibility.Hidden;
					this.gridQuerySearch.Visibility = Visibility.Visible;
					break;
				default:
					this.gridQueryOrderBy.Visibility = Visibility.Hidden;
					this.gridQueryFilter.Visibility = Visibility.Hidden;
					this.gridQuerySearch.Visibility = Visibility.Hidden;
					break;
			}
		}

		private string GetAuthenticationString()
		{
            switch (((ComboBoxItem)this.cboAuthentication.SelectedItem).Tag.ToString())
			{
				case "none":
					return "none";
				case "basic":
					if (!String.IsNullOrEmpty(this.username.Text) && !String.IsNullOrEmpty(this.password.Password))
					{
						return "basic/" + username.Text + ":" + password.Password;
					}
					else
					{
						return string.Empty;
					}
				case "bearer":
					if (!string.IsNullOrEmpty(this.token.Text))
					{
						return "bearer/" + this.token.Text;
					}
					else
					{
						return string.Empty;
					}
				default:
					return "none";
			}
		}

		private string GetQueryOptionString()
		{
			switch (((ComboBoxItem)this.cboQueryOptionType.SelectedItem).Tag.ToString())
			{
				case "none":
					return string.Empty;
				case "orderby":
					if (!String.IsNullOrEmpty(this.queryOrderBy.Text))
					{
						return "$orderby=" + this.queryOrderBy.Text;
					}
					else
					{
                        return string.Empty;
                    }
				case "filter":
					if (!String.IsNullOrEmpty(this.queryFilter.Text))
					{
						return "$filter=" + this.queryFilter.Text;
					}
					else
					{
                        return string.Empty;
                    }
				case "search":
					if (!String.IsNullOrEmpty(this.querySearch.Text))
					{
						return "$search=" + this.querySearch.Text;
					}
					else
					{
                        return string.Empty;
                    }
				default:
                    return string.Empty;
			}
		}
	}
}