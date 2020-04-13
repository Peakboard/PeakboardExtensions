using System;
using Peakboard.ExtensionKit;


namespace PeakboardExtensionsSQLServer
{
    public class SQLServerExtension : ExtensionBase
    {
        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "SQLServer", // Must be unqiue over all extensions, so may use a namespace notation
                Name = "SQLServer Demo Extension",
                Description = "This is a sample implementation for accessing SQL Server data",
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
                new SQLServerCustomList(),
            };
        }
    }
}
