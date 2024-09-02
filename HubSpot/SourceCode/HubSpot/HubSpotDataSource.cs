using Peakboard.ExtensionKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HubSpot
{
    [ExtensionIcon("HubSpot.icon.png")]
    internal class HubSpotExtension : ExtensionBase
    {
        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "HubSpot",
                Name = "HubSpot API Extension",
                Description = "Interface with the HubSpot API to retrieve chatbot answers",
                Version = "1.0",
                Author = "Your Name",
                Company = "Your Company",
                Copyright = "Your Copyright"
            };
        }

        protected override CustomListCollection GetCustomListsOverride()
        {
            return new CustomListCollection
            {
                new HubSpotCustomList(),
            };
        }
    }
}
