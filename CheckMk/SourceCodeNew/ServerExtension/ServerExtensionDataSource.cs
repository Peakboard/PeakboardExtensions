using Peakboard.ExtensionKit;

namespace CheckMkExtension
{
    [ExtensionIcon("ServerExtension.icon.png")]
    internal class CheckMkExtension : ExtensionBase
    {
        // REQUIRED constructors
        public CheckMkExtension() : base() { }
        public CheckMkExtension(IExtensionHost host) : base(host) { }

        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "CheckMkExtension",
                Name = "CheckMk",
                Description = "Interface with the Server Service and Host Problems",
                Version = "2.0",
                Author = "Makhsum",
                Company = "Peakboard"
            };
        }
        protected override CustomListCollection GetCustomListsOverride()
        {
            return new CustomListCollection
            {
                new CheckMkExtensionCustomList(),
            };
        }
    }
}
