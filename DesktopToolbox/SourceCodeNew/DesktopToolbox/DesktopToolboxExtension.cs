using Peakboard.ExtensionKit;

namespace DesktopToolbox
{
    [ExtensionIcon("DesktopToolbox.DesktopToolbox.png")]
    public class DesktopToolboxExtension : ExtensionBase
    {
        public DesktopToolboxExtension(IExtensionHost host) : base(host)
        {
        }

        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "DesktopToolbox",
                Name = "Desktop Toolbox",
                Description = "Provides desktop session information and utility functions.",
                Version = "1.0",
                MinVersion = "1.0",
                Author = "Patrick",
                Company = "Peakboard GmbH",
                Copyright = "Copyright © Peakboard GmbH",
            };
        }

        protected override CustomListCollection GetCustomListsOverride()
        {
            return new CustomListCollection
            {
                new DesktopInformationCustomList(),
            };
        }
    }
}
