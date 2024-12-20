using Peakboard.ExtensionKit;
using RfidReader.CustomLists;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RfidReader
{
    public class RfidReaderEXtension:ExtensionBase
    {
        public RfidReaderEXtension():base()
        {
            
        }
        public RfidReaderEXtension(IExtensionHost host):base(host)
        {
            
        }

        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "RfidReaedr",
                Name = "RfidReader",
                Description = "Rfid Reader",
                Version = "1.0",
                MinVersion = "1.0",
                Author = "Makhsum",
                Company = "Peakboard GmbH",
                Copyright = "Copyright © Peakboard GmbH",
            };
        }

        protected override CustomListCollection GetCustomListsOverride()
        {
            return new CustomListCollection
            {
                new RfidCustomList()
            };
        }
    }
}
