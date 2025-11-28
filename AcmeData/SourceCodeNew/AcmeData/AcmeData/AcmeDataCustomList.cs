using System;
using System.Collections.Generic;
using Peakboard.ExtensionKit;

namespace AcmeData
{
    [Serializable]
    class AcmeDataCustomList : CustomListBase
    {
        private static readonly Random _random = new Random();
        private static readonly string[] _productNames = { "Widget A", "Widget B", "Gadget X", "Gadget Y", "Tool 1", "Tool 2", "Device Alpha", "Device Beta" };
        private static readonly string[] _categories = { "Electronics", "Tools", "Accessories", "Components", "Supplies" };
        private static readonly string[] _statuses = { "Active", "Inactive", "Pending", "Discontinued" };
        private static readonly string[] _locations = { "Warehouse A", "Warehouse B", "Store Front", "Back Room", "Storage" };

        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "AcmeData",
                Name = "Acme Data",
                Description = "Generates random sample data for demonstration",
                PropertyInputPossible = true,
                PropertyInputDefaults = {
                    new CustomListPropertyDefinition() { Name = "RecordCount", Value = "10" },
                    new CustomListPropertyDefinition() { Name = "IncludePrice", Value = "true" },
                    new CustomListPropertyDefinition() { Name = "IncludeLocation", Value = "true" }
                }
            };
        }

        protected override void CheckDataOverride(CustomListData data)
        {
            if (string.IsNullOrWhiteSpace(data.Properties["RecordCount"]))
            {
                throw new InvalidOperationException("RecordCount is required");
            }

            if (!int.TryParse(data.Properties["RecordCount"], out int count) || count <= 0 || count > 1000)
            {
                throw new InvalidOperationException("RecordCount must be a number between 1 and 1000");
            }
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            var columns = new CustomListColumnCollection();
            columns.Add(new CustomListColumn("ID", CustomListColumnTypes.String));
            columns.Add(new CustomListColumn("ProductName", CustomListColumnTypes.String));
            columns.Add(new CustomListColumn("Category", CustomListColumnTypes.String));
            columns.Add(new CustomListColumn("Quantity", CustomListColumnTypes.Number));
            columns.Add(new CustomListColumn("Status", CustomListColumnTypes.String));
            
            if (data.Properties["IncludePrice"]?.ToLower() == "true")
            {
                columns.Add(new CustomListColumn("Price", CustomListColumnTypes.Number));
            }
            
            if (data.Properties["IncludeLocation"]?.ToLower() == "true")
            {
                columns.Add(new CustomListColumn("Location", CustomListColumnTypes.String));
            }
            
            columns.Add(new CustomListColumn("LastUpdated", CustomListColumnTypes.String));
            
            return columns;
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            int recordCount = int.Parse(data.Properties["RecordCount"] ?? "10");
            bool includePrice = data.Properties["IncludePrice"]?.ToLower() == "true";
            bool includeLocation = data.Properties["IncludeLocation"]?.ToLower() == "true";
            
            var items = new CustomListObjectElementCollection();
            
            for (int i = 1; i <= recordCount; i++)
            {
                var item = new CustomListObjectElement();
                item.Add("ID", $"ACME-{i:D4}");
                item.Add("ProductName", _productNames[_random.Next(_productNames.Length)]);
                item.Add("Category", _categories[_random.Next(_categories.Length)]);
                item.Add("Quantity", _random.Next(1, 100));
                item.Add("Status", _statuses[_random.Next(_statuses.Length)]);
                
                if (includePrice)
                {
                    item.Add("Price", Math.Round(_random.NextDouble() * 1000 + 10, 2));
                }
                
                if (includeLocation)
                {
                    item.Add("Location", _locations[_random.Next(_locations.Length)]);
                }
                
                item.Add("LastUpdated", DateTime.Now.AddDays(-_random.Next(0, 30)).ToString("yyyy-MM-dd HH:mm:ss"));
                
                items.Add(item);
            }
            
            return items;
        }
    }
}
