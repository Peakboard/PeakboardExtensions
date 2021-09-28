using Peakboard.ExtensionKit;
using PeakboardExtensionMonday.MondayEntities;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace PeakboardExtensionMonday
{
    /// <summary>
    /// Interaction logic for MondayBoardUIControl.xaml
    /// </summary>
    public partial class MondayBoardUIControl : CustomListUserControlBase
    {
        public MondayBoardUIControl()
        {
            InitializeComponent();
        }

        protected override string GetParameterOverride()
        {
            string selectedBoardId="";
            if (this.cboBoard.Items.Contains((ComboBoxItem)this.cboBoard.SelectedItem))
            {
                selectedBoardId = ((ComboBoxItem)this.cboBoard.SelectedItem).Tag.ToString();
            }

            string selectedGroupId = "";
            if (this.cboGroup.Items.Contains((ComboBoxItem)this.cboGroup.SelectedItem))
            {
                selectedGroupId = ((ComboBoxItem)this.cboGroup.SelectedItem).Tag.ToString();
            }

            return $"{this.url.Text};{this.token.Text};{selectedBoardId};{selectedGroupId}";
        }

        protected override void SetParameterOverride(string parameter)
        {
            if (String.IsNullOrEmpty(parameter))
            {
                return;
            }

            this.url.Text = parameter.Split(';')[0];
            this.token.Text = parameter.Split(';')[1];

        }

        protected override void ValidateParameterOverride()
        {

        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            this.cboBoard.Items.Clear();
            this.cboGroup.Items.Clear();

            this.cboBoard.Text = "";
            this.cboGroup.Text = "";

            try
            {
                string apiToken = this.token.Text;
                string apiRoot = this.url.Text;

                using (var client = new MondayClient(apiToken, apiRoot))
                {
                    var service = new MondayService(client);

                    // get all boards
                    List<Board> boards = service.GetBoards();

                    if (boards == null || boards.Count == 0)
                    {
                        return;
                    }
                    else
                    {
                        foreach (var board in boards)
                        {
                            ComboBoxItem cboi = new ComboBoxItem
                            {
                                Content = board.Name,
                                Tag = board.Id
                            };
                            this.cboBoard.Items.Add(cboi);
                        }
                    }
                }
            }
            catch(Exception e1)
            {
                MessageBox.Show(e1.Message);
            }
            
        }

        private void btnSelect_Click(object sender, RoutedEventArgs e)
        {
            if (this.cboBoard.Items.Count != 0 && this.cboBoard.Items.Contains(((ComboBoxItem)this.cboBoard.SelectedItem)))
            {
                this.cboGroup.Items.Clear();
                this.cboGroup.Text = "";

                ComboBoxItem allGroups = new ComboBoxItem
                {
                    Content = "All Groups",
                    Tag = "allGroups"
                };

                this.cboGroup.Items.Add(allGroups);
                this.cboGroup.SelectedItem = allGroups;

                string apiToken = this.token.Text;
                string apiRoot = this.url.Text;

                using (var client = new MondayClient(apiToken, apiRoot))
                {
                    var service = new MondayService(client);

                    int boardId = Int32.Parse(((ComboBoxItem)this.cboBoard.SelectedItem).Tag.ToString());
                    // get all groups
                    List<Group> groups = service.GetGroups(boardId);

                    if (groups == null || groups.Count == 0)
                    {
                        return;
                    }
                    else
                    {
                        foreach (var group in groups)
                        {
                            ComboBoxItem cboi = new ComboBoxItem
                            {
                                Content = group.Title,
                                Tag = group.Id
                            };
                            this.cboGroup.Items.Add(cboi);
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Invalid selected Board. Please check carefully");
            }
            
        }

        private void cboBoard_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.cboGroup.Items.Clear();
        }
    }
}
