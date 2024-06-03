﻿using System;
using System.Net.Http;
using System.Collections.Generic;
using Peakboard.ExtensionKit;
using Newtonsoft.Json.Linq;

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
                PropertyInputDefaults = {
                new CustomListPropertyDefinition() { Name = "BaseURL", Value = "https://XXX.playground.wheelme-web.com/" },
                new CustomListPropertyDefinition() { Name = "UserName", Value = "" },
                new CustomListPropertyDefinition() { Name = "Password", Masked = true, Value=""  },
                new CustomListPropertyDefinition() { Name = "FloorID", Value="2"  },
                new CustomListPropertyDefinition() { Name = "RobotNameFilter", Value="*"  }
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
            List<Robot> robots = GetRobotList(data);
            var items = new CustomListObjectElementCollection();
            foreach (var row in robots)
            {
                var item = new CustomListObjectElement();
                item.Add("ID",row.ID);
                item.Add("Name", row.Name);
                item.Add("PositionX", row.PositionX);
                item.Add("PositionY", row.PositionY);
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

        private List<Robot> GetRobotList(CustomListData data)
        {
            using (HttpClient client = new HttpClient())
            {
                WheelMeExtension.AuthenticateClient(client, data.Properties["BaseURL"], data.Properties["UserName"], data.Properties["Password"]);
                HttpResponseMessage response = client.GetAsync(data.Properties["BaseURL"] + $"api/public/maps/{data.Properties["FloorID"]}/robots").Result;
                var responseBody = response.Content.ReadAsStringAsync().Result;

                if (response.IsSuccessStatusCode)
                {
                    JArray rawRobotList = JArray.Parse(responseBody);
                    var items = new List<Robot>();
                    foreach (var row in rawRobotList)
                    {
                        var item = new Robot();
                        item.ID = row["id"]?.ToString();
                        item.Name = row["name"]?.ToString();

                        if (item.Name.Equals(data.Properties["RobotNameFilter"]) || data.Properties["RobotNameFilter"].Equals("*"))
                        { 
                            item.PositionX = double.Parse(row["position"]?["x"]?.ToString());
                            item.PositionY = double.Parse(row["position"]?["y"]?.ToString());
                            item.State = row["state"]?.ToString();
                            item.OperatingMode = row["operatingMode"]?.ToString();
                            item.NavigatingToPositionId = row["navigatingToPositionId"]?.ToString();
                            item.NavigatingToPositionName = WheelMeHelper.GetPositionNameFromID(client, data, item.NavigatingToPositionId);
                            item.CurrentPositionId = row["currentPositionId"]?.ToString();
                            item.CurrentPositionName = WheelMeHelper.GetPositionNameFromID(client, data, item.CurrentPositionId);
                            items.Add(item);
                        }
                    }
                    return items;
                }
                else
                {
                    throw new Exception("Error during call of api/public/maps\r\n" + response.StatusCode + response.ReasonPhrase + "\r\n" + responseBody.ToString());
                }
            }
        }

        protected override CustomListExecuteReturnContext ExecuteFunctionOverride(CustomListData data, CustomListExecuteParameterContext context)
        {
            var returnContext = default(CustomListExecuteReturnContext);

            if (context.FunctionName.Equals("NavigateToPositionID", StringComparison.InvariantCultureIgnoreCase))
            {
                string RobotID = context.Values[0].StringValue;
                string FloorID = context.Values[1].StringValue;
                string PositionID = context.Values[2].StringValue;

                using (HttpClient client = new HttpClient())
                {
                    WheelMeExtension.AuthenticateClient(client, data.Properties["BaseURL"], data.Properties["UserName"], data.Properties["Password"]);
                    string json = $"{{\r\n  \"positionId\": \"{PositionID}\"\r\n}}";
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    HttpResponseMessage response = client.PostAsync(data.Properties["BaseURL"] + $"api/public/maps/{FloorID}/robots/{RobotID}/navigate", content).Result;
                    var responseBody = response.Content.ReadAsStringAsync().Result;
                    if (response.IsSuccessStatusCode)
                    {
                        this.Log?.Info($"Navigation request to position {PositionID} on floor {FloorID} succesfully sent");
                    }
                    else
                    {
                        this.Log?.Error($"Wheel.me API return code {response.StatusCode} - requestbody was {json} - responseBody is {responseBody}");
                        throw new Exception("Error during authentification\r\n" + responseBody.ToString() + "\r\nOriginal Request: " + json);
                    }
                }
            }
            else if (context.FunctionName.Equals("NavigateToPositionName", StringComparison.InvariantCultureIgnoreCase))
            {
                string RobotID = context.Values[0].StringValue;
                string FloorID = context.Values[1].StringValue;
                string PositionName = context.Values[2].StringValue;

                this.Log?.Info($"Trying to navigate robot {RobotID} to position {PositionName}");

                using (HttpClient client = new HttpClient())
                {
                    WheelMeExtension.AuthenticateClient(client, data.Properties["BaseURL"], data.Properties["UserName"], data.Properties["Password"]);

                    string PositionID = null;
                    HttpResponseMessage response = client.GetAsync(data.Properties["BaseURL"] + $"api/public/maps/{FloorID}/positions").Result;
                    var responseBody = response.Content.ReadAsStringAsync().Result;

                    if (response.IsSuccessStatusCode)
                    {
                        JArray rawlist = JArray.Parse(responseBody);
                        var items = new CustomListObjectElementCollection();
                        foreach (var row in rawlist)
                        {
                            if (row["name"]?.ToString().Equals(PositionName) == true)
                            {
                                PositionID = row["id"]?.ToString();
                            }
                        }
                        if (PositionID == null)
                        {
                            throw new Exception($"Could no find position with name {PositionName}");
                        }
                    }
                    else
                    {
                        throw new Exception("Error during call of api/public/maps\r\n" + response.StatusCode + response.ReasonPhrase + "\r\n" + responseBody.ToString());
                    }


                    string json = $"{{\r\n  \"positionId\": \"{PositionID}\"\r\n}}";
                    var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
                    response = client.PostAsync(data.Properties["BaseURL"] + $"api/public/maps/{FloorID}/robots/{RobotID}/navigate", content).Result;
                    responseBody = response.Content.ReadAsStringAsync().Result;
                    if (response.IsSuccessStatusCode)
                    {
                        this.Log?.Info($"Navigation request to position {PositionID} on floor {FloorID} succesfully sent");
                    }
                    else
                    {
                        this.Log?.Error($"Wheel.me API return code {response.StatusCode} - requestbody was {json} - responseBody is {responseBody}");
                        throw new Exception("Error during authentification\r\n" + responseBody.ToString() + "\r\nOriginal Request: " + json);
                    }
                }
            }
            else
            {
                throw new DataErrorException("Function is not supported in this version.");
            }

            return returnContext;
        }


        class Robot
        {
            public string ID;
            public string Name;
            public double PositionX;
            public double PositionY;
            public string State;
            public string OperatingMode;
            public string NavigatingToPositionId;
            public string NavigatingToPositionName;
            public string CurrentPositionId;
            public string CurrentPositionName;
        }
    }
}
