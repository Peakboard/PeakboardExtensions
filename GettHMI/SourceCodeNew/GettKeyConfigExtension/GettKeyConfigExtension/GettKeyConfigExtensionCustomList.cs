using System.Globalization;
using System.Runtime.InteropServices;
using GETT_CapDeviceLib;
using GETT_CapDeviceLib.Parameter;
using GettKeyConfigExtension.Helper;
using Peakboard.ExtensionKit;

namespace GettKeyConfigExtension
{
    [Serializable]
    [CustomListIcon("GettKeyConfigExtension.pb_datasource_gett.png")]
    class GettKeyConfigExtensionCustomList : CustomListBase
    {
        GETT_CapDevice deviceHandle;

        const int TIMEOUT = 2000;
        const int KEYEVENTF_KEYDOWN = 0x0000;
        const int KEYEVENTF_KEYUP = 0x0002;
        const byte VK_LWIN = 0x5B;
        const byte VK_D = 0x44;

        // Import the necessary functions from user32.dll
        [DllImport("user32.dll", SetLastError = true)]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = $"GettKeyConfigExtensionList",
                Name = "Gett HMI Keys",
                PropertyInputPossible = true,
                Functions = new CustomListFunctionDefinitionCollection
                {
                    new CustomListFunctionDefinition()
                    {
                        Name = "SetKeyColor",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "Key Number",
                                Description = "Select your Key",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.Number
                            },
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "HexCode",
                                Description = "Set your color as Hex-Code",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.String
                            }
                        },
                    },
                    new CustomListFunctionDefinition()
                    {
                        Name = "SetMultipleKeysColor",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "HexCodeKey1",
                                Description = "Set your color as Hex-Code",
                                Optional = true,
                                Type = CustomListFunctionParameterTypes.String
                            },
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "HexCodeKey2",
                                Description = "Set your color as Hex-Code",
                                Optional = true,
                                Type = CustomListFunctionParameterTypes.String
                            },
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "HexCodeKey3",
                                Description = "Set your color as Hex-Code",
                                Optional = true,
                                Type = CustomListFunctionParameterTypes.String
                            },
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "HexCodeKey4",
                                Description = "Set your color as Hex-Code",
                                Optional = true,
                                Type = CustomListFunctionParameterTypes.String
                            },
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "HexCodeKey5",
                                Description = "Set your color as Hex-Code",
                                Optional = true,
                                Type = CustomListFunctionParameterTypes.String
                            },
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "HexCodeKey6",
                                Description = "Set your color as Hex-Code",
                                Optional = true,
                                Type = CustomListFunctionParameterTypes.String
                            }
                        },
                    },
                    new CustomListFunctionDefinition()
                    {
                        Name = "SetBlinkMode_Click",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "Key Number",
                                Description = "Select your Key",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.Number
                            },
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "HexOff",
                                Description = "Set your off color as Hex-Code",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.String
                            },
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "HexDelay",
                                Description = "Set your delay color as Hex-Code",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.String
                            },
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "Delay",
                                Description = "Set your delay in ms",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.Number
                            }
                        },
                    },
                    new CustomListFunctionDefinition()
                    {
                        Name = "StopBlinkMode_Click",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "Key Number",
                                Description = "Select your Key",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.Number
                            }
                        },
                    },
                    new CustomListFunctionDefinition()
                    {
                        Name = "ResetSettings_Click",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            },
                    },
                    new CustomListFunctionDefinition()
                    {
                        Name = "SetSwitchMode_Click",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "Key Number",
                                Description = "Select your Key",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.Number
                            },
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "HexOff",
                                Description = "Set your off color as Hex-Code",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.String
                            },
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "HexOn",
                                Description = "Set your on color as Hex-Code",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.String
                            }
                        },
                    },
                    new CustomListFunctionDefinition()
                    {
                        Name = "SetButtonMode_Click",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "Key Number",
                                Description = "Select your Key",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.Number
                            }
                        },
                    },
                    new CustomListFunctionDefinition()
                    {
                        Name = "SetOnDelay_Click",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "Key Number",
                                Description = "Select your Key",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.Number
                            },
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "HexOff",
                                Description = "Set your off color as Hex-Code",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.String
                            },
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "HexDelay",
                                Description = "Set your delay color as Hex-Code",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.String
                            },
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "Delay",
                                Description = "Set your delay in ms",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.Number
                            },
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "HexOn",
                                Description = "Set your on color as Hex-Code",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.String
                            }
                        },
                    },
                    new CustomListFunctionDefinition()
                    {
                        Name = "RestOnDelay_Click",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "Key Number",
                                Description = "Select your Key",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.Number
                            }
                        },
                    },
                    new CustomListFunctionDefinition()
                    {
                        Name = "ShowDesktop",
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
            Thread THR1 = new Thread(connect);
            THR1.Start();
            var ret = new CustomListExecuteReturnContext();

            if (
                context.FunctionName.Equals(
                    "ShowDesktop",
                    StringComparison.InvariantCultureIgnoreCase
                )
            )
            {
                try
                {
                    // Simulate Win key down
                    keybd_event(VK_LWIN, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                    // Simulate 'D' key down
                    keybd_event(VK_D, 0, KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                    // Simulate 'D' key up
                    keybd_event(VK_D, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                    // Simulate Win key up
                    keybd_event(VK_LWIN, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
                }
                catch (Exception ex) { }
            }
            if (
                context.FunctionName.Equals(
                    "SetKeyColor",
                    StringComparison.InvariantCultureIgnoreCase
                )
            )
            {
                KeyColor keyColor = new KeyColor();

                keyColor.key = Int32.Parse(context.Values[0].StringValue) - 1;
                keyColor.rgb = CalculateRGB(context.Values[1].StringValue);

                THR1.Join();

                if (deviceHandle.IsConnected)
                {
                    deviceHandle.SetColor(
                        keyColor.key,
                        eCapDevice_KeyState.OFF,
                        new CapDevice_RGBColor(keyColor.rgb[0], keyColor.rgb[1], keyColor.rgb[2])
                    );
                }
            }
            if (
                context.FunctionName.Equals(
                    "SetMultipleKeysColor",
                    StringComparison.InvariantCultureIgnoreCase
                )
            )
            {
                KeyColor[] keyColors = new KeyColor[6];

                for (int i = 0; i < keyColors.Length; i++)
                {
                    keyColors[i] = new KeyColor();
                    keyColors[i].key = i;
                    keyColors[i].rgb = CalculateRGB(context.Values[i].StringValue);
                }

                THR1.Join();

                if (deviceHandle.IsConnected)
                {
                    for (int i = 0; i < keyColors.Length; i++)
                    {

                        if (keyColors[i] != null)
                        {
                            deviceHandle.SetColor(
                                keyColors[i].key,
                                eCapDevice_KeyState.OFF,
                                new CapDevice_RGBColor(
                                    keyColors[i].rgb[0],
                                    keyColors[i].rgb[1],
                                    keyColors[i].rgb[2]
                                )
                            );
                        }
                    }
                }
            }
            if (
                context.FunctionName.Equals(
                    "SetBlinkMode_Click",
                    StringComparison.InvariantCultureIgnoreCase
                )
            )
            {
                KeyColor[] keyColors = new KeyColor[2];

                keyColors[0].key = Int32.Parse(context.Values[0].StringValue) - 1;
                keyColors[0].rgb = CalculateRGB(context.Values[1].StringValue);

                keyColors[1].key = Int32.Parse(context.Values[0].StringValue) - 1;
                keyColors[1].rgb = CalculateRGB(context.Values[2].StringValue);

                int delay = Int32.Parse(context.Values[3].StringValue);

                THR1.Join();

                if (deviceHandle.IsConnected)
                {
                    deviceHandle.SetColor(
                        keyColors[0].key,
                        eCapDevice_KeyState.OFF,
                        new CapDevice_RGBColor(
                            keyColors[0].rgb[0],
                            keyColors[0].rgb[1],
                            keyColors[0].rgb[2]
                        )
                    );
                    deviceHandle.SetColor(
                        keyColors[1].key,
                        eCapDevice_KeyState.DELAY,
                        new CapDevice_RGBColor(
                            keyColors[1].rgb[0],
                            keyColors[1].rgb[1],
                            keyColors[1].rgb[2]
                        )
                    );
                    deviceHandle.SetFlashDuration(
                        keyColors[0].key,
                        eCapDevice_KeyState.OFF,
                        TimeSpan.FromMilliseconds(delay)
                    );
                }
            }
            if (
                context.FunctionName.Equals(
                    "StopBlinkMode_Click",
                    StringComparison.InvariantCultureIgnoreCase
                )
            )
            {
                int keyIdx = Int32.Parse(context.Values[0].StringValue) - 1;
                THR1.Join();

                if (deviceHandle.IsConnected)
                {
                    deviceHandle.SetFlashDuration(keyIdx, eCapDevice_KeyState.OFF, TimeSpan.Zero);
                }
            }
            if (
                context.FunctionName.Equals(
                    "ResetSettings_Click",
                    StringComparison.InvariantCultureIgnoreCase
                )
            )
            {
                THR1.Join();

                if (deviceHandle.IsConnected)
                {
                    deviceHandle.FactoryReset();
                }
            }
            if (
                context.FunctionName.Equals(
                    "SetSwitchMode_Click",
                    StringComparison.InvariantCultureIgnoreCase
                )
            )
            {
                KeyColor[] keyColors = new KeyColor[2];

                keyColors[0].key = Int32.Parse(context.Values[0].StringValue) - 1;
                keyColors[0].rgb = CalculateRGB(context.Values[1].StringValue);

                keyColors[1].key = Int32.Parse(context.Values[0].StringValue) - 1;
                keyColors[1].rgb = CalculateRGB(context.Values[2].StringValue);

                THR1.Join();

                if (deviceHandle.IsConnected)
                {
                    deviceHandle.SetColor(
                        keyColors[0].key,
                        eCapDevice_KeyState.OFF,
                        new CapDevice_RGBColor(
                            keyColors[0].rgb[0],
                            keyColors[0].rgb[1],
                            keyColors[0].rgb[2]
                        )
                    );
                    deviceHandle.SetColor(
                        keyColors[1].key,
                        eCapDevice_KeyState.ON,
                        new CapDevice_RGBColor(
                            keyColors[1].rgb[0],
                            keyColors[1].rgb[1],
                            keyColors[1].rgb[2]
                        )
                    );
                    deviceHandle.SetKeyMode(keyColors[0].key, eKeyMode.SWITCH);
                }
            }
            if (
                context.FunctionName.Equals(
                    "SetButtonMode_Click",
                    StringComparison.InvariantCultureIgnoreCase
                )
            )
            {
                int keyIdx = Int32.Parse(context.Values[0].StringValue) - 1;

                THR1.Join();

                if (deviceHandle.IsConnected)
                {
                    deviceHandle.SetKeyMode(keyIdx, eKeyMode.BUTTON);
                }
            }
            if (
                context.FunctionName.Equals(
                    "SetOnDelay_Click",
                    StringComparison.InvariantCultureIgnoreCase
                )
            )
            {
                KeyColor[] keyColors = new KeyColor[3];

                keyColors[0].key = Int32.Parse(context.Values[0].StringValue) - 1;
                keyColors[0].rgb = CalculateRGB(context.Values[1].StringValue); //Off

                keyColors[1].key = Int32.Parse(context.Values[0].StringValue) - 1;
                keyColors[1].rgb = CalculateRGB(context.Values[2].StringValue); //Delay

                keyColors[2].key = Int32.Parse(context.Values[0].StringValue) - 1;
                keyColors[2].rgb = CalculateRGB(context.Values[4].StringValue); //On

                int delay = Int32.Parse(context.Values[3].StringValue);

                THR1.Join();

                if (deviceHandle.IsConnected)
                {
                    deviceHandle.SetColor(
                        keyColors[0].key,
                        eCapDevice_KeyState.OFF,
                        new CapDevice_RGBColor(
                            keyColors[0].rgb[0],
                            keyColors[0].rgb[1],
                            keyColors[0].rgb[2]
                        )
                    );
                    deviceHandle.SetColor(
                        keyColors[1].key,
                        eCapDevice_KeyState.DELAY,
                        new CapDevice_RGBColor(
                            keyColors[1].rgb[0],
                            keyColors[1].rgb[1],
                            keyColors[1].rgb[2]
                        )
                    );
                    deviceHandle.SetFlashDuration(
                        keyColors[0].key,
                        eCapDevice_KeyState.DELAY,
                        TimeSpan.FromMilliseconds(delay)
                    );
                    deviceHandle.SetColor(
                        keyColors[2].key,
                        eCapDevice_KeyState.ON,
                        new CapDevice_RGBColor(
                            keyColors[2].rgb[0],
                            keyColors[2].rgb[1],
                            keyColors[2].rgb[2]
                        )
                    );
                    deviceHandle.SetOnDelay(keyColors[0].key, TimeSpan.FromSeconds(1));
                }
            }
            if (
                context.FunctionName.Equals(
                    "RestOnDelay_Click",
                    StringComparison.InvariantCultureIgnoreCase
                )
            )
            {
                int KeyIdx = Int32.Parse(context.Values[0].StringValue) - 1;

                THR1.Join();

                if (deviceHandle.IsConnected)
                {
                    deviceHandle.SetFlashDuration(KeyIdx, eCapDevice_KeyState.DELAY, TimeSpan.Zero);
                    deviceHandle.SetOnDelay(KeyIdx, TimeSpan.Zero); //default delay = 100 ms
                }
            }

            if (deviceHandle.IsConnected)
                deviceHandle.Dispose();
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

        private byte[] CalculateRGB(String hexString)
        {
            if (hexString.Length < 6) { return null; }

            if (hexString.IndexOf('#') != -1)
                hexString = hexString.Replace("#", "");
            byte r,
                g,
                b = 0;
            r = byte.Parse(hexString.Substring(0, 2), NumberStyles.AllowHexSpecifier);
            g = byte.Parse(hexString.Substring(2, 2), NumberStyles.AllowHexSpecifier);
            b = byte.Parse(hexString.Substring(4, 2), NumberStyles.AllowHexSpecifier);
            byte[] returnArray = { r, g, b };
            return returnArray;
        }

        private void connect()
        {
            int run = 0;
            deviceHandle = GETT_CapDevice.Instance;
            while (!deviceHandle.IsConnected && run <= TIMEOUT)
            {
                Thread.Sleep(1);
                run++;
            }
        }
    }
}