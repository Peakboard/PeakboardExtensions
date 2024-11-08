using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Peakboard.ExtensionKit;
namespace CheckMkExtension
{
    [ExtensionIcon("ServerExtension.icon.png")]
    internal class CheckMkExtension : ExtensionBase
    {
        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "CheckMkExtension",
                Name = "CheckMk",
                Description = "Interface with the Server Service and Host Problems",
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
                new CheckMkExtensionCustomList(),
            };
        }
    }
}
