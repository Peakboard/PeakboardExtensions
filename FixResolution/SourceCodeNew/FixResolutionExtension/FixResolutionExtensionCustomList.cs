using System;
using System.Runtime.InteropServices;
using System.Threading;
using Peakboard.ExtensionKit;

namespace FixResolutionExtension
{
    [Serializable]
    class FixResolutionExtensionCustomList : CustomListBase
    {
        const int TIMEOUT = 200;                  // Kurze Pause zwischen den Tastendrücken
        const int KEYEVENTF_KEYDOWN = 0x0000;     // Flag für Key-Down-Event
        const int KEYEVENTF_KEYUP = 0x0002;       // Flag für Key-Up-Event
        const byte VK_MENU = 0x12;                // Virtuelle Taste für Alt
        const byte VK_SPACE = 0x20;               // Virtuelle Taste für Leertaste
        const byte VK_DOWN = 0x28;                // Virtuelle Taste für Pfeil nach unten
        const byte VK_ENTER = 0x0D;               // Virtuelle Taste für Enter
        const byte VK_LWIN = 0x5B;                // Virtuelle Taste für linke Windows-Taste
        const byte VK_UP = 0x26;                  // Virtuelle Taste für Pfeil nach oben

        // Import the necessary functions from user32.dll
        [DllImport("user32.dll", SetLastError = true)]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = $"FixResolutionExtensionList",
                Name = "FixResolution",
                PropertyInputPossible = true,
                Functions = new CustomListFunctionDefinitionCollection
                {
                    new CustomListFunctionDefinition()
                    {
                        Name = "FixResolution",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            },
                    },
                }
            };
        }

        protected override CustomListExecuteReturnContext ExecuteFunctionOverride(
            CustomListData data,
            CustomListExecuteParameterContext context
        )
        {

            var ret = new CustomListExecuteReturnContext();

            if (
                context.FunctionName.Equals(
                    "FixResolution",
                    StringComparison.InvariantCultureIgnoreCase
                )
            )
            {
                try
                {
                    // Win + Down drücken
                    keybd_event(VK_LWIN, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero); // Win drücken
                    Thread.Sleep(TIMEOUT);
                    keybd_event(VK_DOWN, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);   // Pfeil nach oben drücken
                    Thread.Sleep(TIMEOUT);
                    keybd_event(VK_DOWN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);     // Pfeil nach oben loslassen
                    keybd_event(VK_LWIN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);   // Win loslassen

                    Thread.Sleep(TIMEOUT);

                    // Win + Up drücken
                    keybd_event(VK_LWIN, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero); // Win drücken
                    Thread.Sleep(TIMEOUT);
                    keybd_event(VK_UP, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);   // Pfeil nach oben drücken
                    Thread.Sleep(TIMEOUT);
                    keybd_event(VK_UP, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);     // Pfeil nach oben loslassen
                    keybd_event(VK_LWIN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);   // Win loslassen
                }
                catch (Exception ex) { }
            }
            return ret;
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            return new CustomListColumnCollection
            {
                new CustomListColumn("Dummy", CustomListColumnTypes.Boolean)
            };
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            var items = new CustomListObjectElementCollection();
            items.Add(new CustomListObjectElement { { "Dummy", true } });
            return items;
        }
    }
}
