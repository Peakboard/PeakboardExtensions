using System;
using System.IO;
using System.Linq;
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
                Description = "Sends Data to Ms-Graph API",
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
                    Name = "Dummy",
                    Type = CustomListColumnTypes.Number
                }
            };
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            return new CustomListObjectElementCollection()
            {
                new CustomListObjectElement { { "Dummy", 0 } }
            };
        }

        #region Functions

        protected override CustomListFunctionDefinitionCollection GetDynamicFunctionsOverride(CustomListData data)
        {
            var functions = base.GetDynamicFunctionsOverride(data);

            string funcNames = data.Parameter.Split(';')[7];
            string jsons = data.Parameter.Split(';')[9];

            if (!String.IsNullOrWhiteSpace(jsons) && !String.IsNullOrWhiteSpace(funcNames))
            {
                string[] names = funcNames.Split('|');
                string[] bodies = jsons.Split('|');

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
                        
                        var reader = new JsonTextReader(new StringReader(bodies[i]));
                        string parameterName = "";
                        while (reader.Read())
                        {
                            if (reader.TokenType == JsonToken.PropertyName)
                            {
                                parameterName = reader.Value?.ToString();
                            }
                            if (reader.TokenType == JsonToken.String && reader.Value != null && reader.Value.ToString().StartsWith("$") &&
                                reader.Value.ToString().EndsWith("$"))
                            {
                                func.InputParameters.Add(new CustomListFunctionInputParameterDefinition
                                {
                                    Name = parameterName,
                                    Optional = false,
                                    Type = CustomListFunctionParameterTypes.String
                                });
                            }
                        }
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

            // check if 
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
                
                // put user input into json template
                for (int i = 0; i < parameters.Count; i++)
                {
                    body = body.Replace($"${i}$", parameters[i].StringValue);
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
                    UpdateParameter(helper.GetAccessToken(), helper.GetExpirationTime(), helper.GetMillis(), helper.GetRefreshToken(), data);
                }
            }

            this.Log?.Verbose("Successful initialized GraphHelper");

            return helper;
        }
        
        public void UpdateParameter(string accessToken, string expiresIn, long millis, string refreshToken, CustomListData data)
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

        #endregion
    }
}