using System.Net;
using Peakboard.ExtensionKit;

namespace NetworkFiles;

[CustomListIcon("NetworkFiles.File.png")]
internal class NetworkFilesCustomList : CustomListBase
{
    protected override CustomListDefinition GetDefinitionOverride()
    {
        return new CustomListDefinition
        {
            ID = $"NetworkFiles",
            Name = "Network files",
            Description = "List all files of a folder",
            PropertyInputPossible = true,
            PropertyInputDefaults = {
                new CustomListPropertyDefinition(){ Name = "Domain", Value = "domain"},
                new CustomListPropertyDefinition(){ Name = "User", Value = "johndoe"},
                new CustomListPropertyDefinition(){ Name = "Password", TypeDefinition = new CustomListPropertyStringTypeDefinition() { Masked = true } },
                new CustomListPropertyDefinition(){ Name = "UNCFolder", Value = @"\\server\folder"},
                new CustomListPropertyDefinition(){ Name = "CheckSubfolders", Value = "False", TypeDefinition = new CustomListPropertyBooleanTypeDefinition()},
                new CustomListPropertyDefinition(){ Name = "AddFolders", Value = "False", TypeDefinition = new CustomListPropertyBooleanTypeDefinition()}
            }
        };
    }

    protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
    {
        data.Properties.TryGetValue("AddFolders", out var addFolders);
        
        if (addFolders == "True")
        {
            return new CustomListColumnCollection
            {
                new CustomListColumn("Path", CustomListColumnTypes.String),
                new CustomListColumn("Name", CustomListColumnTypes.String),
                new CustomListColumn("LastModified", CustomListColumnTypes.String),
                new CustomListColumn("IsFolder", CustomListColumnTypes.Boolean),
            };
        }
        else
        {
            return new CustomListColumnCollection
            {
                new CustomListColumn("Path", CustomListColumnTypes.String),
                new CustomListColumn("Name", CustomListColumnTypes.String),
                new CustomListColumn("LastModified", CustomListColumnTypes.String),
            };
        }
    }

    protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
    {
        var items = new CustomListObjectElementCollection();

        data.Properties.TryGetValue("UNCFolder", out var folder);
        data.Properties.TryGetValue("User", out var user);
        data.Properties.TryGetValue("Password", out var password);
        data.Properties.TryGetValue("Domain", out var domain);
        data.Properties.TryGetValue("CheckSubfolders", out var checkSubfolders);
        data.Properties.TryGetValue("AddFolders", out var addFolders);

        var includeSubfolders = string.Equals(checkSubfolders, "True", StringComparison.OrdinalIgnoreCase);
        var includeFolders = string.Equals(addFolders, "True", StringComparison.OrdinalIgnoreCase);

        var credentials = new NetworkCredential(user, password, domain);

        using (var nc = new NetworkConnection(folder, credentials))
        {
            var root = nc.NetworkName.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            AddFolderContent(items, root, includeSubfolders, includeFolders);
        }

        return items;
    }

    private void AddFolderContent(CustomListObjectElementCollection items, string folderPath, bool includeSubfolders, bool includeFolders)
    {
        if (includeFolders)
        {
            var folderName = Path.GetFileName(folderPath);

            var obj = new CustomListObjectElement
        {
            { "Path", folderPath },
            { "Name", folderName },
            { "LastModified", string.Empty }
        };

            obj.Add("IsFolder", true);

            items.Add(obj);
        }

        if (includeSubfolders)
        {
            foreach (var subFolder in Directory.GetDirectories(folderPath, "*", SearchOption.TopDirectoryOnly))
            {
                AddFolderContent(items, subFolder, includeSubfolders, includeFolders);
            }
        }

        foreach (var file in Directory.GetFiles(folderPath, "*", SearchOption.TopDirectoryOnly))
        {
            var modified = File.GetLastWriteTime(file);

            var obj = new CustomListObjectElement
            {
                { "Path", file },
                { "Name", Path.GetFileName(file) },
                { "LastModified", modified.ToString("yyyyMMddHHmmss") }
            };

            if (includeFolders)
                obj.Add("IsFolder", false);

            items.Add(obj);
        }
    }
}