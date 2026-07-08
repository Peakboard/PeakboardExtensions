using Peakboard.ExtensionKit;

namespace NetworkFiles;

[ExtensionIcon("NetworkFiles.File.png")]
public class NetworkFilesExtension : ExtensionBase
{
    protected override ExtensionDefinition GetDefinitionOverride()
    {
        return new ExtensionDefinition
        {
            ID = "NetworkFiles",
            Name = "Network files",
            Version = "1.2",
            Author = "Peakboard GmbH",
            Company = "Peakboard GmbH",
            Description = "Reads all files of an UNC path folder (SMB session via IPC$/share root)"
        };
    }

    protected override CustomListCollection GetCustomListsOverride()
    {
        return
        [
            new NetworkFilesCustomList(),
        ];
    }
}