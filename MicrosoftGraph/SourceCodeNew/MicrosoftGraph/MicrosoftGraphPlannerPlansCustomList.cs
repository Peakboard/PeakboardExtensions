using System;
using Newtonsoft.Json.Linq;
using Peakboard.ExtensionKit;

namespace MicrosoftGraph
{
    [Serializable]
    [CustomListIcon("MicrosoftGraph.MicrosoftGraph.png")]
    class MicrosoftGraphPlannerPlansCustomList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "MicrosoftGraphPlannerPlans",
                Name = "Microsoft Planner Plans",
                Description = "Lists Microsoft Planner plans owned by a given Microsoft 365 group / Team.",
                PropertyInputPossible = true,
                PropertyInputDefaults =
                {
                    new CustomListPropertyDefinition() { Name = "TenantId", Value = "" },
                    new CustomListPropertyDefinition() { Name = "ClientId", Value = "" },
                    new CustomListPropertyDefinition() { Name = "ClientSecret", TypeDefinition = TypeDefinition.String.With(masked: true) },
                    new CustomListPropertyDefinition() { Name = "GroupId", Value = "" },
                }
            };
        }

        protected override void CheckDataOverride(CustomListData data)
        {
            if (string.IsNullOrWhiteSpace(data.Properties["TenantId"]))
                throw new InvalidOperationException("TenantId is required.");
            if (string.IsNullOrWhiteSpace(data.Properties["ClientId"]))
                throw new InvalidOperationException("ClientId is required.");
            if (string.IsNullOrWhiteSpace(data.Properties["ClientSecret"]))
                throw new InvalidOperationException("ClientSecret is required.");
            if (string.IsNullOrWhiteSpace(data.Properties["GroupId"]))
                throw new InvalidOperationException("GroupId is required (the Microsoft 365 group / Team that owns the plans).");
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            return new CustomListColumnCollection
            {
                new CustomListColumn("id", CustomListColumnTypes.String),
                new CustomListColumn("title", CustomListColumnTypes.String),
                new CustomListColumn("owner", CustomListColumnTypes.String),
                new CustomListColumn("createdDateTime", CustomListColumnTypes.String),
            };
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            var groupId = data.Properties["GroupId"].Trim();
            var url = $"groups/{groupId}/planner/plans";

            using var http = MicrosoftGraphExtension.CreateGraphClient(data);
            using var response = http.GetAsync(url).GetAwaiter().GetResult();
            var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Microsoft Graph returned {(int)response.StatusCode} {response.ReasonPhrase} for /{url}: {body}");
            }

            this.Log.Info($"Fetched Planner plans for group {groupId}.");

            var json = JObject.Parse(body);
            var plans = json["value"] as JArray ?? new JArray();

            var items = new CustomListObjectElementCollection();
            foreach (var plan in plans)
            {
                var item = new CustomListObjectElement();
                item.Add("id", plan["id"]?.ToString() ?? string.Empty);
                item.Add("title", plan["title"]?.ToString() ?? string.Empty);
                item.Add("owner", plan["owner"]?.ToString() ?? string.Empty);
                item.Add("createdDateTime", plan["createdDateTime"]?.ToString() ?? string.Empty);
                items.Add(item);
            }

            return items;
        }
    }
}
