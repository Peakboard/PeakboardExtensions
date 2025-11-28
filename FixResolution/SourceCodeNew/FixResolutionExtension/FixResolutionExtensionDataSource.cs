using Peakboard.ExtensionKit;

namespace FixResolutionExtension
{
    [ExtensionIcon("FixResolutionExtension.pb_datasource_gett.png")]
    public class FixResolutionExtension : ExtensionBase
    {
        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "FixResolutionExtension",
                Name = "FixResolution",
                Description = "This Extension is used to Fix an Resolution issue with 4K Screens",
                Version = "1.0",
                Author = "Benjamin Sturm",
                Company = "Peakboard GmbH",
                Copyright = "Peakboard GmbH",
            };
        }

        protected override CustomListCollection GetCustomListsOverride()
        {
                return new CustomListCollection { new FixResolutionExtensionCustomList(), };
        }
    }
}
