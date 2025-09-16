using Peakboard.ExtensionKit;
using MifareReaderApp.CustomLists;

namespace MifareReaderApp
{
    [ExtensionIcon("MifareReader.pb_datasource_mifare.png")]
    public class MifareReaderExtension : ExtensionBase
    {
        public MifareReaderExtension() : base() { }
        public MifareReaderExtension(IExtensionHost host) : base(host) { }

        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "MifareReaderExtension",
                Name = "MIFARE Card Reader",
                Description = "Reads data from MIFARE Classic 1K/4K cards.",
                Version = "1.0",
                Author = "Benjamin Sturm",
                Company = "Peakboard GmbH",
                Copyright = "Copyright © Peakboard GmbH", 
            };
        }

        protected override CustomListCollection GetCustomListsOverride()
        {
            return new CustomListCollection
            {
                new MifareCardList()
            };
        }
    }
}