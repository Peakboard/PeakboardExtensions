using System;
using System.IO;
using System.Linq;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Peakboard.ExtensionKit;

namespace PeakboardExtensionGraph.UserAuth
{
    [Serializable]
    public class MsGraphCustomList : CustomListBase
    {
        // todo fix unusual behavior with multiple accounts
        private bool _initialized;
        private GraphHelperUserAuth _graphHelper;

        private CustomListFunctionDefinition _func = new CustomListFunctionDefinition()
        {
            Name = "SendMail",
            InputParameters = new CustomListFunctionInputParameterDefinitionCollection()
            {
                new CustomListFunctionInputParameterDefinition()
                {
                    Name = "Parameters",
                    Description = "String containing all parameters for the function separated by ';' character",
                    Optional = false,
                    Type = CustomListFunctionParameterTypes.String
                }
            },
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
        
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = $"MsGraphUserAuthCustomList",
                Name = "Microsoft Graph UserAuth List",
                Description = "Returns data from MS-Graph API",
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
            return new GraphUiControl();
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            if (!_initialized)
            {
                InitializeGraph(data);
            }
            
            // check if access token expired
            var expiredTask = _graphHelper.CheckIfTokenExpiredAsync();
            expiredTask.Wait();
            
            // update refresh token in parameter if renewed
            if (expiredTask.Result)
            {
                UpdateRefreshToken(_graphHelper.GetRefreshToken(), data);
            }
            
            // make graph call
            string request = data.Parameter.Split(';')[3];
            string customCall = data.Parameter.Split(';')[12];

            if (customCall != "") request = customCall;

            var task = _graphHelper.MakeGraphCall(request, BuildParameter(data));
            task.Wait();
            var response = task.Result;

            var cols = new CustomListColumnCollection();
            
            // parse json to PB Columns
            JsonTextReader reader = PreparedReader(response);

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.StartObject)
                {
                    JsonHelper.ColumnsWalkThroughObject(reader, "root", cols);
                    break;
                }
            }

