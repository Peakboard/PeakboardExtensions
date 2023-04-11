using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using Newtonsoft.Json;
using Peakboard.ExtensionKit;
using PeakboardExtensionGraph.UserAuth;

namespace PeakboardExtensionGraph.UserAuthFunctions
{
    [Serializable]
    public class MsGraphFunctionsCustomList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = $"MsGraphFunctionsCustomList",
                Name = "Microsoft Graph Functions",
                Description = "Sends post-requests to Ms-Graph API",
                PropertyInputPossible = true,
                Functions = new CustomListFunctionDefinitionCollection
                {
                    new CustomListFunctionDefinition
                    {
                        Name = "GetDynamicFunctionsMetadata",
                        Description = "Returns metadata of the defined dynamic functions.",
                        ReturnParameters = new CustomListFunctionReturnParameterDefinitionCollection
                        {
                            new CustomListFunctionReturnParameterDefinition
                            {
                                Name = "Count",
                                Type = CustomListFunctionParameterTypes.Number,
                                Description = "The number of dynamic functions."
                            }
                        }
                    }
                },
                SupportsDynamicFunctions = true
            };
        }
        
        protected override FrameworkElement GetControlOverride()
        {
            // return an instance of the UI user control
            return new GraphFunctionsUiControl();
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            return new CustomListColumnCollection()
            {
                new CustomListColumn()
                {
                    Name = "Function Count",
                    Type = CustomListColumnTypes.Number
                }
            };
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            var count = 0;
            if (!String.IsNullOrEmpty(data.Parameter))
            {
                count = data.Parameter.Split(';')[7].Split('|').Length;
            }

            return new CustomListObjectElementCollection()
            {
                new CustomListObjectElement { { "FunctionCount", count } }
            };
        }

        #region Functions

        protected override CustomListFunctionDefinitionCollection GetDynamicFunctionsOverride(CustomListData data)
        {
            var functions = base.GetDynamicFunctionsOverride(data);
            
            // get function names & bodies
            string funcNames = data.Parameter.Split(';')[7];
            string jsons = data.Parameter.Split(';')[9];

            if (!String.IsNullOrWhiteSpace(jsons) && !String.IsNullOrWhiteSpace(funcNames))
            {
                // split up into arrays
                string[] names = funcNames.Split('|');
                string[] bodies = jsons.Split('|');

                // check if length of arrays vary -> each function body must have a name
                if (names.Length == bodies.Length)
                {
                    for (int i = 0; i < names.Length; i++)
                    {
                        var func = new CustomListFunctionDefinition(){
                            Name = names[i],
                            ReturnParameters = new CustomListFunctionReturnParameterDefinitionCollection
                            {
                                new CustomListFunctionReturnParameterDefinition
                                {
                                    Name = "result",
                                    Description = "Success",
                                    Type = CustomListFunctionParameterTypes.Boolean
                                }
                            }
                        };

                        // get placeholders in json body
                        var values = GetInputParameters(bodies[i]);
                        
                        // define input parameters of custom list function with placeholders
                        foreach (var value in values)
                        {
                            func.InputParameters.Add(new CustomListFunctionInputParameterDefinition()
                            {
                                Name = value.Split('_')[1].Replace("$", ""),
                                Type = GetParameterType(value.Replace("$", "")),
                                Optional = false
                            });
                        }
                        
                        // add new function to function collection
                        functions.Add(func);
                        
                    }
                }
                else
                {
                    this.Log?.Warning($"Parameter String might be corrupted: Number of function names and function bodies vary");
                }
            }
            return functions;
        }
        
        protected double GetDynamicFunctionsMetadata(CustomListData data, CustomListExecuteParameterContext context)
        {
            Log?.Verbose($"Function '{nameof(GetDynamicFunctionsMetadata)}' for CustomList '{data.ListName ?? "?"}' called...");

            return 0;
        }

        protected override CustomListExecuteReturnContext ExecuteFunctionOverride(CustomListData data, CustomListExecuteParameterContext context)
        {
            if (context.TryExecute("GetDynamicFunctionsMetadata", data, GetDynamicFunctionsMetadata, out var returnContext))
            {
                return returnContext;
            }

            // Run at the end.
            if (context.TryExecute(data, RunDynamicFunction, out returnContext))
            {
                return returnContext;
            }

            // Ignore by not doing anything OR throw exception to return error.
            throw new DataErrorException("Function is not supported in this version.");
        }
        
        protected CustomListExecuteReturnContext RunDynamicFunction(CustomListData data, CustomListExecuteParameterContext context)
        {
            Log?.Verbose($"Function '{context.FunctionName}' for CustomList '{data.ListName ?? "?"}' called...");

            var helper = GetGraphHelper(data);
            
            var funcNames = data.Parameter.Split(';')[7].Split('|');
            var funcUrls = data.Parameter.Split(';')[8].Split('|');
            var funcBodies = data.Parameter.Split(';')[9].Split('|');

            // get user input
            var parameters = context.Values;
            
            // get function name
            var funcName = context.FunctionName;

            // check if array sizes vary
            if (funcNames.Length == funcUrls.Length && funcUrls.Length == funcBodies.Length)
            {
                // get index of called function
                var index = Array.IndexOf(funcNames, funcName);
                if (index < 0)
                {
                    this.Log?.Warning($"Function not found: {funcName}");
                    return new CustomListExecuteReturnContext() { false };
                }
                
                // get corresponding url & json object
                var url = funcUrls[index];
                var body = funcBodies[index];
                var values = GetInputParameters(body);
                
                if(parameters.Count == values.Count)
                {
                    // put user input into json template
                    for (int i = 0; i < parameters.Count; i++)
                    {
                        string newVal = (parameters[i].TypeName == CustomListFunctionParameterTypes.Boolean
                            ? parameters[i].StringValue.ToLower()
                            : parameters[i].StringValue);
                        body = body.Replace(values[i], newVal);
                    }
                }
                else
                {
                    this.Log?.Warning($"Number of user inputs doesnt match number of expected arguments. Function execution aborted.");
                    return new CustomListExecuteReturnContext() { false };
                }

                // make graph post request 
                var task = helper.PostAsync(url, body);
                task.Wait();
                return new CustomListExecuteReturnContext { task.Result };
            }
            
            this.Log?.Warning($"Parameter String might be corrupted: Number of function names, function urls and function bodies vary");
            return new CustomListExecuteReturnContext() { false };
        }

        #endregion

        #region HelperMethods

        private GraphHelperUserAuth GetGraphHelper(CustomListData data)
        {
            this.Log?.Verbose("Initializing GraphHelper");
            GraphHelperUserAuth helper;
            // get refresh token
            string refreshToken = data.Parameter.Split(';')[6];

            // check if refresh token is available
            if (string.IsNullOrEmpty(refreshToken))
            {
                // if refresh token isn't available -> user did not authenticate
                throw new NullReferenceException("Refresh token not initialized: User did not authenticate");
            }
            else
            {
                // get parameters for azure app
                string clientId = data.Parameter.Split(';')[0];
                string tenantId = data.Parameter.Split(';')[1];
                string permissions = data.Parameter.Split(';')[2];
                string accessToken = data.Parameter.Split(';')[3];
                string expiresIn = data.Parameter.Split(';')[4];
                long millis = Int64.Parse(data.Parameter.Split(';')[5]);
                
                // if available initialize with access token (in runtime)
                helper = new GraphHelperUserAuth(clientId, tenantId, permissions);
                helper.InitGraphWithAccessToken(accessToken, expiresIn, millis, refreshToken);
                
                // check if access token expired
                var expired = helper.CheckIfTokenExpiredAsync();
                expired.Wait();
                if (expired.Result)
                {
                    UpdateAccessInformation(helper.GetAccessToken(), helper.GetExpirationTime(), helper.GetMillis(), helper.GetRefreshToken(), data);
                }
            }

            this.Log?.Verbose("Successful initialized GraphHelper");

            return helper;
        }
        
        public void UpdateAccessInformation(string accessToken, string expiresIn, long millis, string refreshToken, CustomListData data)
        {
            // replace tokens in parameter if renewed
            var values = data.Parameter.Split(';');
            values[3] = accessToken;
            values[4] = expiresIn;
            values[5] = millis.ToString();
            values[6] = refreshToken;
            string result = values[0];
            
            for(int i = 1; i < values.Length; i++)
            {
                result += $";{values[i]}";
            }

            data.Parameter = result;
        }

        private List<string> GetInputParameters(string json)
        {
            // walk through json string and get all placeholders (indicated by '$')
            
            int start, end;
            int startIndex = 0;
            List<string> values = new List<string>();

            while (json.IndexOf('$', startIndex) >= 0)
            {
                // get indexes of next two $ tokens
                start = json.IndexOf('$', startIndex);
                startIndex = start + 1;
                end = json.IndexOf('$', startIndex);
                startIndex = end + 1;
                
                // get substring between $ tokens
                string value = json.Substring(start, (end-start)+1);
                if (value.Split('_').Length == 2)
                {
                    // add to list if placeholder is valid
                    values.Add(value);
                }
                else
                {
                    throw new ArgumentException($"Invalid placeholder {value}. Expected: $<type>_<name>$");
                }
            }

            return values;
        }

        private string GetParameterType(string value)
        {
            // Get the expected datatype of a placeholder
            var values = value.Split('_');
            switch (values[0])
            {
                case "s":
                    return CustomListFunctionParameterTypes.String;
                case "d":
                    return CustomListFunctionParameterTypes.Number;
                case "b":
                    return CustomListFunctionParameterTypes.Boolean;
                default:
                    // throw exception if type doesn't exist
                    throw new ArgumentException($"Invalid placeholder type: {values[0]}. Supported are 's' (string), 'd' (numeric type), 'b' (boolean)");
            }
        }
        
        #endregion
    }
}