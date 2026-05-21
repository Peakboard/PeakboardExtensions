using System;
using System.Diagnostics;
using System.IO;
using Peakboard.ExtensionKit;

namespace DesktopToolbox
{
    [Serializable]
    [CustomListIcon("DesktopToolbox.DesktopToolbox.png")]
    public class DesktopInformationCustomList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "DesktopInformation",
                Name = "Desktop Information",
                Description = "Returns information about the current desktop session and provides utility functions.",
                PropertyInputPossible = false,
                Functions = new CustomListFunctionDefinitionCollection
                {
                    new CustomListFunctionDefinition
                    {
                        Name = "OpenURLInBrowser",
                        Description = "Opens the given URL in the default browser.",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "url",
                                Type = CustomListFunctionParameterTypes.String,
                                Optional = false,
                                Description = "The URL to open in the default browser"
                            },
                        },
                    },
                    new CustomListFunctionDefinition
                    {
                        Name = "WriteTextFile",
                        Description = "Writes the given content to a text file. Creates the file if it does not exist, overwrites it if it does. Returns \"OK\" on success, otherwise the error message.",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "fileName",
                                Type = CustomListFunctionParameterTypes.String,
                                Optional = false,
                                Description = "Full path of the file to write, including the folder (e.g. C:\\Temp\\out.txt)"
                            },
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "content",
                                Type = CustomListFunctionParameterTypes.String,
                                Optional = false,
                                Description = "The text content to write to the file"
                            },
                        },
                        ReturnParameters = new CustomListFunctionReturnParameterDefinitionCollection
                        {
                            new CustomListFunctionReturnParameterDefinition
                            {
                                Name = "result",
                                Type = CustomListFunctionParameterTypes.String,
                                Description = "\"OK\" on success, or the error message on failure"
                            },
                        },
                    }
                }
            };
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            return new CustomListColumnCollection
            {
                new CustomListColumn("WindowsUserName", CustomListColumnTypes.String),
                new CustomListColumn("OSVersion", CustomListColumnTypes.String),
            };
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            var items = new CustomListObjectElementCollection();
            items.Add(new CustomListObjectElement
            {
                { "WindowsUserName", Environment.UserName },
                { "OSVersion", Environment.OSVersion.ToString() },
            });
            return items;
        }

        protected override CustomListExecuteReturnContext ExecuteFunctionOverride(CustomListData data, CustomListExecuteParameterContext context)
        {
            if (context.FunctionName.Equals("OpenURLInBrowser", StringComparison.InvariantCultureIgnoreCase))
            {
                var url = context.Values[0].StringValue;
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd",
                    Arguments = $"/c start \"\" \"{url}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                });
            }
            else if (context.FunctionName.Equals("WriteTextFile", StringComparison.InvariantCultureIgnoreCase))
            {
                var ret = new CustomListExecuteReturnContext();
                ret.Add(WriteTextFile(context.Values[0].StringValue, context.Values[1].StringValue));
                return ret;
            }

            return new CustomListExecuteReturnContext();
        }

        /// <summary>
        /// Writes <paramref name="content"/> to <paramref name="fileName"/> as UTF-8 text,
        /// overwriting any existing file. Returns "OK" on success, or the error message
        /// on failure (never throws back into Peakboard). The resolved absolute path is
        /// written to the extension log, and Windows UAC file virtualization is detected
        /// and reported so a redirected write is never silently mistaken for success.
        /// </summary>
        private string WriteTextFile(string fileName, string content)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    return "fileName is required.";
                }

                // Resolve to an absolute path. A relative path (no drive/root) would be
                // written relative to the Peakboard process's current directory, which is
                // almost never where the caller is looking - this makes the location explicit.
                var fullPath = Path.GetFullPath(fileName.Trim());
                this.Log.Info($"WriteTextFile: requested '{fileName}', resolved to '{fullPath}'.");

                var writtenAtUtc = DateTime.UtcNow;
                File.WriteAllText(fullPath, content ?? string.Empty);

                // Confirm the file is really on disk at the resolved path. If it is not,
                // something removed it right after the write (e.g. antivirus) - report
                // that instead of a misleading "OK".
                if (!File.Exists(fullPath))
                {
                    this.Log.Warning($"WriteTextFile: write reported success but no file at '{fullPath}'.");
                    return $"Write reported success but the file is not present at '{fullPath}'.";
                }

                // Detect Windows UAC file virtualization. A write to a folder the process
                // user cannot actually write to (commonly anything under C:\) is silently
                // redirected to %LOCALAPPDATA%\VirtualStore. The write "succeeds", but the
                // file is NOT at fullPath for Explorer or any other process - which looks
                // exactly like "returns OK but no file in the destination directory".
                var virtualPath = GetVirtualStorePath(fullPath);
                if (!string.IsNullOrEmpty(virtualPath) && File.Exists(virtualPath)
                    && File.GetLastWriteTimeUtc(virtualPath) >= writtenAtUtc.AddSeconds(-2))
                {
                    this.Log.Warning($"WriteTextFile: Windows redirected the write (UAC virtualization) to '{virtualPath}'.");
                    return $"Windows redirected the file to '{virtualPath}' because the folder " +
                           $"'{Path.GetDirectoryName(fullPath)}' is not writable by the Peakboard process. " +
                           "Use a user-writable folder (e.g. under the user profile or a folder you granted write access to).";
                }

                var bytes = new FileInfo(fullPath).Length;
                this.Log.Info($"WriteTextFile: wrote {bytes} bytes to '{fullPath}'.");
                return "OK";
            }
            catch (Exception ex)
            {
                this.Log.Error($"WriteTextFile failed: {ex.Message}");
                return ex.Message;
            }
        }

        /// <summary>
        /// Computes the %LOCALAPPDATA%\VirtualStore equivalent of an absolute local path,
        /// used to detect UAC file virtualization redirection. Returns an empty string if
        /// the path is not on a local drive (e.g. a UNC path), where virtualization never
        /// applies.
        /// </summary>
        private static string GetVirtualStorePath(string fullPath)
        {
            try
            {
                var root = Path.GetPathRoot(fullPath); // e.g. "C:\"
                if (string.IsNullOrEmpty(root) || root.Length < 2 || root[1] != ':')
                {
                    return string.Empty; // UNC or unexpected - virtualization does not apply
                }

                var relative = fullPath.Substring(root.Length); // e.g. "temp\MeineMeldungen.csv"
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (string.IsNullOrEmpty(localAppData))
                {
                    return string.Empty;
                }

                return Path.Combine(localAppData, "VirtualStore", relative);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
