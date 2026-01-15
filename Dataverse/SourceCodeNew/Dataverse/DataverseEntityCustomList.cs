using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using Peakboard.ExtensionKit;
using Newtonsoft.Json.Linq;

namespace Dataverse
{
    [Serializable]
    [CustomListIcon("WheelMe.WheelMe.png")]

    class DataverseEntityCustomList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "DataverseEntities",
                Name = "Dataverse Entities",
                Description = "Fetches data from Dataverse Entities",
                PropertyInputPossible = true,
                PropertyInputDefaults = {
                new CustomListPropertyDefinition() { Name = "DataverseURL", Value = "https://xxx.crm4.dynamics.com/" },
                new CustomListPropertyDefinition() { Name = "ClientId", Value = "" },
                new CustomListPropertyDefinition() { Name = "ClientSecret", TypeDefinition = TypeDefinition.String.With(masked: true) },
                new CustomListPropertyDefinition() { Name = "TenantId", Value=""  },
                new CustomListPropertyDefinition() { Name = "Entity", Value=""  }
                    }
            };
        }

        protected override void CheckDataOverride(CustomListData data)
        {
            if (string.IsNullOrWhiteSpace(data.Properties["DataverseURL"]))
            {
                throw new InvalidOperationException("Invalid DataverseURL");
            }
            if (!data.Properties["DataverseURL"].EndsWith($"/"))
            {
                throw new InvalidOperationException("BaseURL must end with /");
            }
            if (string.IsNullOrWhiteSpace(data.Properties["ClientId"]))
            {
                throw new InvalidOperationException("Invalid ClientId");
            }
            if (string.IsNullOrWhiteSpace(data.Properties["ClientSecret"]))
            {
                throw new InvalidOperationException("Invalid ClientSecret");
            }
            if (string.IsNullOrWhiteSpace(data.Properties["TenantId"]))
            {
                throw new InvalidOperationException("Invalid TenantId");
            }
            if (string.IsNullOrWhiteSpace(data.Properties["Entity"]))
            {
                throw new InvalidOperationException("Invalid Entity");
            }
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            var columns = new CustomListColumnCollection();
            columns.Add(new CustomListColumn("ID", CustomListColumnTypes.String));
            return columns;
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            var items = new CustomListObjectElementCollection();

                var item = new CustomListObjectElement();
                item.Add("ID", "123");
                items.Add(item);
            return items;
        }
    }
}
