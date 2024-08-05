using System;
using Peakboard.ExtensionKit;
using GETT_CapDeviceLib;
using GETT_CapDeviceLib.Parameter;
using System.Threading;
using System.Globalization;


namespace GettKeyConfigExtension
{
    [Serializable]
    [CustomListIcon("GettKeyConfigExtension.pb_datasource_gett.png")]
    class GettKeyConfigExtensionCustomList : CustomListBase
    {
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
                }
            };
        }

        protected override CustomListExecuteReturnContext ExecuteFunctionOverride(CustomListData data, CustomListExecuteParameterContext context)
        {
            var ret = new CustomListExecuteReturnContext();
            GETT_CapDevice deviceHandle;
            deviceHandle = GETT_CapDevice.Instance;
            Thread.Sleep(1500);

            if (context.FunctionName.Equals("SetKeyColor", StringComparison.InvariantCultureIgnoreCase))
            {
                if (deviceHandle.IsConnected)
                {
                    int keyIdx = Int32.Parse(context.Values[0].StringValue) - 1;

                    byte[] rgb = CalculateRGB(context.Values[1].StringValue);

                    byte r = rgb[0];
                    byte g = rgb[1];
                    byte b = rgb[2];

                    deviceHandle.SetColor(keyIdx, eCapDevice_KeyState.OFF, new CapDevice_RGBColor(r, g, b));
                }
            }
            if (context.FunctionName.Equals("SetBlinkMode_Click", StringComparison.InvariantCultureIgnoreCase))
            {
                if (deviceHandle.IsConnected)
                {
                    int keyIdx = Int32.Parse(context.Values[0].StringValue) - 1;

                    byte[] rgbOff = CalculateRGB(context.Values[1].StringValue);

                    byte rOff = rgbOff[0];
                    byte gOff = rgbOff[1];
                    byte bOff = rgbOff[2];

                    byte[] rgbDelay = CalculateRGB(context.Values[2].StringValue);

                    byte rDelay = rgbDelay[0];
                    byte gDelay = rgbDelay[1];
                    byte bDelay = rgbDelay[2];

                    int delay = Int32.Parse(context.Values[3].StringValue);

                    deviceHandle.SetColor(keyIdx, eCapDevice_KeyState.OFF, new CapDevice_RGBColor(rOff, gOff, bOff));
                    deviceHandle.SetColor(keyIdx, eCapDevice_KeyState.DELAY, new CapDevice_RGBColor(rDelay, gDelay, bDelay));
                    deviceHandle.SetFlashDuration(keyIdx, eCapDevice_KeyState.OFF, TimeSpan.FromMilliseconds(delay));

                }
            }
            if (context.FunctionName.Equals("StopBlinkMode_Click", StringComparison.InvariantCultureIgnoreCase))
            {
                if (deviceHandle.IsConnected)
                {
                    int keyIdx = Int32.Parse(context.Values[0].StringValue) - 1;
                    deviceHandle.SetFlashDuration(keyIdx, eCapDevice_KeyState.OFF, TimeSpan.Zero);
                }
            }
            if (context.FunctionName.Equals("ResetSettings_Click", StringComparison.InvariantCultureIgnoreCase))
            {
                if (deviceHandle.IsConnected)
                {
                    deviceHandle.FactoryReset();
                }
            }
            if (context.FunctionName.Equals("SetSwitchMode_Click", StringComparison.InvariantCultureIgnoreCase))
            {
                if (deviceHandle.IsConnected)
                {
                    int keyIdx = Int32.Parse(context.Values[0].StringValue) -1 ;

                    byte[] rgbOff = CalculateRGB(context.Values[1].StringValue);

                    byte rOff = rgbOff[0];
                    byte gOff = rgbOff[1];
                    byte bOff = rgbOff[2];

                    byte[] rgbOn = CalculateRGB(context.Values[2].StringValue);

                    byte rOn = rgbOn[0];
                    byte gOn = rgbOn[1];
                    byte bOn = rgbOn[2];

                    deviceHandle.SetColor(keyIdx, eCapDevice_KeyState.OFF, new CapDevice_RGBColor(rOff, gOff, bOff));
                    deviceHandle.SetColor(keyIdx, eCapDevice_KeyState.ON, new CapDevice_RGBColor(rOn, gOn, bOn));
                    deviceHandle.SetKeyMode(keyIdx, eKeyMode.SWITCH);
                }
            }
            if (context.FunctionName.Equals("SetButtonMode_Click", StringComparison.InvariantCultureIgnoreCase))
            {
                if (deviceHandle.IsConnected)
                {
                    int keyIdx = Int32.Parse(context.Values[0].StringValue) -1;
                    deviceHandle.SetKeyMode(keyIdx, eKeyMode.BUTTON);
                }
            }
            if (context.FunctionName.Equals("SetOnDelay_Click", StringComparison.InvariantCultureIgnoreCase))
            {
                if (deviceHandle.IsConnected)
                {
                    int KeyIdx = Int32.Parse(context.Values[0].StringValue) -1;

                    byte[] rgbOff = CalculateRGB(context.Values[1].StringValue);

                    byte rOff = rgbOff[0];
                    byte gOff = rgbOff[1];
                    byte bOff = rgbOff[2];

                    byte[] rgbDelay = CalculateRGB(context.Values[2].StringValue);

                    byte rDelay = rgbDelay[0];
                    byte gDelay = rgbDelay[1];
                    byte bDelay = rgbDelay[2];

                    int delay = Int32.Parse(context.Values[3].StringValue);

                    byte[] rgbOn = CalculateRGB(context.Values[4].StringValue);

                    byte rOn = rgbOn[0];
                    byte gOn = rgbOn[1];
                    byte bOn = rgbOn[2];

                    deviceHandle.SetColor(KeyIdx, eCapDevice_KeyState.OFF, new CapDevice_RGBColor(rOff, gOff, bOff));
                    deviceHandle.SetColor(KeyIdx, eCapDevice_KeyState.DELAY, new CapDevice_RGBColor(rDelay, gDelay, bDelay));
                    deviceHandle.SetFlashDuration(KeyIdx, eCapDevice_KeyState.DELAY, TimeSpan.FromMilliseconds(delay));
                    deviceHandle.SetColor(KeyIdx, eCapDevice_KeyState.ON, new CapDevice_RGBColor(rOn, gOn, bOff));
                    deviceHandle.SetOnDelay(KeyIdx, TimeSpan.FromSeconds(1));
                }
            }
            if (context.FunctionName.Equals("RestOnDelay_Click", StringComparison.InvariantCultureIgnoreCase))
            {
                if (deviceHandle.IsConnected)
                {
                    int KeyIdx = Int32.Parse(context.Values[0].StringValue) -1;

                    deviceHandle.SetFlashDuration(KeyIdx, eCapDevice_KeyState.DELAY, TimeSpan.Zero);
                    deviceHandle.SetOnDelay(KeyIdx, TimeSpan.Zero);//default delay = 100 ms
                }
            }

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
            items.Add(new CustomListObjectElement { { "Dummy", true}});
            return items;
        }

        private byte[] CalculateRGB(String hexString)
        {
            if (hexString.IndexOf('#') != -1) hexString = hexString.Replace("#", "");            
            byte r,g,b = 0;             
            r = byte.Parse(hexString.Substring(0, 2), NumberStyles.AllowHexSpecifier);             
            g = byte.Parse(hexString.Substring(2, 2), NumberStyles.AllowHexSpecifier);             
            b = byte.Parse(hexString.Substring(4, 2), NumberStyles.AllowHexSpecifier); 
            byte[] returnArray = { r, g, b };
            return returnArray;
        }
    }
}
