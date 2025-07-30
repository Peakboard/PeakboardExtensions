using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Peakboard.ExtensionKit;

namespace HubSpot;

internal class HubSpotExtension : ExtensionBase
{
    protected override ExtensionDefinition GetDefinitionOverride()
    {
        return new ExtensionDefinition
        {
            ID = "HubSpot",
            Name = "HubSpot API Extension",
            Description = "Interface with the HubSpot API to get all Tickets",
            Version = "1.0",
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