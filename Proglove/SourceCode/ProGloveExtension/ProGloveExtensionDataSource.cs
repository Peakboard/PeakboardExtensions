using Peakboard.ExtensionKit;
using ProGloveExtension.CustomLists;
namespace ProGloveExtension
{
    public class ProGloveExtension : ExtensionBase
    {
        public ProGloveExtension() : base() { }
        public ProGloveExtension(IExtensionHost host) : base(host) { }
        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "ProGloveExtension",
                Name = "ProGlove",
                Description = "Description",
                Version = "1.0",
                Author = "Makhsum",
                Company = "Peakboard",
                Copyright = "Your Copyright"
            };  
        }
        protected override CustomListCollection GetCustomListsOverride()
        {
            return new CustomListCollection
            {
                new ProGloveExtensionEventsList(),
                new ProGloveExtensionGatewaysList(),
                new ProGloveExtensionReportsList()
            };
        }
    }
}
