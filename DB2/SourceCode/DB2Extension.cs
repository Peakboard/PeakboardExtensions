using Peakboard.ExtensionKit;


namespace PeakboardExtensionDB2
{
    [ExtensionIcon("PeakboardExtensionDB2.DB2.png")]    
    public class DB2Extension : ExtensionBase
    {
        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "DB2", // Must be unqiue over all extensions, so may use a namespace notation
                Name = "DB2 Extension",
                Description = "This is an Extension for accessing DB2 data",
                Version = "1.0",
                Author = "Peakboard Team",
                Company = "Peakboard GmbH",
                Copyright = "Copyright © 2021",
            };
        }

        protected override CustomListCollection GetCustomListsOverride()
        {
            return new CustomListCollection
            {
                new DB2CustomList(),
            };
        }
    }
}
