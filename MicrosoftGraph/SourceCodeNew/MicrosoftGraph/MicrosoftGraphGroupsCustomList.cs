using System;
using Newtonsoft.Json.Linq;
using Peakboard.ExtensionKit;

namespace MicrosoftGraph
{
    [Serializable]
    [CustomListIcon("MicrosoftGraph.MicrosoftGraph.png")]
    class MicrosoftGraphGroupsCustomList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "MicrosoftGraphGroups",
                Name = "Microsoft 365 Groups",
                Description = "Lists Microsoft 365 (Unified) groups in your tenant. Use this to find the GroupId that owns a Planner plan or Team.",
                PropertyInputPossible = true,
                PropertyInputDefaults =
                {
                    new CustomListPropertyDefinition() { Name = "TenantId", Value = "" },
                    new CustomListPropertyDefinition() { Name = "ClientId", Value = "" },
                    new CustomListPropertyDefinition() { Name = "ClientSecret", TypeDefinition = TypeDefinition.String.With(masked: true) },
                    new CustomListPropertyDefinition() { Name = "MaxRows", TypeDefinition = TypeDefinition.Number.With(minimum: 1, maximum: 999), Value = "100" },
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
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            return new CustomListColumnCollection
            {
                new CustomListColumn("id", CustomListColumnTypes.String),
                new CustomListColumn("displayName", CustomListColumnTypes.String),
                new CustomListColumn("description", CustomListColumnTypes.String),
                new CustomListColumn("mail", CustomListColumnTypes.String),
                new CustomListColumn("visibility", CustomListColumnTypes.String),
                new CustomListColumn("isTeamsEnabled", CustomListColumnTypes.Boolean),
                new CustomListColumn("createdDateTime", CustomListColumnTypes.String),
            };
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            var maxRows = int.TryParse(data.Properties["MaxRows"], out var parsed) ? parsed : 100;
            if (maxRows < 1) maxRows = 1;
            if (maxRows > 999) maxRows = 999;

            // Filter to Microsoft 365 groups (groupTypes contains "Unified"). Security groups
            // and distribution lists cannot own Planner plans, so excluding them keeps the
            // list focused on what's useful for Planner / Teams discovery.
            // To list all groups instead, drop the $filter clause below.
            const string filter = "groupTypes/any(c:c eq 'Unified')";
            const string select = "id,displayName,description,mail,visibility,resourceProvisioningOptions,createdDateTime";
            var url = $"groups?$top={maxRows}&$filter={Uri.EscapeDataString(filter)}&$select={select}";

            using var http = MicrosoftGraphExtension.CreateGraphClient(data);
            using var response = http.GetAsync(url).GetAwaiter().GetResult();
            var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Microsoft Graph returned {(int)response.StatusCode} {response.ReasonPhrase} for /{url}: {body}");
            }

            this.Log.Info($"Fetched {maxRows} max Microsoft 365 groups.");

            var json = JObject.Parse(body);
            var groups = json["value"] as JArray ?? new JArray();

            var items = new CustomListObjectElementCollection();
            foreach (var group in groups)
            {
                var provisioning = group["resourceProvisioningOptions"] as JArray;
                var isTeamsEnabled = false;
                if (provisioning != null)
                {
                    foreach (var opt in provisioning)
                    {
                        if (string.Equals(opt?.ToString(), "Team", StringComparison.OrdinalIgnoreCase))
                        {
                            isTeamsEnabled = true;
                            break;
                        }
                    }
                }

                var item = new CustomListObjectElement();
                item.Add("id", group["id"]?.ToString() ?? string.Empty);
                item.Add("displayName", group["displayName"]?.ToString() ?? string.Empty);
                item.Add("description", group["description"]?.ToString() ?? string.Empty);
                item.Add("mail", group["mail"]?.ToString() ?? string.Empty);
                item.Add("visibility", group["visibility"]?.ToString() ?? string.Empty);
                item.Add("isTeamsEnabled", isTeamsEnabled);
                item.Add("createdDateTime", group["createdDateTime"]?.ToString() ?? string.Empty);
                items.Add(item);
            }

            return items;
        }
    }
}
