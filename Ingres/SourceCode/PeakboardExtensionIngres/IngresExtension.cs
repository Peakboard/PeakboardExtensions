using System;
using Peakboard.ExtensionKit;


namespace PeakboardExtensionIngres
{
    public class IngresExtension : ExtensionBase
    {
        public IngresExtension(IExtensionHost host) : base(host)
        {
        }


        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "Ingres", // Must be unqiue over all extensions, so may use a namespace notation
                Name = "Ingres Extension",
                Description = "This is an Extension for accessing Ingres data",
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
                new IngresCustomList(),
            };
        }
    }
}
