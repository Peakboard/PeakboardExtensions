using System;
using Peakboard.ExtensionKit;


namespace PeakboardExtensionMD5
{
    public class MD5Extension : ExtensionBase
    {
        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "MD5",
                Name = "MD5",
                Description = "MD5",
                Version = "1.0",
                Author = "Thilo Brosinsky",
                Company = "Peakboard GmbH"
            };
        }

        protected override CustomListCollection GetCustomListsOverride()
        {
            return new CustomListCollection
            {
                new MD5CustomList(),
            };
        }
    }
}
