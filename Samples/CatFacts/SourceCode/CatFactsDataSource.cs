using System;
using Peakboard.ExtensionKit;


namespace PeakboardExtensionCatFacts
{
    public class CatFactsExtension : ExtensionBase
    {
        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "CatFacts", // Must be unqiue over all extensions, so may use a namespace notation
                Name = "Cat Facts Demo Extension",
                Description = "This is a sample implementation for cat facts",
                Version = "1.0",
                Author = "Jesse Pinkman",
                Company = "Los Pollos Hermanos, Inc.",
                Copyright = "Copyright © Los Pollos Hermanos, Inc",
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
