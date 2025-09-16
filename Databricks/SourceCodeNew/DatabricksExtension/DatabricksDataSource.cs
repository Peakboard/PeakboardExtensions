using System;
using Peakboard.ExtensionKit;


namespace Databricks
{
    [ExtensionIcon("Databricks.databricks.png")]
    public class DatabricksExtension : ExtensionBase
    {
        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "Databricks",
                Name = "Databricks Extension",
                Description = "Get your data from Databricks",
                Version = "1.0",
                Author = "Thilo Brosinsky",
                Company = "Peakboard GmbH.",
                Copyright = "Copyright © Peakboard GmbH",
            };
        }

        protected override CustomListCollection GetCustomListsOverride()
        {
            return new CustomListCollection
            {
                new DatabricksCustomList(),
            };
        }
    }
}
