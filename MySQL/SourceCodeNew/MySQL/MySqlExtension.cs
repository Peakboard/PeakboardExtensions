using Peakboard.ExtensionKit;

namespace MySql;

public class MySqlExtension : ExtensionBase
{
    public MySqlExtension(IExtensionHost host) : base(host)
    {
    }


    protected override ExtensionDefinition GetDefinitionOverride()
    {
        return new ExtensionDefinition
        {
            ID = "MySql", // Must be unqiue over all extensions, so may use a namespace notation
            Name = "MySql Extension",
            Description = "This is an Extension for accessing MySql data",
            Version = "1.0",
            Author = "Peakboard Team",
            Company = "Peakboard GmbH",
            Copyright = "Copyright © 2025",
        };
    }

    protected override CustomListCollection GetCustomListsOverride()
    {
        return new CustomListCollection
        {
            new MySqlCustomList(),
        };
    }
}