using System;
using System.Collections.Generic;
using Peakboard.ExtensionKit;
using System.Windows;
using static PeakboardExtensionHue.HueHelper;

namespace PeakboardExtensionHue
{
    [CustomListIcon("PeakboardExtensionHue.lightbulb.png")]
    [Serializable]
    class HueLightsCustomList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            // Create a list definition
            return new CustomListDefinition
            {
                ID = $"hueLightsCustomList",
                Name = "Hue Lights",
                Description = "Enables access to Hue lights",
                PropertyInputPossible = true,
                Functions = new CustomListFunctionDefinitionCollection
                {
                    new CustomListFunctionDefinition
                    {
                        Name = "switchlighton",
                        Description = "Switches a light on",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "nameoflight",
                                Type = CustomListFunctionParameterTypes.String,
                                Optional = false,
                                Description = "The name of the light bulb as indicated in the lights list"
                            },
                        },
                    },
                    new CustomListFunctionDefinition
                    {
                        Name = "switchlightoff",
                        Description = "Switches a light off",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "nameoflight",
                                Type = CustomListFunctionParameterTypes.String,
                                Optional = false,
                                Description = "The name of the light bulb as indicated in the lights list"
                            },
                        },
                    },
                                        new CustomListFunctionDefinition
                    {
                        Name = "setlightbrightness",
                        Description = "Sets the brightness of the light (0-254)",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "nameoflight",
                                Type = CustomListFunctionParameterTypes.String,
                                Optional = false,
                                Description = "The name of the light bulb as indicated in the lights list"
                            },
                                                        new CustomListFunctionInputParameterDefinition
                            {
                                Name = "brightness",
                                Type = CustomListFunctionParameterTypes.Number,
                                Optional = false,
                                Description = "Brightness of the light bulb (0-254)"
                            },
                        },
                    },
                }
            };
        }

        protected override FrameworkElement GetControlOverride()
        {
            // return an instance of the UI user control
            return new HueUIControl();
        }

        protected override void CheckDataOverride(CustomListData data)
        {
            data.Properties.TryGetValue("BridgeIP", StringComparison.OrdinalIgnoreCase, out var BridgeIP);
            data.Properties.TryGetValue("UserName", StringComparison.OrdinalIgnoreCase, out var UserName);

            if (string.IsNullOrWhiteSpace(BridgeIP))
            {
                throw new InvalidOperationException("Please provide a Bridge IP");
            }
            if (string.IsNullOrWhiteSpace(UserName))
            {
                throw new InvalidOperationException("Please provide a User Name");
            }
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            // create a static collection of columns 
            return new CustomListColumnCollection
            {
                new CustomListColumn("Id", CustomListColumnTypes.String),
                new CustomListColumn("Name", CustomListColumnTypes.String),
                new CustomListColumn("Type", CustomListColumnTypes.String),
                new CustomListColumn("ProductName", CustomListColumnTypes.String),
                new CustomListColumn("SwitchedOn", CustomListColumnTypes.Boolean),
                new CustomListColumn("Brightness", CustomListColumnTypes.Number),
            };
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            data.Properties.TryGetValue("BridgeIP", StringComparison.OrdinalIgnoreCase, out var BridgeIP);
            data.Properties.TryGetValue("UserName", StringComparison.OrdinalIgnoreCase, out var UserName);

            List<HueLight> mylights = GetLights(BridgeIP, UserName);

            CustomListObjectElementCollection items = new CustomListObjectElementCollection();

            foreach (var light in mylights)
            {
                items.Add(new CustomListObjectElement { {"Id", light.Id}, {"Name", light.Name}, {"Type", light.Type},
                { "ProductName", light.ProductName }, {"SwitchedOn", light.SwitchedOn},   {"Brightness", light.Brightness},});
            }

            this.Log?.Info(string.Format("Hue extension fetched {0} rows.", items.Count));
            
            return items;
        }

        protected override CustomListExecuteReturnContext ExecuteFunctionOverride(CustomListData data, CustomListExecuteParameterContext context)
        {
            data.Properties.TryGetValue("BridgeIP", StringComparison.OrdinalIgnoreCase, out var BridgeIP);
            data.Properties.TryGetValue("UserName", StringComparison.OrdinalIgnoreCase, out var UserName);

            this.Log?.Info(string.Format("The function {0} has been called with {1} parameters", context.FunctionName, context.Values.Count));
            var returnContext = default(CustomListExecuteReturnContext);
            
            if (context.FunctionName.Equals("SwitchLightOn"))
            {
                string lightname = context.Values[0].StringValue;
                this.Log?.Info(string.Format("Lightname: {0} -> SwitchLightOn", lightname));
                HueHelper.SwitchLight(BridgeIP, UserName, lightname, true);
            }
            else if (context.FunctionName.Equals("SwitchLightOff"))
            {
                string lightname = context.Values[0].StringValue;
                this.Log?.Info(string.Format("Lightname: {0} -> SwitchLightOff", lightname));
                HueHelper.SwitchLight(BridgeIP, UserName, lightname, false);
            }
            else if (context.FunctionName.Equals("SetLightBrightness"))
            {
                string lightname = context.Values[0].StringValue;
                int brightness = Convert.ToInt32(context.Values[1].GetValue());
                this.Log?.Info(string.Format("Lightname: {0} -> SetLightBrightness -> {1}", lightname, brightness));
                HueHelper.SetLightsBrightness(BridgeIP, UserName, lightname, brightness);
            }
            else
            {
                throw new DataErrorException("Function is not supported in this version.");
            }

            return returnContext;
        }
    }
}
