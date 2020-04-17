using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Peakboard.ExtensionKit;
using System.Net;
using System.Globalization;
using System.Windows;
using Newtonsoft.Json;
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
                        Name = "SwitchLightOn",
                        Description = "Switches a light on",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "NameOfLight",
                                Type = CustomListFunctionParameterTypes.String,
                                Optional = false,
                                Description = "The name of the light bulb as indicated in the lights list"
                            },
                        },
                    },
                    new CustomListFunctionDefinition
                    {
                        Name = "SwitchLightOff",
                        Description = "Switches a light off",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "NameOfLight",
                                Type = CustomListFunctionParameterTypes.String,
                                Optional = false,
                                Description = "The name of the light bulb as indicated in the lights list"
                            },
                        },
                    },
                                        new CustomListFunctionDefinition
                    {
                        Name = "SetLightBrightness",
                        Description = "Sets the brightness of the light (0-254)",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "NameOfLight",
                                Type = CustomListFunctionParameterTypes.String,
                                Optional = false,
                                Description = "The name of the light bulb as indicated in the lights list"
                            },
                                                        new CustomListFunctionInputParameterDefinition
                            {
                                Name = "Brightness",
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
            // doing several checks with the Parameter that contains all out properties as JSon string
            HueLightsCustomListStorage mystorage = HueLightsCustomListStorage.GetFromParameterString(data.Parameter);

            if (string.IsNullOrWhiteSpace(mystorage.BridgeIP))
            {
                throw new InvalidOperationException("Please provide a Bridge IP");
            }
            if (string.IsNullOrWhiteSpace(mystorage.UserName))
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
            // Downloading the weather data for the airport code
            HueLightsCustomListStorage mystorage = HueLightsCustomListStorage.GetFromParameterString(data.Parameter);

            List<HueLight> mylights = GetLights(mystorage.BridgeIP, mystorage.UserName);

            CustomListObjectElementCollection items = new CustomListObjectElementCollection();

            foreach (var light in mylights)
            {
                items.Add(new CustomListObjectElement { {"Id", light.Id}, {"Name", light.Name}, {"Type", light.Type},
                { "ProductName", light.ProductName }, {"SwitchedOn", light.SwitchedOn},   {"Brightness", light.Brightness},});
            }

            this.Log?.Info(string.Format("Airport condition extension fetched {0} rows.", items.Count));
            
            return items;
        }

        protected override CustomListExecuteReturnContext ExecuteFunctionOverride(CustomListData data, CustomListExecuteParameterContext context)
        {
            this.Log?.Info(string.Format("The function {0} has been called with {1} parameters", context.FunctionName, context.Values.Count));
            HueLightsCustomListStorage mystorage = HueLightsCustomListStorage.GetFromParameterString(data.Parameter);
            var returnContext = default(CustomListExecuteReturnContext);
            
            if (context.FunctionName.Equals("SwitchLightOn"))
            {
                string lightname = context.Values[0].StringValue;
                this.Log?.Info(string.Format("Lightname: {0} -> SwitchLightOn", lightname));
                HueHelper.SwitchLight(mystorage.BridgeIP, mystorage.UserName, lightname, true);
            }
            else if (context.FunctionName.Equals("SwitchLightOff"))
            {
                string lightname = context.Values[0].StringValue;
                this.Log?.Info(string.Format("Lightname: {0} -> SwitchLightOff", lightname));
                HueHelper.SwitchLight(mystorage.BridgeIP, mystorage.UserName, lightname, false);
            }
            else if (context.FunctionName.Equals("SetLightBrightness"))
            {
                string lightname = context.Values[0].StringValue;
                int brightness = Convert.ToInt32(context.Values[1].GetValue());
                this.Log?.Info(string.Format("Lightname: {0} -> SetLightBrightness -> {1}", lightname, brightness));
                HueHelper.SetLightsBrightness(mystorage.BridgeIP, mystorage.UserName, lightname, brightness);
            }
            else
            {
                throw new DataErrorException("Function is not supported in this version.");
            }

            return returnContext;
        }
    }
}
