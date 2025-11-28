using Peakboard.ExtensionKit;

namespace AcmeData
{
    public class AcmeDataExtension : ExtensionBase
    {
        public AcmeDataExtension(IExtensionHost host) : base(host) 
        { 
        
        }
        
        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "AcmeData",
                Name = "Acme Data Extension",
                Description = "Generates random sample data for demonstration purposes",
                Version = "1.0",
                Author = "Peakboard Assistant",
                Company = "Peakboard GmbH.",
                Copyright = "Copyright © Peakboard GmbH",
            };
        }

        protected override CustomListCollection GetCustomListsOverride()
        {
            return new CustomListCollection
            {
                new AcmeDataCustomList(),
            };
        }
    }
}

