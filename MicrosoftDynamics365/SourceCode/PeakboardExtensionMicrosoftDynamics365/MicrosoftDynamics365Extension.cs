using Peakboard.ExtensionKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeakboardExtensionMicrosoftDynamics365
{
    [ExtensionIcon("PeakboardExtensionMicrosoftDynamics365.d365Icon.png")]
    
    public class MicrosoftDynamics365Extension : ExtensionBase
    {
        public MicrosoftDynamics365Extension(IExtensionHost host)
            : base(host)
        {
        }

        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "MicrosoftDynamics365",
                Name = "Microsoft Dynamics 365",
                Description = "This is an Extension for accessing Microsoft Dynamics 365 data.",
                Version = "2.0",
                Author = "Peakboard Team",
                Company = "Peakboard GmbH",
                Copyright = "Copyright © 2023",
            };
        }

        protected override CustomListCollection GetCustomListsOverride()
        {
            return new CustomListCollection
            {
                new CrmList()
            };
        }
    }
}
