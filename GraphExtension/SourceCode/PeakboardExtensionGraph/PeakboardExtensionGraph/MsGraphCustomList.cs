using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Peakboard.ExtensionKit;

namespace PeakboardExtensionGraph
{
    [Serializable]
    public class MsGraphCustomList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = $"MsGraphCustomList",
                Name = "MsGraph List",
                Description = "Returns data from MySql database",
                PropertyInputPossible = true,
                PropertyInputDefaults = {
                    new CustomListPropertyDefinition() { Name = "ClientID", Value = "" },
                    new CustomListPropertyDefinition() { Name = "TenantID", Value = "" },
                    new CustomListPropertyDefinition() { Name = "Data", Value = "contacts" }
                },
            };
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {

            GraphHelper.InitGraph((code, url) =>
            {
                StreamWriter writer = new StreamWriter(@"C:\Users\Yannis\Documents\Peakboard\auth.txt");
                writer.WriteLine(code);
                writer.WriteLine(url);
                writer.Close();
                return Task.FromResult(0);
            });
            
            var cols = new CustomListColumnCollection
            {
                new CustomListColumn("id", CustomListColumnTypes.String),
                new CustomListColumn("name", CustomListColumnTypes.String),
                new CustomListColumn("email", CustomListColumnTypes.String)
            };

            return cols;
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            data.Properties.TryGetValue("Data", StringComparison.OrdinalIgnoreCase, out var type);
            var response = GraphHelper.MakeGraphCall(type);
            var contacts = JsonConvert.DeserializeObject<ContactList>(response.Result);
            
            var items = new CustomListObjectElementCollection();

            foreach (var contact in contacts.value)
            {
                var newItem = new CustomListObjectElement();
                newItem.Add("id", contact.id);
                newItem.Add("name", contact.displayName);
                newItem.Add("email", contact.emailAddresses[0].address);
                
                items.Add(newItem);
            }

            return items;
        }
    }
}