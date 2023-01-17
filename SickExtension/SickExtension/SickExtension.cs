using Peakboard.ExtensionKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SickExtension
{
    [ExtensionIcon("Peakboard.Extensions.SickExtension.Sick.png")]
    public class SickExtension : ExtensionBase
    {
        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "Peakboard.Extensions.SickExtension",
                Name = "Sick",
                Description = "This is an Extension for accessing a Sick sensor.",
                Version = "1.0",
                Author = "Peakboard Team",
                Company = "Peakboard GmbH",
                Copyright = "Copyright © 2022"
            };
        }

        protected override CustomListCollection GetCustomListsOverride()
        {
            return new CustomListCollection
            {
                new SickListBase()
            };
        }
    }
}
