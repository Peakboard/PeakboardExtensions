using Peakboard.ExtensionKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
namespace HubSpot
{
    [Serializable]
    [ExtensionIcon("HubSpot.icon.png")]
    public class HubSpotCustomList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "HubSpotCustomList",
                Name = "HubSpot API",
                Description = "Interface with HubSpot API",
                PropertyInputPossible = true,
                PropertyInputDefaults =
                {
                    new CustomListPropertyDefinition() { Name = "Token", Value = "Enter your API token here", Masked = true }
                }
            };
        }
        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            return new CustomListColumnCollection
            {
                new CustomListColumn("Id", CustomListColumnTypes.String),
                new CustomListColumn("Content", CustomListColumnTypes.String),            
                new CustomListColumn("Subject", CustomListColumnTypes.String),
                new CustomListColumn("Archived", CustomListColumnTypes.Boolean),
                new CustomListColumn("CreatedAt", CustomListColumnTypes.String),
                new CustomListColumn("UpdatedAt", CustomListColumnTypes.String),
            };
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            string token = data.Properties["Token"];

            var result = GetResult(token).Result;
            var items = new CustomListObjectElementCollection();

            if (result != null)
            {
                foreach (var item in result.Results)
                {
                    items.Add(new CustomListObjectElement {
                        {"Id", item.Id},
                        {"Content", item.Poperties.Content},
                        {"Subject",item.Poperties.Subject},
                        {"Archived",item.Archived},
                        {"CreatedAt",item.CreatedAt},
                        {"UpdatedAt",item.UpdatedAt},
                    });
                }
                return items;
            }
            return null;
        }

        public static async Task<HubSpot.Models.Result> GetResult(string token)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                var responce = client.GetAsync("https://api.hubapi.com/crm/v3/objects/tickets").Result;
                if (responce.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var result = JsonConvert.DeserializeObject<HubSpot.Models.Result>(await responce.Content.ReadAsStringAsync());
                    return result;
                }
                return null;

            }

        }
    }
}
