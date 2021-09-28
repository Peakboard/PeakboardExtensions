using Peakboard.ExtensionKit;

namespace PeakboardExtensionMonday
{
    [ExtensionIcon("PeakboardExtensionMonday.monday_icon.png")]
    public class MondayExtension : ExtensionBase
    {
        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "Monday",
                Name = "Monday.com Extension",
                Description = "This is an Extension for accessing Monday.com data.",
                Version = "1.0",
                Author = "Peakboard Team",
                Company = "Peakboard GmbH",
                Copyright = "Copyright © 2021",
            };
        }

        protected override CustomListCollection GetCustomListsOverride()
        {
            return new CustomListCollection
            {
                new MondayDataByBoard(),
                new MondayQuerying()
            };
        }
    }
}
