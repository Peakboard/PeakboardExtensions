using System;
using Peakboard.ExtensionKit;


namespace PeakboardExtensionHue
{
    [ExtensionIcon("PeakboardExtensionHue.huelogo.png")]
    public class HueExtension : ExtensionBase
    {
        public HueExtension(IExtensionHost host)
            : base(host)
        {
        }

        protected override ExtensionDefinition GetDefinitionOverride()
        {
            // Create the extension definition
            return new ExtensionDefinition
            {
                ID = "Hue", // Must be unqiue over all extensions, so may use a namespace notation
                Name = "Phillips Hue",
                Description = "This extension enables easy access to Hue bridges",
                Version = "1.0",
                Author = "Patrick Theobald",
                Company = "Peakboard GmbH",
                Copyright = "Copyright © Peakboard GmbH",
            };
        }

        protected override CustomListCollection GetCustomListsOverride()
        {
            return new CustomListCollection
            {
                new HueLightsCustomList(),
            };
        }
    }
}
