using System;
using System.Net.Http;
using System.Threading.Tasks;
using Peakboard.ExtensionKit;
using WheelMe.DTO;

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
                PropertyInputDefaults =
                {
                    new CustomListPropertyDefinition() { Name = "BaseURL", Value = "https://XXX.playground.wheelme-web.com/" },
                    new CustomListPropertyDefinition() { Name = "UserName", Value = "" },
                    new CustomListPropertyDefinition() { Name = "Password", Masked = true, Value = "" },
                    new CustomListPropertyDefinition() { Name = "FloorID", Value = "2" }
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
            return Task.Run(async () => await GetItemsAsync(data)).GetAwaiter().GetResult();
        }

        protected virtual async Task<CustomListObjectElementCollection> GetItemsAsync(CustomListData data)
        {
            using (HttpClient client = WheelMeExtension.ProduceHttpClient(data))
            {
                await WheelMeExtension.AuthenticateClientAsync(client, data.Properties["UserName"], data.Properties["Password"]);
                var response = await client.GetRequestAsync<PositionDto[]>($"api/public/maps/{data.Properties["FloorID"]}/positions");

                var items = new CustomListObjectElementCollection();
                foreach (var row in response)
                {
                    var item = new CustomListObjectElement();
                    item.Add("ID", row.Id.ToString("D"));
                    item.Add("Name", row.Name);
                    item.Add("State", row.State.ToString());
                    item.Add("PositionX", row.Position.X);
                    item.Add("PositionY", row.Position.Y);
                    item.Add("OccupiedBy", row.OccupiedBy);
                    items.Add(item);
                }

                return items;
            }
        }
    }
}
