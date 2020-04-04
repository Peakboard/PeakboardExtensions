using System;
using Peakboard.ExtensionKit;


namespace PeakboardExtensionCatFacts
{
    public class CatFactsDataSource : ExtensionBase
    {
        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "CatFacts", // Must be unqiue over all extensions, so may use a namespace notation
                Name = "Cat Facts Data Source",
                Description = "This is a sample implmentation for cat facts",
                Version = "1.0",
                Author = "Patrick Theobald",
                Company = "Acme, Inc.",
                Copyright = "Copyright © Acme, Inc",
            };
        }

        protected override CustomListCollection GetCustomListsOverride()
        {
            return new CustomListCollection
            {
                new CatFactsCustomList(),
            };
        }
    }
}
