using BacNetExtension.CustomLists;
using Peakboard.ExtensionKit;
using System.Globalization;

namespace BacNetExtension
{
    public class BacNetExtensionDataSource : ExtensionBase
    {
        public BacNetExtensionDataSource(): base()
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        }

        public BacNetExtensionDataSource(IExtensionHost extensionHost): base(extensionHost)
        {
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
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
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
            return new CustomListCollection
            {
                new BacNetDevicesCustomList(),
                new BacNetObjectsCustomList(),
                new BacNetPropertysCustomList()
            };
        }
    }
}
