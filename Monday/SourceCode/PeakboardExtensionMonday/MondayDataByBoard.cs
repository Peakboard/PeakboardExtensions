using Peakboard.ExtensionKit;
using PeakboardExtensionMonday.MondayEntities;
using System;
using System.Windows;

namespace PeakboardExtensionMonday
{
    [Serializable]
    [CustomListIcon("PeakboardExtensionMonday.monday_icon.png")]
    class MondayDataByBoard : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = $"MondayDataByBoard",
                Name = "Monday.com Data By Board",
                Description = "Returns all the items from a selected Board and/or a selected Group",
                PropertyInputPossible = true,
            };
        }

        protected override FrameworkElement GetControlOverride()
        {
            // return an instance of the UI user control
            return new MondayBoardUIControl();
        }

        protected override void CheckDataOverride(CustomListData data)
        {
            string url = data.Parameter.Split(';')[0];
            string token = data.Parameter.Split(';')[1];
            string boardId = data.Parameter.Split(';')[2];
            string groupId = data.Parameter.Split(';')[3];

            if (string.IsNullOrWhiteSpace(url))
            {
                throw new InvalidOperationException("Please provide an Url");
            }
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidOperationException("Please provide a Token");
            }
            if (string.IsNullOrWhiteSpace(boardId))
            {
                throw new InvalidOperationException("Please select a Board");
            }
            if (string.IsNullOrWhiteSpace(groupId))
            {
                throw new InvalidOperationException("Please select a Group");
            }

        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            var columnCollection = new CustomListColumnCollection();

            columnCollection.Add(new CustomListColumn("items_id", CustomListColumnTypes.String));
            columnCollection.Add(new CustomListColumn("items_name", CustomListColumnTypes.String));

            string url = data.Parameter.Split(';')[0];
            string token = data.Parameter.Split(';')[1];
            string boardId = data.Parameter.Split(';')[2];
            string groupId = data.Parameter.Split(';')[3];

            using (var client = new MondayClient(token, url))
            {
                var service = new MondayService(client);

                if(groupId == "allGroups")
                {
                    // get items for the first board in the list
                    Board board = service.GetBoardWithItems(Int32.Parse(boardId));
                    foreach (var columnName in board.Items[0].ItemColumnValues)
                    {
                        columnCollection.Add(new CustomListColumn(("items_" + columnName.Title).ToLower(), CustomListColumnTypes.String));
                    }
                }
                else
                {
                    Group group = service.GetGroupWithItems(Int32.Parse(boardId), groupId);
                    foreach (var columnName in group.Items[0].ItemColumnValues)
                    {
                        columnCollection.Add(new CustomListColumn(("items_" + columnName.Title).ToLower(), CustomListColumnTypes.String));
                    }
                }
                
            }

            return columnCollection;
        }

        

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            CustomListObjectElementCollection itemsCollection = new CustomListObjectElementCollection();

            string url = data.Parameter.Split(';')[0];
            string token = data.Parameter.Split(';')[1];
            string boardId = data.Parameter.Split(';')[2];
            string groupId = data.Parameter.Split(';')[3];

            using (var client = new MondayClient(token, url))
            {
                var service = new MondayService(client);

                if(groupId == "allGroups")
                {
                    // get items for the first board in the list
                    Board board = service.GetBoardWithItems(Int32.Parse(boardId));
                    foreach (var boardItem in board.Items)
                    {
                        CustomListObjectElement item = new CustomListObjectElement();
                        item.Add("items_id", boardItem.Id);
                        item.Add("items_name", boardItem.Name);
                        foreach (var itemColumn in boardItem.ItemColumnValues)
                        {
                            item.Add(("items_" + itemColumn.Title).ToLower(), itemColumn.Text);
                        }
                        itemsCollection.Add(item);
                    }
                }
                else
                {
                    Group group = service.GetGroupWithItems(Int32.Parse(boardId), groupId);
                    foreach (var groupItem in group.Items)
                    {
                        CustomListObjectElement item = new CustomListObjectElement();
                        item.Add("items_id", groupItem.Id);
                        item.Add("items_name", groupItem.Name);
                        foreach (var itemColumn in groupItem.ItemColumnValues)
                        {
                            item.Add(("items_"+itemColumn.Title).ToLower(), itemColumn.Text);
                        }
                        itemsCollection.Add(item);
                    }
                }
                
            }

            return itemsCollection;
        }
    }
}
