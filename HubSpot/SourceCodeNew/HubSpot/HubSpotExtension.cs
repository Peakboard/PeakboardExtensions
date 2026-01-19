using Peakboard.ExtensionKit;

namespace HubSpot;

// This class name is referenced in the Extension.xml 'Class' attribute
internal class HubSpotExtension : ExtensionBase
{
    // REQUIRED: parameterless ctor
    public HubSpotExtension() : base()
    {
    }

    // REQUIRED: host ctor
    public HubSpotExtension(IExtensionHost host) : base(host)
    {
    }

    protected override ExtensionDefinition GetDefinitionOverride()
    {
        return new ExtensionDefinition
        {
            ID = "HubSpot",
            Name = "HubSpot API Extension",
            Description = "Interface with the HubSpot API to get all Tickets",
            Version = "1.1",
            Author = "Makhsum",
            Company = "Peakboard"
        };
    }

    protected override CustomListCollection GetCustomListsOverride()
    {
        return
        [
            new HubSpotCustomList(),
        ];
    }
}
