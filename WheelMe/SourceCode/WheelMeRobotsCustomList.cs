using System;
using System.Net.Http;
using System.Linq;
using System.Threading.Tasks;
using Peakboard.ExtensionKit;
using WheelMe.DTO;

namespace WheelMe
{
    [Serializable]
    [CustomListIcon("WheelMe.WheelMe.png")]
    class WheelMeRobotsCustomList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "WheelMeRobots",
                Name = "Wheel.Me Robots",
                Description = "Fetches data from WheelMe API",
                PropertyInputPossible = true,
                PropertyInputDefaults =
                {
                    new CustomListPropertyDefinition() { Name = "BaseURL", Value = "https://XXX.playground.wheelme-web.com/" },
                    new CustomListPropertyDefinition() { Name = "UserName", Value = "" },
                    new CustomListPropertyDefinition() { Name = "Password", Masked = true, Value = "" },
                    new CustomListPropertyDefinition() { Name = "FloorID", Value = "2" },
                    new CustomListPropertyDefinition() { Name = "RobotNameFilter", Value = "*" }
                },
                Functions = new CustomListFunctionDefinitionCollection
                {
                    new CustomListFunctionDefinition
                    {
                        Name = "NavigateToPositionID",
                        Description = "Navigates the robot to a certain position",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "RobotID",
                                Type = CustomListFunctionParameterTypes.String,
                                Optional = false,
                                Description = "The name of the robot"
                            },
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "FloorID",
                                Type = CustomListFunctionParameterTypes.String,
                                Optional = false,
                                Description = "The name of the position"
                            },
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "PositionID",
                                Type = CustomListFunctionParameterTypes.String,
                                Optional = false,
                                Description = "The name of the position"
                            },
                        },
                    },
                    new CustomListFunctionDefinition
                    {
                        Name = "NavigateToPositionName",
                        Description = "Navigates the robot to a certain position",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "RobotID",
                                Type = CustomListFunctionParameterTypes.String,
                                Optional = false,
                                Description = "The name of the robot"
                            },
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "FloorID",
                                Type = CustomListFunctionParameterTypes.String,
                                Optional = false,
                                Description = "The name of the position"
                            },
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "PositionName",
                                Type = CustomListFunctionParameterTypes.String,
                                Optional = false,
                                Description = "The name of the position"
                            },
                        },
                    }
                }
            };
        }

        protected override void CheckDataOverride(CustomListData data)
        {
            if (string.IsNullOrWhiteSpace(data.Properties["BaseURL"]))
            {
                throw new InvalidOperationException("Invalid BaseURL");
            }

            if (string.IsNullOrWhiteSpace(data.Properties["UserName"]))
            {
                throw new InvalidOperationException("Invalid UserName");
            }

            if (string.IsNullOrWhiteSpace(data.Properties["Password"]))
            {
                throw new InvalidOperationException("Invalid Password");
            }

            if (string.IsNullOrWhiteSpace(data.Properties["FloorID"]))
            {
                throw new InvalidOperationException("Invalid FloorID");
            }

            if (string.IsNullOrWhiteSpace(data.Properties["RobotNameFilter"]))
            {
                throw new InvalidOperationException("Invalid RobotNameFilter");
            }
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            var columns = new CustomListColumnCollection();
            columns.Add(new CustomListColumn("ID", CustomListColumnTypes.String));
            columns.Add(new CustomListColumn("Name", CustomListColumnTypes.String));
            columns.Add(new CustomListColumn("PositionX", CustomListColumnTypes.Number));
            columns.Add(new CustomListColumn("PositionY", CustomListColumnTypes.Number));
            columns.Add(new CustomListColumn("State", CustomListColumnTypes.String));
            columns.Add(new CustomListColumn("OperatingMode", CustomListColumnTypes.String));
            columns.Add(new CustomListColumn("NavigatingToPositionId", CustomListColumnTypes.String));
            columns.Add(new CustomListColumn("NavigatingToPositionName", CustomListColumnTypes.String));
            columns.Add(new CustomListColumn("CurrentPositionId", CustomListColumnTypes.String));
            columns.Add(new CustomListColumn("CurrentPositionName", CustomListColumnTypes.String));
            return columns;
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            var robots = Task.Run(async () => await GetRobotsAsync(data)).GetAwaiter().GetResult();
            var items = new CustomListObjectElementCollection();
            foreach (var row in robots)
            {
                var item = new CustomListObjectElement();
                item.Add("ID", row.Id);
                item.Add("Name", row.Name);
                item.Add("PositionX", row.Position.X);
                item.Add("PositionY", row.Position.Y);
                item.Add("State", row.State);
                item.Add("OperatingMode", row.OperatingMode);
                item.Add("NavigatingToPositionId", row.NavigatingToPositionId);
                item.Add("NavigatingToPositionName", row.NavigatingToPositionName);
                item.Add("CurrentPositionId", row.CurrentPositionId);
                item.Add("CurrentPositionName", row.CurrentPositionName);
                items.Add(item);
            }

            return items;
        }

        private async Task<RobotDto[]> GetRobotsAsync(CustomListData data)
        {
            using (var client = WheelMeExtension.ProduceHttpClient(data))
            {
                await WheelMeExtension.AuthenticateClientAsync(client, data.Properties["UserName"], data.Properties["Password"]);

                var floorId = data.Properties["FloorID"];
                var response = await client.GetRequestAsync<RobotDto[]>($"api/public/maps/{floorId}/robots");
                
                var query = response.Select(r =>
                {
                    r.NavigatingToPositionName = WheelMeHelper.GetPositionNameFromId(client, floorId, r.NavigatingToPositionId?.ToString("D"));
                    r.CurrentPositionName = WheelMeHelper.GetPositionNameFromId(client, floorId, r.CurrentPositionId?.ToString("D"));

                    return r;
                });

                if (!data.Properties["RobotNameFilter"].Equals("*"))
                {
                    var filter = data.Properties["RobotNameFilter"];
                    query = query.Where(r => r.Name.Equals(filter, StringComparison.OrdinalIgnoreCase));
                }
                
                return query.ToArray();
            }
        }

        protected override CustomListExecuteReturnContext ExecuteFunctionOverride(CustomListData data,
            CustomListExecuteParameterContext context)
        {
            var returnContext = default(CustomListExecuteReturnContext);

            if (context.FunctionName.Equals("NavigateToPositionID", StringComparison.OrdinalIgnoreCase))
            {
                string RobotID = context.Values[0].StringValue;
                string FloorID = context.Values[1].StringValue;
                string PositionID = context.Values[2].StringValue;

                try
                {
                    Task.Run(async () => await NavigateToPositionAsync(data, FloorID, RobotID, PositionID)).GetAwaiter().GetResult();
                    Log?.Info($"Navigation request to position {PositionID} on floor {FloorID} successfully sent");
                }
                catch (Exception e)
                {
                    Log?.Error(e.Message);
                    throw;
                }
            }
            else if (context.FunctionName.Equals("NavigateToPositionName", StringComparison.OrdinalIgnoreCase))
            {
                string RobotID = context.Values[0].StringValue;
                string FloorID = context.Values[1].StringValue;
                string PositionName = context.Values[2].StringValue;

                Log?.Info($"Trying to navigate robot {RobotID} to position {PositionName}");
                
                try
                {
                    Task.Run(async () => await NavigateToPositionAsync(data, FloorID, RobotID, PositionName)).GetAwaiter().GetResult();
                    Log?.Info($"Navigation request to position {PositionName} on floor {FloorID} successfully sent");
                }
                catch (Exception e)
                {
                    Log?.Error(e.Message);
                    throw;
                }
            }
            else
            {
                throw new DataErrorException("Function is not supported in this version.");
            }

            return returnContext;
        }
        
        private async Task NavigateToPositionAsync(CustomListData data, string floor, string robot, string destination)
        {
            using (var client = WheelMeExtension.ProduceHttpClient(data))
            {
                await WheelMeExtension.AuthenticateClientAsync(client, data.Properties["UserName"], data.Properties["Password"]);
                
                if (long.TryParse(destination, out var id))
                {
                    await NavigateToPositionAsync(client, floor, robot, id);
                }
                else
                {
                    var positions = await client.GetRequestAsync<PositionDto[]>($"api/public/maps/{floor}/positions");
                    var positionId = positions.First(p => p.Name.Equals(destination, StringComparison.OrdinalIgnoreCase)).Id;
                    await NavigateToPositionAsync(client, floor, robot, positionId);
                }
            }
        }

        private async Task NavigateToPositionAsync(HttpClient client, string floor, string robot, long positionId)
        {
            var request = new NavigationRequestDto
            {
                PositionId = positionId
            };

            await client.PostRequestAsync($"api/public/maps/{floor}/robots/{robot}/navigate", request);
        }
    }
}
