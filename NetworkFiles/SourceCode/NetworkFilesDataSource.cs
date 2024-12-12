using System;
using Peakboard.ExtensionKit;
using PeakboardExtensionNetworkFiles;

namespace PeakboardExtensionNetworkFiles
{
    [ExtensionIcon("PeakboardExtensionNetworkFiles.File.png")]
    public class NetworkFiles : ExtensionBase
    {
        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "NetworkFiles",
                Name = "Network files",
                Version = "1.0",
                Author = "Peakboard GmbH",
                Company = "Peakboard GmbH",
                Description = "Reads all files of an unc path folder"
            };
        }

        protected override CustomListCollection GetCustomListsOverride()
        {
            return new CustomListCollection
            {
                new NetworkFilesList(),
            };
        }
    }
}
