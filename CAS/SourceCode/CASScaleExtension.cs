using Peakboard.ExtensionKit;
using CASScaleExtension.CustomLists;

namespace CASScaleExtension
{
    public class CASScaleExtension : ExtensionBase
    {
        public CASScaleExtension()
            : base()
        {
        }

        public CASScaleExtension(IExtensionHost host)
            : base(host)
        {
        }

        protected override ExtensionDefinition GetDefinitionOverride()
        {
            Log?.Verbose("CASScaleExtension.GetDefinitionOverride");

            return new ExtensionDefinition
            {
                ID = "CASScaleExtension",
                Name = "CAS Scale",
                Description = "CAS Scale Extension for Peakboard",
                Version = "1.0",
                MinVersion = "1.0",
                Author = "Benjamin Sturm",
                Company = "Peakboard GmbH",
                Copyright = "Copyright © Peakboard GmbH",
            };
        }

        protected override CustomListCollection GetCustomListsOverride()
        {
            Log?.Verbose("CASScaleExtension.GetCustomListsOverride");

            return new CustomListCollection
            {
                new PdnEcr12CustomList(),
                new PdnEcr14CustomList(),
                new Pb2SerialCustomList(),
                new Pb2BleCustomList()
            };
        }

        protected override void SetupOverride()
        {
            Log?.Verbose("CASScaleExtension.SetupOverride");
        }

        protected override void CleanupOverride()
        {
            Log?.Verbose("CASScaleExtension.CleanupOverride");
        }
    }
}
