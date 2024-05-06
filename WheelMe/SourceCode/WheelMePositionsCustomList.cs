﻿using System;
using System.Net.Http;
using Peakboard.ExtensionKit;
using Newtonsoft.Json.Linq;

namespace WheelMe
{
    [Serializable]
    [CustomListIcon("WheelMe.WheelMe.png")]
    
    class WheelMePositionsCustomList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "WheelMePositions",
                Name = "Wheel.Me Positions",
                Description = "Fetches data from WheelMe API",
                PropertyInputPossible = true,
                PropertyInputDefaults = {
                new CustomListPropertyDefinition() { Name = "BaseURL", Value = "https://XXX.playground.wheelme-web.com/" },
                new CustomListPropertyDefinition() { Name = "UserName", Value = "" },
                new CustomListPropertyDefinition() { Name = "Password", Masked = true, Value=""  },
                new CustomListPropertyDefinition() { Name = "FloorID", Value="2"  }
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
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            var columns = new CustomListColumnCollection();
            columns.Add(new CustomListColumn("ID", CustomListColumnTypes.String));
            columns.Add(new CustomListColumn("Name", CustomListColumnTypes.String));
            columns.Add(new CustomListColumn("State", CustomListColumnTypes.String));
            columns.Add(new CustomListColumn("PositionX", CustomListColumnTypes.Number));
            columns.Add(new CustomListColumn("PositionY", CustomListColumnTypes.Number));
            columns.Add(new CustomListColumn("OccupiedBy", CustomListColumnTypes.String));
            return columns;
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            using (HttpClient client = new HttpClient())
            {
                WheelMeExtension.AuthenticateClient(client, data.Properties["BaseURL"], data.Properties["UserName"], data.Properties["Password"]);
                HttpResponseMessage response = client.GetAsync(data.Properties["BaseURL"] + $"api/public/maps/{data.Properties["FloorID"]}/positions").Result;
                var responseBody = response.Content.ReadAsStringAsync().Result;

                if (response.IsSuccessStatusCode)
                {

                    JArray rawlist = JArray.Parse(responseBody);

                    var items = new CustomListObjectElementCollection();
                    foreach (var row in rawlist)
                    {
                        var item = new CustomListObjectElement();
                        item.Add("ID", row["id"]?.ToString());
                        item.Add("Name", row["name"]?.ToString());
                        item.Add("State", row["state"]?.ToString());
                        item.Add("PositionX", double.Parse(row["position"]?["x"]?.ToString()));
                        item.Add("PositionY", double.Parse(row["position"]?["y"]?.ToString()));
                        item.Add("OccupiedBy", row["occupiedBy"]?.ToString());
                        items.Add(item);
                    }
                    return items;
                }
                else
                {
                    throw new Exception("Error during call of api/public/maps\r\n" + response.StatusCode + response.ReasonPhrase + "\r\n" + responseBody.ToString());
                }
            }
        }
    }
}
