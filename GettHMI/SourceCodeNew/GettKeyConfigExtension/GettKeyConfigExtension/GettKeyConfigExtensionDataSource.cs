using Peakboard.ExtensionKit;

namespace GettKeyConfigExtension
{
    [ExtensionIcon("GettKeyConfigExtension.pb_datasource_gett.png")]
    public class GettKeyConfigExtension : ExtensionBase
    {
        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "GettKeyConfigExtension",
                Name = "Gett HMI Keys",
                Description = "This Extension is used to configure GETT HMI Keys",
                Version = "3.0",
                Author = "Benjamin Sturm",
                Company = "Peakboard GmbH",
                Copyright = "Peakboard GmbH",
            };
        }

        protected override CustomListCollection GetCustomListsOverride()
        {
            return new CustomListCollection { new GettKeyConfigExtensionCustomList(), };
        }
    }
}