using System;
using System.IO;
using System.Net;
using Peakboard.ExtensionKit;

namespace PeakboardExtensionNetworkFiles
{
    [Serializable]
    [CustomListIcon("PeakboardExtensionNetworkFiles.File.png")]
    internal class NetworkFilesList : CustomListBase
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
                    new CustomListPropertyDefinition(){ Name = "Password", Masked = true},
                    new CustomListPropertyDefinition(){ Name = "UNCFolder", Value = @"\\server\folder"},
                    new CustomListPropertyDefinition(){ Name = "Check subfolders", Value = "False"}
                }
            };
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            return new CustomListColumnCollection
            {
                new CustomListColumn("Path", CustomListColumnTypes.String),
                new CustomListColumn("Name", CustomListColumnTypes.String),
                new CustomListColumn("LastModified", CustomListColumnTypes.String),
            };
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            var items = new CustomListObjectElementCollection();
            data.Properties.TryGetValue("UNCFolder", out var folder);
            data.Properties.TryGetValue("User", out var user);
            data.Properties.TryGetValue("Password", out var password);
            data.Properties.TryGetValue("Domain", out var domain);
            data.Properties.TryGetValue("Check subfolders", out var checkSubfolders);
            var credentials = new NetworkCredential(user, password, domain);

            using (var nc = new NetworkConnection(folder, credentials))
            {
                foreach (var file in Directory.GetFiles(nc.NetworkName, "*", checkSubfolders == "True" ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
                {
                    var modified = File.GetLastWriteTime(file);
                    items.Add(new CustomListObjectElement() {{"Path", file}, {"Name", Path.GetFileName(file)}, { "LastModified", modified.ToString("yyyyMMddHHmmss") } });
                }
            }

            return items;
        }
    }
}