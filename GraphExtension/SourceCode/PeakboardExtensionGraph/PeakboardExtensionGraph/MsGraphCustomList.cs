using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Peakboard.ExtensionKit;

namespace PeakboardExtensionGraph
{
    [Serializable]
    public class MsGraphCustomList : CustomListBase
    {
        private bool _initialized = false;
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
                    new CustomListPropertyDefinition() { Name = "Data", Value = "contacts" },
                    new CustomListPropertyDefinition() { Name = "RefreshToken", Value = ""}
                },
            };
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            if (!_initialized)
            { 
                InitializeGraph(data);
            }

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
            
            if (!_initialized)
            { 
                InitializeGraph(data);
            }

            var task = GraphHelper.MakeGraphCall(type);
            task.Wait();
            
            var contacts = JsonConvert.DeserializeObject<ContactList>(task.Result);
            
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

        private void InitializeGraph(CustomListData data)
        {
            string refreshToken;
            data.Properties.TryGetValue("RefreshToken", out refreshToken);
            
            // check if refresh token is available
            if (string.IsNullOrEmpty(refreshToken))
            {
                // if not (in designer) initialize by authentication
                var task = GraphHelper.InitGraph((code, url) =>
                {
                    StreamWriter writer = new StreamWriter(@"C:\Users\Yannis\Documents\Peakboard\auth.txt");
                    writer.WriteLine(code);
                    writer.Close();
                    Process.Start(url);
                    return Task.FromResult(0);
                });
                task.Wait();

                StreamWriter writer1 = new StreamWriter(@"C:\Users\Yannis\Documents\Peakboard\auth.txt");
                writer1.WriteLine(GraphHelper.GetRefreshToken());
                writer1.Close();
            }
            else
            {
                // if available initialize by refresh token (in runtime)
                var task = GraphHelper.InitGraphInRuntime(refreshToken);
                task.Wait();
                
            }
            _initialized = true;
        }
        
    }
}