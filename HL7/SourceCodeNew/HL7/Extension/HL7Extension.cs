using Peakboard.ExtensionKit;

namespace PeakboardExtensionHL7.Extension
{
    [ExtensionIcon("PeakboardExtensionHL7.HL7.png")]
    public class HL7Extension : ExtensionBase
    {
        public HL7Extension() : base() { }
        public HL7Extension(IExtensionHost host) : base(host) { }

        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "PeakboardExtensionHL7",
                Name = "HL7",
                Description = "Receives HL7 v2 messages over MLLP, filtered by message type, segment type and patient.",
                Version = "1.0",
                MinVersion = "1.0",
                Author = "Peakboard",
                Company = "Peakboard GmbH",
                Copyright = "Copyright © Peakboard GmbH",
            };
        }

        protected override CustomListCollection GetCustomListsOverride()
        {
            return new CustomListCollection
            {
                new SegmentsCustomList(),
                new MessagesCustomList(),
            };
        }

        protected override void SetupOverride() { }
        protected override void CleanupOverride() { }
    }
}
