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
                    new CustomListFunctionDefinition
                    {
                        Name = "setlightcolor",
                        Description = "Sets the color of the light (0-65535)",
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
                                Name = "color",
                                Type = CustomListFunctionParameterTypes.Number,
                                Optional = false,
                                Description = "Color of the light bulb (0-65535)"
                            },
                        },
                    },
                    new CustomListFunctionDefinition
                    {
                        Name = "Alert",
                        Description = "Runs a 15 seconds alert",
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
                    }
                }
            };
        }

        protected override FrameworkElement GetControlOverride()
        {
            // return an instance of the UI user control
            return new HueUIControl(Extension);
        }

        protected override void CheckDataOverride(CustomListData data)
        {
            if (data.Parameter.Split(';').Length != 2)
            {
                throw new InvalidOperationException("Invalid data");
            }

            var bridgeIP = data.Parameter.Split(';')[0];
            var userName = data.Parameter.Split(';')[1];

            if (string.IsNullOrWhiteSpace(bridgeIP))
            {
                throw new InvalidOperationException("Please provide a Bridge IP");
            }
            if (string.IsNullOrWhiteSpace(userName))
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
            if (data.Parameter.Split(';').Length != 2)
            {
                throw new InvalidOperationException("Invalid data");
            }

            var bridgeIP = data.Parameter.Split(';')[0];
            var userName = data.Parameter.Split(';')[1];

            List<HueLight> mylights = GetLights(bridgeIP, userName);

            var items = new CustomListObjectElementCollection();

            foreach (var light in mylights)
            {
                items.Add(new CustomListObjectElement { {"Id", light.Id}, {"Name", light.Name}, {"Type", light.Type},
                { "ProductName", light.ProductName }, {"SwitchedOn", light.SwitchedOn},   {"Brightness", light.Brightness},});
            }

            //this.Log?.Info(string.Format("Hue extension fetched {0} rows.", items.Count));

            return items;
        }

        protected override CustomListExecuteReturnContext ExecuteFunctionOverride(CustomListData data, CustomListExecuteParameterContext context)
        {
            var bridgeIP = data.Parameter.Split(';')[0];
            var userName = data.Parameter.Split(';')[1];

            this.Log?.Info(string.Format("The function {0} has been called with {1} parameters", context.FunctionName, context.Values.Count));
            var returnContext = default(CustomListExecuteReturnContext);
            
            if (context.FunctionName.Equals("switchlighton", StringComparison.InvariantCultureIgnoreCase))
            {
                string lightname = context.Values[0].StringValue;
                this.Log?.Info(string.Format("Lightname: {0} -> switchlighton", lightname));
                HueHelper.SwitchLight(bridgeIP, userName, lightname, true);
            }
            else if (context.FunctionName.Equals("switchlightoff", StringComparison.InvariantCultureIgnoreCase))
            {
                string lightname = context.Values[0].StringValue;
                this.Log?.Info(string.Format("Lightname: {0} -> switchlightoff", lightname));
                HueHelper.SwitchLight(bridgeIP, userName, lightname, false);
            }
            else if (context.FunctionName.Equals("setlightbrightness", StringComparison.InvariantCultureIgnoreCase))
            {
                string lightname = context.Values[0].StringValue;
                int brightness = Convert.ToInt32(context.Values[1].GetValue());
                this.Log?.Info(string.Format("Lightname: {0} -> setlightbrightness -> {1}", lightname, brightness));
                HueHelper.SetLightBrightness(bridgeIP, userName, lightname, brightness);
            }
            else if (context.FunctionName.Equals("setlightcolor", StringComparison.InvariantCultureIgnoreCase))
            {
                string lightname = context.Values[0].StringValue;
                int red = Convert.ToInt32(context.Values[1].GetValue());
                int green = Convert.ToInt32(context.Values[1].GetValue());
                int blue = Convert.ToInt32(context.Values[1].GetValue());
                this.Log?.Info(string.Format("Lightname: {0} -> setlightcolor -> {1}", lightname, red, green, blue));
                HueHelper.SetLightColor(bridgeIP, userName, lightname, red, green, blue);
            }
            else if (context.FunctionName.Equals("alert", StringComparison.InvariantCultureIgnoreCase))
            {
                string lightname = context.Values[0].StringValue;
                this.Log?.Info(string.Format("Lightname: {0} -> alert", lightname));
                HueHelper.Alert(bridgeIP, userName, lightname);
            }
            else
            {
                throw new DataErrorException("Function is not supported in this version.");
            }

            return returnContext;
        }
    }
}
