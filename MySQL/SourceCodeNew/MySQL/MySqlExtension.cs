using Peakboard.ExtensionKit;

namespace MySql;

public class MySqlExtension : ExtensionBase
{
    public MySqlExtension(IExtensionHost host) : base(host)
    {
    }


    protected override ExtensionDefinition GetDefinitionOverride()
    {
        Log?.Verbose("MySqlExtension.GetDefinitionOverride");

        return new ExtensionDefinition
        {
            ID = "MySql", // Must be unqiue over all extensions, so may use a namespace notation
            Name = "MySql Extension",
            Description = "This is an Extension for accessing MySql data",
            Version = "1.5",
            Author = "Peakboard Team",
            Company = "Peakboard GmbH",
            Copyright = "Copyright © 2025",
        };
    }

    protected override CustomListCollection GetCustomListsOverride()
    {
        // Logged deliberately. This call is where the custom list publishes its
        // Functions collection, so it is the moment a write-capable datasource
        // either becomes write-capable or silently does not. Without this line
        // there is no way to tell a good bind from a bad one in a box log - a
        // runtime that skips it leaves reads working and every write throwing,
        // and the only visible symptom is missing data.
        Log?.Verbose("MySqlExtension.GetCustomListsOverride");

        return new CustomListCollection
        {
            new MySqlCustomList(),
        };
    }
}