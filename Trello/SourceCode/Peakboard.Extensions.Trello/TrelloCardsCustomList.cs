using Manatee.Trello;
using Peakboard.ExtensionKit;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;

namespace Peakboard.Extensions.Trello
{
    [CustomListIcon("Peakboard.Extensions.Trello.logo.png")]
    public class TrelloCardsCustomList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "TrelloCustomList",
                Name = "Trello Cards",
                Description = "Returns cards from a Trello list",
                PropertyInputPossible = true,
                PropertyInputDefaults = {
                    new CustomListPropertyDefinition() { Name = "AppKey", Value = "" },
                    new CustomListPropertyDefinition() { Name = "UserToken", Value = "" },
                    new CustomListPropertyDefinition() { Name = "BoardName", Value = "" },
                    new CustomListPropertyDefinition() { Name = "ListName", Value = "" }
                },
                Functions = new CustomListFunctionDefinitionCollection
                {
                    new CustomListFunctionDefinition
                    {
                        Name = "movecard",
                        Description = "Moves a card from to another list",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "cardid",
                                Type = CustomListFunctionParameterTypes.String,
                                Optional = false,
                                Description = "The ID of the card"
                            },
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "targetlist",
                                Type = CustomListFunctionParameterTypes.String,
                                Optional = false,
                                Description = "The name of the list that the card should be moved to"
                            }
                        },
                    },
                    new CustomListFunctionDefinition
                    {
                        Name = "addcard",
                        Description = "Adds a card",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "name",
                                Type = CustomListFunctionParameterTypes.String,
                                Optional = false,
                                Description = "The name of the new card"
                            },
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "description",
                                Type = CustomListFunctionParameterTypes.String,
                                Optional = false,
                                Description = "The description of the new card"
                            },
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "targetlist",
                                Type = CustomListFunctionParameterTypes.String,
                                Optional = false,
                                Description = "The name of the list that the card should be moved to"
                            }
                        },
                    },
                    new CustomListFunctionDefinition
                    {
                        Name = "changecarddescription",
                        Description = "Changes the description of a card",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "cardid",
                                Type = CustomListFunctionParameterTypes.String,
                                Optional = false,
                                Description = "The ID of the card"
                            },
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "description",
                                Type = CustomListFunctionParameterTypes.String,
                                Optional = false,
                                Description = "The new description of the card"
                            }
                        },
                    },
                    new CustomListFunctionDefinition
                    {
                        Name = "changecardtitle",
                        Description = "Changes the title of a card",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "cardid",
                                Type = CustomListFunctionParameterTypes.String,
                                Optional = false,
                                Description = "The ID of the card"
                            },
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "description",
                                Type = CustomListFunctionParameterTypes.String,
                                Optional = false,
                                Description = "The new title of the card"
                            }
                        },
                    },
                    new CustomListFunctionDefinition
                    {
                        Name = "delete",
                        Description = "Deletes a card",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "cardid",
                                Type = CustomListFunctionParameterTypes.String,
                                Optional = false,
                                Description = "The ID of the card that should be deleted."
                            }
                        },
                    },
                }
            };
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            var columns = new CustomListColumnCollection
            {
                new CustomListColumn("CardId", CustomListColumnTypes.String),
                new CustomListColumn("Name", CustomListColumnTypes.String),
                new CustomListColumn("Description", CustomListColumnTypes.String),
                new CustomListColumn("Labels", CustomListColumnTypes.String),
                new CustomListColumn("LabelColor", CustomListColumnTypes.String)
            };

            return columns;
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            IMe me = OpenConnection(data);
            string boardName = data.Properties["BoardName"];
            string listName = data.Properties["ListName"];

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

            var board = boards.FirstOrDefault(x => x.Name == boardName);
            
            if (board == null)
            {
                Log.Error($"Cannot find the board {boardName}. Board names are {string.Join(", ", boards.Select(x => x.Name).ToArray())}");
                return null;
            }

            var list = board.Lists.FirstOrDefault(x => x.Name == listName);

            if (list == null)
            {
                Log.Error($"Cannot find the list {listName} of board {boardName}. List names are {string.Join(", ", board.Lists.Select(x => x.Name).ToArray())}");
                return null;
            }

            var items = new CustomListObjectElementCollection();

            var cards = list.Cards;
            cards.Refresh().Wait();

            foreach (var card in cards)
            {
                CustomListObjectElement newitem = new CustomListObjectElement
                {
                    { "CardId", card.Id },
                    { "Name", card.Name },
                    { "Description", card.Description },
                    { "Labels", string.Join(", ", card.Labels.Select(x => x.Name)) },
                    { "LabelColors", string.Join(", ", card.Labels.Where(x => x.Color.HasValue).Select(x => x.Color.Value.ToString())) }
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
                data.Properties.TryGetValue("BoardName", out var boardName) &&
                data.Properties.TryGetValue("ListName", out var listName) &&
                !string.IsNullOrEmpty(appKey) &&
                !string.IsNullOrEmpty(userToken) &&
                !string.IsNullOrEmpty(boardName) &&
                !string.IsNullOrEmpty(listName);

            if (!validData)
            {
                throw new InvalidOperationException("Invalid or no data provided. Make sure to fill out all properties.");
            }

            base.CheckDataOverride(data);
        }
        protected override CustomListExecuteReturnContext ExecuteFunctionOverride(CustomListData data, CustomListExecuteParameterContext context)
        {
            var me = OpenConnection(data);
            string boardName = data.Properties["BoardName"];

            me.Boards.Refresh().Wait();

           var board = me.Boards.FirstOrDefault(x => x.Name == boardName);

            board.Lists.Refresh().Wait();

            if (context.FunctionName.Equals("movecard"))
            {
                var cardId = context.Values[0].StringValue;
                var listName = context.Values[1].StringValue;

                var list = board.Lists.FirstOrDefault(x => x.Name == listName);

                board.Cards.Refresh().Wait();

                var card = board.Cards.FirstOrDefault(x => x.Id == cardId);

                card.List = list;
                TrelloProcessor.Flush().Wait();
            }
            else if (context.FunctionName.Equals("addcard"))
            {
                var name = context.Values[0].StringValue;
                var description = context.Values[1].StringValue;
                var listName = context.Values[2].StringValue;

                var list = board.Lists.FirstOrDefault(x => x.Name == listName);
                var card = list.Cards.Add(name).Result;
                card.Description = description;

                TrelloProcessor.Flush().Wait();
            }
            else if (context.FunctionName.Equals("changecarddescription"))
            {
                var cardId = context.Values[0].StringValue;
                var description = context.Values[1].StringValue;

                var card = board.Cards.FirstOrDefault(x => x.Id == cardId);

                card.Description = description;
                TrelloProcessor.Flush().Wait();
            }
            else if (context.FunctionName.Equals("changecardtitle"))
            {
                var cardId = context.Values[0].StringValue;
                var title = context.Values[1].StringValue;

                var card = board.Cards.FirstOrDefault(x => x.Id == cardId);

                card.Name = title;
                TrelloProcessor.Flush().Wait();
            }
            else if (context.FunctionName.Equals("delete"))
            {
                var cardId = context.Values[0].StringValue;

                var card = board.Cards.FirstOrDefault(x => x.Id == cardId);

                card.Delete().Wait();
                TrelloProcessor.Flush().Wait();
            }

            var returnContext = default(CustomListExecuteReturnContext);
            return returnContext;
        }
    }
}