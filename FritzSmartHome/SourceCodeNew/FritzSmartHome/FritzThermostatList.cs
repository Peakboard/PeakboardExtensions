using Peakboard.ExtensionKit;
using System;
using System.Globalization;
using System.Linq;

namespace AVMFritz
{
    [CustomListIcon("AVMFritz.Fritz.png")]
    [Serializable]
    class FritzThermostatList : CustomListBase
    {
        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            // create a static collection of columns 
            return new CustomListColumnCollection
            {
                new CustomListColumn("Id", CustomListColumnTypes.String),
                new CustomListColumn("Name", CustomListColumnTypes.String),
                new CustomListColumn("Present", CustomListColumnTypes.Boolean),
                new CustomListColumn("Battery", CustomListColumnTypes.Number),
                new CustomListColumn("TempCurrent", CustomListColumnTypes.Number),
                new CustomListColumn("TempTarget", CustomListColumnTypes.Number),
            };
        }

        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = $"FritzThermostatList",
                Name = "Fritz Thermostats",
                Description = "Enables access to Fritz Thermostats",
                PropertyInputPossible = true,
                PropertyInputDefaults = {
                    new CustomListPropertyDefinition() { Name = "Hostname", Value = "fritz.box" },
                    new CustomListPropertyDefinition() { Name = "User", Value = string.Empty },
                    new CustomListPropertyDefinition() { Name = "Password", Value = "XXXXXX" },
                },
                Functions = new CustomListFunctionDefinitionCollection
                {
                    new CustomListFunctionDefinition
                    {
                        Name = "settemperature",
                        Description = "Sets the temperature of a thermostat",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "name",
                                Type = CustomListFunctionParameterTypes.String,
                                Optional = false,
                                Description = "The name of thermostat"
                            },
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "temperature",
                                Type = CustomListFunctionParameterTypes.Number,
                                Optional = false,
                                Description = "The target temperature"
                            }
                        },
                    },
                }
            };
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            var items = new CustomListObjectElementCollection();
            var fh = new FritzHelper();
            data.Properties.TryGetValue("Hostname", StringComparison.OrdinalIgnoreCase, out var Hostname);
            data.Properties.TryGetValue("User", StringComparison.OrdinalIgnoreCase, out var User);
            data.Properties.TryGetValue("Password", StringComparison.OrdinalIgnoreCase, out var Password);

            var thermostats = fh.GetThermostats(Hostname, User, Password);

            foreach (var thermostat in thermostats)
            {
                items.Add(new CustomListObjectElement { 
                    {"Id", thermostat.Id},
                    {"Name", thermostat.Name},
                    {"Present", thermostat.Present},
                    {"Battery", thermostat.Battery},
                    {"TempCurrent", thermostat.TempCurrent},
                    {"TempTarget", thermostat.TempTarget}
                });
            }

            return items;
        }

        protected override void CheckDataOverride(CustomListData data)
        {
            data.Properties.TryGetValue("Password", StringComparison.OrdinalIgnoreCase, out var Password);

            if (string.IsNullOrWhiteSpace(Password))
            {
                throw new InvalidOperationException("Please provide a password");
            }
        }

        protected override CustomListExecuteReturnContext ExecuteFunctionOverride(CustomListData data, CustomListExecuteParameterContext context)
        {
            data.Properties.TryGetValue("Hostname", StringComparison.OrdinalIgnoreCase, out var Hostname);
            data.Properties.TryGetValue("User", StringComparison.OrdinalIgnoreCase, out var User);
            data.Properties.TryGetValue("Password", StringComparison.OrdinalIgnoreCase, out var Password);

            Log?.Info(string.Format("The function {0} has been called with {1} parameters", context.FunctionName, context.Values.Count));
            var returnContext = default(CustomListExecuteReturnContext);
            var fh = new FritzHelper();
            var thermostat = fh.GetThermostats(Hostname, User, Password).First(t => t.Name.Equals(context.Values[0].StringValue));

            if (context.FunctionName.Equals("settemperature"))
            {
                fh.SetThermostatTemperature(Hostname, User, Password, thermostat, double.Parse(context.Values[1].StringValue, CultureInfo.InvariantCulture));
            }

            return returnContext;
        }
    }
}
