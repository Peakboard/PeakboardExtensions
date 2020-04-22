using System;
using Peakboard.ExtensionKit;


namespace PeakboardExtensionAirportConditions
{
    [ExtensionIcon("PeakboardExtensionAirportConditions.airplane.png")]
    public class AirportConditionExtension : ExtensionBase
    {
        protected override ExtensionDefinition GetDefinitionOverride()
        {
            // Create the extension definition
            return new ExtensionDefinition
            {
                ID = "AirportCondition", // Must be unqiue over all extensions, so may use a namespace notation
                Name = "Airport Weather Condition",
                Description = "This is a sample implementation for using UI in a Peakboard Extension",
                Version = "1.0",
                Author = "Gustavo Fring",
                Company = "Los Pollos Hermanos, Inc.",
                Copyright = "Copyright © Los Pollos Hermanos, Inc",
            };
        }

        protected override CustomListCollection GetCustomListsOverride()
        {
            return new CustomListCollection
            {
                new AirportConditionCustomList(),
            };
        }
    }
}