            return cols;
        }
        

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            // check if GraphHelper & RequestBuilder are initialized
            if (!_initialized)
            { 
                InitializeGraph(data);
            }
            
            // check if access token expired
            var expiredTask = _graphHelper.CheckIfTokenExpiredAsync();
            expiredTask.Wait();
            
            // update refresh token in parameter if renewed
            if (expiredTask.Result)
            {
                UpdateRefreshToken(_graphHelper.GetRefreshToken(), data);
            }
            
            // make graph call
            string request = data.Parameter.Split(';')[3];
            string customCall = data.Parameter.Split(';')[12];

            if (customCall != "") request = customCall;

            var task = _graphHelper.MakeGraphCall(request, BuildParameter(data));
            task.Wait();
            var response = task.Result;

            var items = new CustomListObjectElementCollection();

            // parse response to PB table
            JsonTextReader reader = PreparedReader(response);
            JObject jObject = JObject.Parse(response);

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.StartObject)
                {
                    var item = new CustomListObjectElement();
                    JsonHelper.ItemsWalkThroughObject(reader, "root", item, jObject);
                    items.Add(item);
                }
            }
            
            return items;
        }

        protected override CustomListFunctionDefinitionCollection GetDynamicFunctionsOverride(CustomListData data)
        {
            var functions = base.GetDynamicFunctionsOverride(data);
            
            string url = data.Parameter.Split(';')[13];
            string json = data.Parameter.Split(';')[14];
            string funcName = url.Split('/').Last();

            if (!String.IsNullOrWhiteSpace(json) && !String.IsNullOrWhiteSpace(url))
            {
                var func = new CustomListFunctionDefinition()
                {
                    Name = funcName,
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

                var reader = new JsonTextReader(new StringReader(json));
                string parameterName = "";
                while (reader.Read())
                {
                    if (reader.TokenType == JsonToken.PropertyName)
                    {
                        parameterName = reader.Value?.ToString();
                    }
                    if (reader.TokenType == JsonToken.String && reader.Value.ToString().StartsWith("$") &&
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
            
            var url = data.Parameter.Split(';')[13];
            var json = data.Parameter.Split(';')[14];

            // get user input
            var parameters = context.Values;

            // put user input into json template
            for (int i = 0; i < parameters.Count; i++)
            {
                json = json.Replace($"${i}$", parameters[i].StringValue);
            }

            // make graph post request 
            var task = _graphHelper.PostAsync(url, json);
            task.Wait();

            // return if request succeeded
            var ret = new CustomListExecuteReturnContext();
            ret.Add(task.Result);

            return ret;
            
        }

        #region HelperFunctions
        
        private void InitializeGraph(CustomListData data)
        {
            this.Log?.Info("Initializing GraphHelper");
            
            // get refresh token from parameter
            string refreshToken = data.Parameter.Split(';')[10];

            // check if refresh token is available
            if (string.IsNullOrEmpty(refreshToken))
            {
                // if refresh token isn't available -> user did not authenticate
                throw new NullReferenceException("Refresh token not initialized: User did not authenticate");
            }
            else
            {
                // get parameter for azure app
                string clientId = data.Parameter.Split(';')[0];
                string tenantId = data.Parameter.Split(';')[1];
                string permissions = data.Parameter.Split(';')[2];
                
                // if available initialize by refresh token (in runtime)
                _graphHelper = new GraphHelperUserAuth(clientId, tenantId, permissions);
                var task = _graphHelper.InitGraphWithRefreshToken(refreshToken);
                task.Wait();
                
            }

            this.Log?.Info("Successful initialized GraphHelper");
            _initialized = true;

        }

        private JsonTextReader PreparedReader(string response)
        {
            // prepare reader for recursive walk trough
            var reader = new JsonTextReader(new StringReader(response));
            bool prepared = false;

            while (reader.Read() && !prepared)
            {
                if (reader.TokenType == JsonToken.PropertyName && reader.Value?.ToString() == "value")
                {
                    // if json contains value array -> collection response with several objects
                    // parsing starts after the array starts
                    prepared = true;
                }
                else if (reader.TokenType == JsonToken.PropertyName && reader.Value?.ToString() == "error")
                {
                    // if json contains an error field -> deserialize to Error Object & throw exception
                    GraphHelperBase.DeserializeError(response);
                }
            }
            if(!prepared)
            {
                // no value array -> response contains single object which starts immediately
                reader = new JsonTextReader(new StringReader(response));
            }

            return reader;
        }
        
        public void UpdateRefreshToken(string token, CustomListData data)
        {
            // replace refresh token in parameter if renewed
            var values = data.Parameter.Split(';');
            values[10] = token;
            string result = values[0];
            
            for(int i = 1; i < values.Length; i++)
            {
                result += $";{values[i]}";
            }

            data.Parameter = result;
        }
        
        private RequestParameters BuildParameter(CustomListData data)
        {
            string[] paramArr = data.Parameter.Split(';');

            if (paramArr[12] != "")
            {
                // custom call -> no request parameter
                return new RequestParameters()
                {
                    ConsistencyLevelEventual = paramArr[7] == "true"
                };
            }
            
            int top, skip;

            // try parse strings to int
            try { top = Int32.Parse(paramArr[8]); } catch (Exception) { top = 0; }
            try { skip = Int32.Parse(paramArr[9]); } catch (Exception) { skip = 0; }

            return new RequestParameters()
            {
                Select = paramArr[4],
                OrderBy = paramArr[5],
                Filter = paramArr[6],
                ConsistencyLevelEventual = paramArr[7] == "true",
                Top = top,
                Skip = skip
            };
            
            /*
                4   =>  select
                5   =>  order by
                6   =>  filter
                7   =>  consistency level (header)(for filter)
                8   =>  top
                9   =>  skip
                10  =>  custom entities (not used here)
                11  =>  custom call
            */
        }
        
        #endregion

    }
}