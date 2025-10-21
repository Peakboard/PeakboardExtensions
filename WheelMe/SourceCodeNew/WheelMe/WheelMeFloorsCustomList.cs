using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using Peakboard.ExtensionKit;
using Newtonsoft.Json.Linq;

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
                PropertyInputDefaults = {
                new CustomListPropertyDefinition() { Name = "BaseURL", Value = "https://XXX.playground.wheelme-web.com/" },
                new CustomListPropertyDefinition() { Name = "UserName", Value = "" },
                new CustomListPropertyDefinition() { Name = "Password", Masked = true, Value=""  },
            }
            };
        }

        protected override void CheckDataOverride(CustomListData data)
        {
            if (string.IsNullOrWhiteSpace(data.Properties["BaseURL"]))
            {
                throw new InvalidOperationException("Invalid BaseURL");
            }
            if (!data.Properties["BaseURL"].EndsWith($"/"))
            {
                throw new InvalidOperationException("BaseURL must end with /");
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
            using (HttpClient client = WheelMeHelper.GetHttpClient())
            {
                WheelMeExtension.AuthenticateClient(client, data.Properties["BaseURL"], data.Properties["UserName"], data.Properties["Password"]);
                HttpResponseMessage response = client.GetAsync(data.Properties["BaseURL"] + "api/public/maps").Result;
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
