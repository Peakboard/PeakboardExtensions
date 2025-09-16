using Manatee.Trello;
using Peakboard.ExtensionKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Peakboard.Extensions.Trello
{
    [CustomListIcon("Peakboard.Extensions.Trello.logo.png")]
    internal class TrelloBoardsCustomList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "TrelloBoardsCustomList",
                Name = "Trello Boards",
                Description = "Returns all boards",
                PropertyInputPossible = true,
                PropertyInputDefaults = {
                    new CustomListPropertyDefinition() { Name = "AppKey", Value = "" },
                    new CustomListPropertyDefinition() { Name = "UserToken", Value = "" }
                },
            };
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            var columns = new CustomListColumnCollection
            {
                new CustomListColumn("BoardId", CustomListColumnTypes.String),
                new CustomListColumn("Name", CustomListColumnTypes.String),
            };

            return columns;
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            IMe me = OpenConnection(data);

            IBoardCollection boards = null;

            try
            {
                boards = me.Boards;
            }
            catch (Exception ex)
            {
                Log.Error("Error getting boards", ex);
                throw;
            }

            try
            {
                boards.Refresh().Wait();
            }
            catch (Exception ex)
            {
                Log.Error("Error refreshing boards", ex);
                throw;
            }

            var items = new CustomListObjectElementCollection();

            foreach (var board in me.Boards)
            {
                CustomListObjectElement newitem = new CustomListObjectElement
                {
                    { "BoardId", board.Id },
                    { "Name", board.Name }
                };
                items.Add(newitem);
            }

            return items;
        }

        private IMe OpenConnection(CustomListData data)
        {
            TrelloAuthorization auth = new TrelloAuthorization
            {
                AppKey = data.Properties["AppKey"],
                UserToken = data.Properties["UserToken"]
            };

            var tf = new TrelloFactory();

            var me = tf.Me(auth).Result;
            return me;
        }

        protected override void CheckDataOverride(CustomListData data)
        {
            bool validData =
                data.Properties.TryGetValue("AppKey", out var appKey) &&
                data.Properties.TryGetValue("UserToken", out var userToken) &&
                !string.IsNullOrEmpty(appKey) &&
                !string.IsNullOrEmpty(userToken);

            if (!validData)
            {
                throw new InvalidOperationException("Invalid or no data provided. Make sure to fill out all properties.");
            }

            base.CheckDataOverride(data);
        }
    }
}