using Peakboard.ExtensionKit;
using System;

namespace AVMFritz
{
    [ExtensionIcon("AVMFritz.Fritz.png")]
    public class FritzExtension : ExtensionBase
    {
        public FritzExtension(IExtensionHost host) : base(host)
        {
        }

        protected override ExtensionDefinition GetDefinitionOverride()
        {
            // Create the extension definition
            return new ExtensionDefinition
            {
                ID = "AVMFritz", // Must be unqiue over all extensions, so may use a namespace notation
                Name = "AVMFritz",
                Description = "This extension enables easy access to Fritz thermostats",
                Version = "1.0",
                Author = "Thilo Brosinsky",
                Company = "Peakboard GmbH",
                Copyright = "Copyright © Peakboard GmbH",
            };
        }

        protected override CustomListCollection GetCustomListsOverride()
        {
            return new CustomListCollection
            {
                new FritzThermostatList(),
            };
        }
    }
}
