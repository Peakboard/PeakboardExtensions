using Peakboard.ExtensionKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProGloveExtension.CustomLists;
namespace ProGloveExtension
{
    public class ProGloveExtension : ExtensionBase
    {
        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "ProGloveExtension",
                Name = "ProGlove",
                Description = "Description",
                Version = "1.0",
                Author = "Makhsum",
                Company = "Peakboard",
                Copyright = "Your Copyright"
            };
        }
        protected override CustomListCollection GetCustomListsOverride()
        {
            return new CustomListCollection
            {
              //  new ProGloveExtensionEventsList(),
                new ProGloveExtensionGatewaysList()
            };
        }
    }
}
