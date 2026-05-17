using System;
using Newtonsoft.Json.Linq;
using Peakboard.ExtensionKit;

namespace MicrosoftGraph
{
    [Serializable]
    [CustomListIcon("MicrosoftGraph.MicrosoftGraph.png")]
    class MicrosoftGraphPlannerBucketsCustomList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "MicrosoftGraphPlannerBuckets",
                Name = "Microsoft Planner Buckets",
                Description = "Lists the buckets (board columns) inside a Microsoft Planner plan.",
                PropertyInputPossible = true,
                PropertyInputDefaults =
                {
                    new CustomListPropertyDefinition() { Name = "TenantId", Value = "" },
                    new CustomListPropertyDefinition() { Name = "ClientId", Value = "" },
                    new CustomListPropertyDefinition() { Name = "ClientSecret", TypeDefinition = TypeDefinition.String.With(masked: true) },
                    new CustomListPropertyDefinition() { Name = "PlanId", Value = "" },
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
            if (string.IsNullOrWhiteSpace(data.Properties["PlanId"]))
                throw new InvalidOperationException("PlanId is required.");
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            return new CustomListColumnCollection
            {
                new CustomListColumn("id", CustomListColumnTypes.String),
                new CustomListColumn("name", CustomListColumnTypes.String),
                new CustomListColumn("planId", CustomListColumnTypes.String),
                new CustomListColumn("orderHint", CustomListColumnTypes.String),
            };
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            var planId = data.Properties["PlanId"].Trim();
            var url = $"planner/plans/{planId}/buckets";

            using var http = MicrosoftGraphExtension.CreateGraphClient(data);
            using var response = http.GetAsync(url).GetAwaiter().GetResult();
            var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Microsoft Graph returned {(int)response.StatusCode} {response.ReasonPhrase} for /{url}: {body}");
            }

            this.Log.Info($"Fetched Planner buckets for plan {planId}.");

            var json = JObject.Parse(body);
            var buckets = json["value"] as JArray ?? new JArray();

            var items = new CustomListObjectElementCollection();
            foreach (var bucket in buckets)
            {
                var item = new CustomListObjectElement();
                item.Add("id", bucket["id"]?.ToString() ?? string.Empty);
                item.Add("name", bucket["name"]?.ToString() ?? string.Empty);
                item.Add("planId", bucket["planId"]?.ToString() ?? string.Empty);
                item.Add("orderHint", bucket["orderHint"]?.ToString() ?? string.Empty);
                items.Add(item);
            }

            return items;
        }
    }
}
