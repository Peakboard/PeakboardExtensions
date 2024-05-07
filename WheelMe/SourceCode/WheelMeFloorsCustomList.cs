using System;
using System.Threading.Tasks;
using Peakboard.ExtensionKit;
using WheelMe.DTO;

namespace WheelMe
{
    [Serializable]
    [CustomListIcon("WheelMe.WheelMe.png")]
    class WheelMeFloorsCustomList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "WheelMeFloors",
                Name = "Wheel.Me Floors",
                Description = "Fetches data from WheelMe API",
                PropertyInputPossible = true,
                PropertyInputDefaults =
                {
                    new CustomListPropertyDefinition() { Name = "BaseURL", Value = "https://XXX.playground.wheelme-web.com/" },
                    new CustomListPropertyDefinition() { Name = "UserName", Value = "" },
                    new CustomListPropertyDefinition() { Name = "Password", Masked = true, Value = "" },
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
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            var columns = new CustomListColumnCollection();
            columns.Add(new CustomListColumn("ID", CustomListColumnTypes.String));
            columns.Add(new CustomListColumn("Name", CustomListColumnTypes.String));
            return columns;
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            return Task.Run(async () => await GetItemsAsync(data)).GetAwaiter().GetResult();
        }

        protected virtual async Task<CustomListObjectElementCollection> GetItemsAsync(CustomListData data)
        {
            using (var client = WheelMeExtension.ProduceHttpClient(data))
            {
                await WheelMeExtension.AuthenticateClientAsync(client, data.Properties["UserName"], data.Properties["Password"]);
                var response = await client.GetRequestAsync<MapItemDto[]>("api/public/maps");

                var items = new CustomListObjectElementCollection();
                foreach (var row in response)
                {
                    var item = new CustomListObjectElement();
                    item.Add("ID", row.Id.ToString("D"));
                    item.Add("Name", row.Name);
                    items.Add(item);
                }

                return items;
            }
        }
    }
}