using SerialBarcodeScanner.CustomLists;
using Peakboard.ExtensionKit;

namespace SerialBarcodeScanner
{
    [ExtensionIcon("SerialBarcodeScanner.pb_datasource_barcode.png")]
    public class SerialBarcodeScannerExtension : ExtensionBase
    {
        public SerialBarcodeScannerExtension()
            : base()
        {
        }

        public SerialBarcodeScannerExtension(IExtensionHost host)
            : base(host)
        {
        }

        protected override ExtensionDefinition GetDefinitionOverride()
        {
            Log?.Verbose("SerialBarcodeScannerExtension.GetDefinitionOverride");

            return new ExtensionDefinition
            {
                ID = "SerialBarcodeScannerExtension",
                Name = "Barcode Scanner",
                Description = "Barcode Scanner Extension for Peakboard",
                Version = "1.0",
                MinVersion = "1.0",
                Author = "Benjamin Sturm",
                Company = "Peakboard GmbH",
                Copyright = "Copyright © Peakboard GmbH",
            };
        }

        protected override CustomListCollection GetCustomListsOverride()
        {
            Log?.Verbose("SerialBarcodeScannerExtension.GetCustomListsOverride");

            return new CustomListCollection
            {
                new SerialBarcodeScannerList()
            };
        }

        protected override void SetupOverride()
        {
            Log?.Verbose("SerialBarcodeScannerExtension.SetupOverride");
        }

        protected override void CleanupOverride()
        {
            Log?.Verbose("SerialBarcodeScannerExtension.CleanupOverride");
        }
    }
}
