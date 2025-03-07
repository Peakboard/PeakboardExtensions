﻿using Peakboard.ExtensionKit;

namespace POSPrinter
{
    [ExtensionIcon("POSPrinterExtension.pb_datasource_pos_printer.png")]
    public class POSPrinterExtension : ExtensionBase
    {
        public POSPrinterExtension(IExtensionHost host)
            : base(host) { }

        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = nameof(POSPrinterExtension),
                Name = "ESC/POS and ZPL printing",
                Description = "This is a sample implementation for ESC/POS and ZPL printing",
                Version = "1.0",
                Author = "Jürgen Bäurle, Benjamin Sturm",
                Company = "Peakboard GmbH",
                Copyright = "Peakboard GmbH",
            };
        }

        protected override CustomListCollection GetCustomListsOverride()
        {
            return new CustomListCollection
            {
                new ESCPOSPrintCustomList(),
                new ZPLPrintCustomList()
            };
        }
    }
}
