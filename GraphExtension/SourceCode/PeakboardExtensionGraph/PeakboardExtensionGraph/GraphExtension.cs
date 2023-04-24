using Peakboard.ExtensionKit;
using PeakboardExtensionGraph.AppOnly;
using PeakboardExtensionGraph.UserAuth;
using PeakboardExtensionGraph.UserAuthFunctions;


namespace PeakboardExtensionGraph
{
    [ExtensionIcon("PeakboardExtensionGraph.graph_clean.png")]
    public class GraphExtension : ExtensionBase
    {
        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "MsGraph", // Must be unqiue over all extensions, so may use a namespace notation
                Name = "Microsoft Graph Extension",
                Description = "This is an Extension for accessing Microsoft Graph data",
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
                new MsGraphCustomList(),
                new MsGraphAppOnlyCustomList(),
                new MsGraphFunctionsCustomList()
            };
        }
    }
}