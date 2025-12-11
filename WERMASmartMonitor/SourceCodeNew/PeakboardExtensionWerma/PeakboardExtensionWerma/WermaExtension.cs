using Peakboard.ExtensionKit;

namespace PeakboardExtensionWerma
{
    [ExtensionIcon("PeakboardExtensionWerma.werma.png")]
    public class WermaExtension : ExtensionBase
    {
        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "Werma",
                Name = "Werma Extension",
                Description = "This is an Extension for accessing Werma data.",
                Version = "2.0",
                Author = "Peakboard Team",
                Company = "Peakboard GmbH",
                Copyright = "Copyright © 2021",
            };
        }

        protected override CustomListCollection GetCustomListsOverride()
        {
            return new CustomListCollection
            {
                new WermaController(),
                new WermaTimestampList()
            };
        }
    }
}
