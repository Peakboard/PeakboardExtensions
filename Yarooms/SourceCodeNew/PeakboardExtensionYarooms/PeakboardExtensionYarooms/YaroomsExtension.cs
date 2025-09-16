using System;
using Peakboard.ExtensionKit;

namespace PeakboardExtensionYarooms
{
    public class YaroomsExtension : ExtensionBase
    {
        public YaroomsExtension(IExtensionHost host) : base(host)
        {
        }


        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "Yarooms", // Must be unqiue over all extensions, so may use a namespace notation
                Name = "Yarooms Extension",
                Description = "This is an Extension for accessing Yarooms data",
                Version = "1.0",
                Author = "Peakboard Team",
                Company = "Peakboard GmbH",
                Copyright = "Copyright © 2020",
            };
        }

        protected override CustomListCollection GetCustomListsOverride()
        {
            return new CustomListCollection
            {
                new YaroomsCustomList(),
            };
        }
    }
}
