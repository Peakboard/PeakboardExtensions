using Peakboard.ExtensionKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeakboardExtensionODataV4
{
    public class ODataV4Extension : ExtensionBase
    {
        public ODataV4Extension(IExtensionHost host)
            : base(host)
        {
        }

        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "OdataV4Extension",
                Name = "Odata V4 Extension",
                Description = "This is an Extension for accessing Odata 4 data.",
                Version = "1.0",
                Author = "Peakboard Team",
                Company = "Peakboard GmbH",
                Copyright = "Copyright © 2021",
            };
        }

        protected override CustomListCollection GetCustomListsOverride()
        {
            return new CustomListCollection
            {
				new ODataV4EntityList()
            };
        }
    }
}
