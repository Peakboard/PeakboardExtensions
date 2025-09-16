using System;
using Peakboard.ExtensionKit;


namespace MPDV
{
    [ExtensionIcon("MPDV.logo.png")]
    public class MPDVExtension : ExtensionBase
    {
        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "MPDV",
                Name = "MPDV Extension",
                Description = "Get your data from MPDV",
                Version = "1.0",
                Author = "Thilo Brosinsky",
                Company = "Peakboard GmbH.",
                Copyright = "Copyright © Peakboard GmbH",
            };
        }

        protected override CustomListCollection GetCustomListsOverride()
        {
            return new CustomListCollection
            {
                new MPDVCustomList(),
            };
        }
    }
}
