using Peakboard.ExtensionKit;

namespace Ping
{
    [ExtensionIcon("Ping.Ping.png")]
    public class PingExtension : ExtensionBase
    {
        public PingExtension() : base() { }
        public PingExtension(IExtensionHost host) : base(host) { }

        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "Ping",
                Name = "Ping Extension",
                Description = "Pings a device by IP address or hostname and returns the reachability status.",
                Version = "1.0",
                MinVersion = "1.0",
                Author = "Patrick",
                Company = "Peakboard GmbH",
                Copyright = "Copyright © Peakboard GmbH",
            };
        }

        protected override CustomListCollection GetCustomListsOverride()
        {
            return new CustomListCollection
            {
                new PingCustomList(),
            };
        }
    }
}
