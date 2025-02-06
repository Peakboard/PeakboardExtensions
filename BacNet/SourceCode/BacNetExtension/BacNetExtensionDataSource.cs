using BacNetExtension.CustomLists;
using Peakboard.ExtensionKit;

namespace BacNetExtension
{
    public class BacNetExtensionDataSource : ExtensionBase
    {
        public BacNetExtensionDataSource(): base()
        {
            
        }
        public BacNetExtensionDataSource(IExtensionHost extensionHost): base(extensionHost)
        {
            
        }

        protected override ExtensionDefinition GetDefinitionOverride() => new ExtensionDefinition
        { 
            ID = "BacNetExtension", 
            Name = "BacNet",
            Description = "Description",
            Version = "1.0",
            Author = "Makhsum",
            Company = "Peakboard",
            Copyright = "Your Copyright" 
        };
        protected override CustomListCollection GetCustomListsOverride()
        {
            return new CustomListCollection
            {
                new BacNetDevicesCustomList(),
                new BacNetObjectsCustomList(),
                new BacNetPropertysCustomList()
            };
        }
    }
}
