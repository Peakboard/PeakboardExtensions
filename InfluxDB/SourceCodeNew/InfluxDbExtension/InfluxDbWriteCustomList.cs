using System;
using Peakboard.ExtensionKit;

namespace InfluxDbExtension
{
    [Serializable]
    [CustomListIcon("InfluxDbExtension.pb_datasource_influx.png")]
    public class InfluxDbWriteCustomList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition()
            {
                ID = "InfluxDbWriteCustomList",
                Name = "InfluxDB Write Custom List",
                Description = "Write data to InfluxDB",
                PropertyInputDefaults = new CustomListPropertyDefinitionCollection()
                {
                    new CustomListPropertyDefinition()
                    {
                        Name = "URL",
                        Value = ""
                    },
                    new CustomListPropertyDefinition()
                    {
                        Name = "Token",
                        Value = ""
                    },
                    new CustomListPropertyDefinition()
                    {
                        Name = "Measurement",
                        Value = ""
                    },
                    new CustomListPropertyDefinition()
                    {
                        Name = "Tags",
                        Value = ""
                    },
                    new CustomListPropertyDefinition()
                    {
                        Name = "Fields",
                        Value = ""
                    },
                    new CustomListPropertyDefinition()
                    {
                        Name = "FieldDatatypes",
                        Value = ""
                    }
                },
                PropertyInputPossible = true,
                SupportsDynamicFunctions = true
            };
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            return new CustomListColumnCollection()
            {
                new CustomListColumn("WriteToMeasurement", CustomListColumnTypes.String)
            };
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            data.Properties.TryGetValue("Measurement", StringComparison.OrdinalIgnoreCase, out var measurement);
            
            return new CustomListObjectElementCollection()
            {
                new CustomListObjectElement()
                {
                    { "WriteToMeasurement", measurement}
                }
            };
        }
        
        protected override CustomListExecuteReturnContext ExecuteFunctionOverride(CustomListData data, CustomListExecuteParameterContext context)
        {
            if (context.TryExecute(data, RunDynamicFunction, out var returnContext))
            {
                return returnContext;
            }

            // Ignore by not doing anything OR throw exception to return error.
            throw new DataErrorException("Function is not supported in this version.");
        }
        
        protected override CustomListFunctionDefinitionCollection GetDynamicFunctionsOverride(CustomListData data)
        {
            Log?.Verbose($"GetDynamicFunctionsOverride for CustomList '{data.ListName ?? "?"}'");

            var functions = base.GetDynamicFunctionsOverride(data);

            data.Properties.TryGetValue("Measurement", StringComparison.OrdinalIgnoreCase, out var measurement);
            data.Properties.TryGetValue("Tags", StringComparison.OrdinalIgnoreCase, out var tags);
            data.Properties.TryGetValue("Fields", StringComparison.OrdinalIgnoreCase, out var fieldString);
            data.Properties.TryGetValue("FieldDatatypes", StringComparison.OrdinalIgnoreCase, out var datatypeString);

            var fun = new CustomListFunctionDefinition()
            {
                Name = $"Write_{measurement}",
                Description = $"Writes data to {measurement}",
                InputParameters = new CustomListFunctionInputParameterDefinitionCollection(),
                ReturnParameters = new CustomListFunctionReturnParameterDefinitionCollection()
                {
                    new CustomListFunctionReturnParameterDefinition()
                    {
                        Name = "Success",
                        Type = CustomListFunctionParameterTypes.Boolean
                    }
                }
            };

            foreach (string tag in tags.Split(','))
            {
                var param = new CustomListFunctionInputParameterDefinition()
                {
                    Name = tag,
                    Optional = false,
                    Type = CustomListFunctionParameterTypes.String
                };
                fun.InputParameters.Add(param);
            }

            var types = datatypeString.Split(',');
            var fields = fieldString.Split(',');

            if (types.Length != fields.Length)
            {
                this.Log?.Warning("Amount of fields and data types vary. Aborted initializing function.");
                return functions;
            }
            
            for(int i = 0; i < fields.Length; i++)
            {
                var type = CustomListFunctionParameterTypes.Object;
                switch (types[i])
                {
                    case "String":
                        type = CustomListFunctionParameterTypes.String;
                        break;
                    case "Number":
                        type = CustomListFunctionParameterTypes.Number;
                        break;
                    case "Boolean":
                        type = CustomListFunctionParameterTypes.Boolean;
                        break;
                }
                
                var param = new CustomListFunctionInputParameterDefinition()
                {
                    Name = fields[i],
                    Optional = false,
                    Type = type
                };
                fun.InputParameters.Add(param);
            }
            
            functions.Add(fun);

            return functions;
        }
        
        protected CustomListExecuteReturnContext RunDynamicFunction(CustomListData data, CustomListExecuteParameterContext context)
        {
            Log?.Verbose($"Function '{context.FunctionName}' for CustomList '{data.ListName ?? "?"}' called...");

            data.Properties.TryGetValue("Url", StringComparison.OrdinalIgnoreCase, out var url);
            data.Properties.TryGetValue("Token", StringComparison.OrdinalIgnoreCase, out var token);
            data.Properties.TryGetValue("Measurement", StringComparison.OrdinalIgnoreCase, out var measurement);
            data.Properties.TryGetValue("Tags", StringComparison.OrdinalIgnoreCase, out var tagstring);
            data.Properties.TryGetValue("Fields", StringComparison.OrdinalIgnoreCase, out var fieldstring);

            string body = measurement;

            var tags = tagstring.Split(',');
            var fields = fieldstring.Split(',');
            int offset = tags.Length;

            for (int i = 0; i < tags.Length; i++)
            {
                body += $",{tags[i]}={context.Values[i].StringValue}";
            }

            body += " ";
            
            for (int i = 0; i < fields.Length; i++)
            {

                if (context.Values[i+offset].TypeName == CustomListFunctionParameterTypes.String)
                {
                    body += $"{fields[i]}=\"{context.Values[i+offset].StringValue}\",";
                }
                else
                {
                    body += $"{fields[i]}={context.Values[i+offset].StringValue},";
                }
                
            }

            body = body.Remove(body.Length - 1);

            var task  = QueryHelper.WriteAsync(url, token, body);
            task.Wait();

            if (!task.Result.Item2)
            {
                this.Log?.Warning($"Writing data to InfluxDB failed: {task.Result.Item1}");
            }

            return new CustomListExecuteReturnContext()
            {
                task.Result.Item2
            };
        }
    }
}