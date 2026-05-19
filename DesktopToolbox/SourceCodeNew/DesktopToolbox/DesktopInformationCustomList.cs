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
        /// on failure (never throws back into Peakboard).
        /// </summary>
        private static string WriteTextFile(string fileName, string content)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(fileName))
                {
                    return "fileName is required.";
                }

                File.WriteAllText(fileName, content ?? string.Empty);
                return "OK";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
    }
}
