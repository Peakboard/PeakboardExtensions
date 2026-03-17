using System;
using System.Diagnostics;
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

            return new CustomListExecuteReturnContext();
        }
    }
}
