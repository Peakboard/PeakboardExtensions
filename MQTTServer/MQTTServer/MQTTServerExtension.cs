using Peakboard.ExtensionKit;

namespace MQTTServer
{
    public class MQTTServerExtension : ExtensionBase
    {
        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "MQTTServer",
                Name = "MQTTServer",
                Description = "This extension provides information about MQTTServer.",
                Version = "1.0",
            };
        }

        protected override CustomListCollection GetCustomListsOverride()
        {
            return new CustomListCollection
            {
                new MQTTServerList()
            };
        }
    }
}
