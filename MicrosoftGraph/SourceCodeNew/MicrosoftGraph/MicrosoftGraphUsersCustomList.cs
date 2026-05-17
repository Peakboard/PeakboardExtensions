using System;
using Newtonsoft.Json.Linq;
using Peakboard.ExtensionKit;

namespace MicrosoftGraph
{
    [Serializable]
    [CustomListIcon("MicrosoftGraph.MicrosoftGraph.png")]
    class MicrosoftGraphUsersCustomList : CustomListBase
    {
        private static readonly string[] SelectedFields = new[]
        {
            "id",
            "displayName",
            "userPrincipalName",
            "mail",
            "jobTitle",
            "accountEnabled",
        };

        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "MicrosoftGraphUsers",
                Name = "Microsoft Graph Users",
                Description = "Lists users from your Microsoft Entra ID tenant via Microsoft Graph.",
                PropertyInputPossible = true,
                PropertyInputDefaults =
                {
                    new CustomListPropertyDefinition() { Name = "TenantId", Value = "" },
                    new CustomListPropertyDefinition() { Name = "ClientId", Value = "" },
                    new CustomListPropertyDefinition() { Name = "ClientSecret", TypeDefinition = TypeDefinition.String.With(masked: true) },
                    new CustomListPropertyDefinition() { Name = "MaxRows", TypeDefinition = TypeDefinition.Number.With(minimum: 1, maximum: 999), Value = "50" },
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
                new CustomListColumn("userPrincipalName", CustomListColumnTypes.String),
                new CustomListColumn("mail", CustomListColumnTypes.String),
                new CustomListColumn("jobTitle", CustomListColumnTypes.String),
                new CustomListColumn("accountEnabled", CustomListColumnTypes.Boolean),
            };
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            var maxRows = int.TryParse(data.Properties["MaxRows"], out var parsed) ? parsed : 50;
            if (maxRows < 1) maxRows = 1;
            if (maxRows > 999) maxRows = 999;

            var select = string.Join(",", SelectedFields);
            var url = $"users?$top={maxRows}&$select={select}";

            using var http = MicrosoftGraphExtension.CreateGraphClient(data);
            using var response = http.GetAsync(url).GetAwaiter().GetResult();
            var body = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Microsoft Graph returned {(int)response.StatusCode} {response.ReasonPhrase}: {body}");
            }

            this.Log.Info($"Microsoft Graph /users call succeeded.");

            var json = JObject.Parse(body);
            var users = json["value"] as JArray ?? new JArray();

            var items = new CustomListObjectElementCollection();
            foreach (var user in users)
            {
                var item = new CustomListObjectElement();
                item.Add("id", user["id"]?.ToString() ?? string.Empty);
                item.Add("displayName", user["displayName"]?.ToString() ?? string.Empty);
                item.Add("userPrincipalName", user["userPrincipalName"]?.ToString() ?? string.Empty);
                item.Add("mail", user["mail"]?.ToString() ?? string.Empty);
                item.Add("jobTitle", user["jobTitle"]?.ToString() ?? string.Empty);
                item.Add("accountEnabled", user["accountEnabled"]?.ToObject<bool>() ?? false);
                items.Add(item);
            }

            return items;
        }
    }
}
