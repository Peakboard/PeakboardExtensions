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
                        Name = "SendMail",
                        Description = "Sends an email through Graph post request",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection()
                        {
                            new CustomListFunctionInputParameterDefinition()
                            {
                                Name = "Subject",
                                Description = "Subject of the email",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.String
                            },
                            new CustomListFunctionInputParameterDefinition()
                            {
                                Name = "Body",
                                Description = "Body of the email",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.String
                            },
                            new CustomListFunctionInputParameterDefinition()
                            {
                                Name = "Recipient",
                                Description = "Recipient of the email",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.String
                            }
                        },
                        ReturnParameters = new CustomListFunctionReturnParameterDefinitionCollection
                        {
                            new CustomListFunctionReturnParameterDefinition
                            {
                                Name = "Sent",
                                Type = CustomListFunctionParameterTypes.Boolean,
                                Description = "Returns if the email was sent"
                            }
                        }
                    },
                    new CustomListFunctionDefinition()
                    {
                        Name = "AddEvent",
                        Description = "Adds a calendar event to the calendar",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection()
                        {
                            new CustomListFunctionInputParameterDefinition()
                            {
                                Name = "Subject",
                                Description = "Subject of the event",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.String
                            },
                            new CustomListFunctionInputParameterDefinition()
                            {
                                Name = "StartDateTime",
                                Description = "Start of the event",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.String
                            },
                            new CustomListFunctionInputParameterDefinition()
                            {
                                Name = "EndDateTime",
                                Description = "End of the event",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.String
                            }
                        },
                        ReturnParameters = new CustomListFunctionReturnParameterDefinitionCollection()
                        {
                            new CustomListFunctionReturnParameterDefinition()
                            {
                                Name = "Added",
                                Description = "Returns if the event was added successfully",
                                Type = CustomListFunctionParameterTypes.Boolean
                            }
                        }
                    },
                    new CustomListFunctionDefinition()
                    {
                        Name = "AddTask",
                        Description = "Adds a task to a todo task list",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection()
                        {
                            new CustomListFunctionInputParameterDefinition()
                            {
                                Name = "ListID",
                                Description = "Id if the list the task should be added to",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.String
                            },
                            new CustomListFunctionInputParameterDefinition()
                            {
                                Name = "Title",
                                Description = "Title of the task",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.String
                            },
                        },
                        ReturnParameters = new CustomListFunctionReturnParameterDefinitionCollection()
                        {
                            new CustomListFunctionReturnParameterDefinition()
                            {
                                Name = "Added",
                                Description = "Returns if the task was added successfully",
                                Type = CustomListFunctionParameterTypes.Boolean
                            }
                        }
                    }
                }
            };
        }

        protected override FrameworkElement GetControlOverride()
        {
            // return an instance of the UI user control
            return new GraphUiControl();
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            // get a helper
            var helper = GetGraphHelper(data);

            // make graph call
            string request = data.Parameter.Split(';')[7];
            string customCall = data.Parameter.Split(';')[14];

            if (customCall != "") request = customCall;

            var task = helper.GetAsync(request, BuildRequestParameters(data));
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
            // get a graph helper
            var helper = GetGraphHelper(data);

            // make graph call
            string request = data.Parameter.Split(';')[7];
            string customCall = data.Parameter.Split(';')[14];

            if (customCall != "") request = customCall;

            var task = helper.GetAsync(request, BuildRequestParameters(data));
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

        #region Functions

        protected override CustomListExecuteReturnContext ExecuteFunctionOverride(CustomListData data,
            CustomListExecuteParameterContext context)
        {
            bool result;

            switch (context.FunctionName)
            {
                case "SendMail":
                    result = SendMail(data, context.Values);
                    break;
                case "AddEvent":
                    result = AddEvent(data, context.Values);
                    break;
                case "AddTask":
                    result = AddTask(data, context.Values);
                    break;
                default:
                    result = false;
                    break;
            }
            
            return new CustomListExecuteReturnContext() { result };
        }

        private bool SendMail(CustomListData data, CustomListExecuteFunctionValueCollection values)
        {
            string url = "/sendMail";
            //string body =
                //"{\"message\": {\"subject\": \"$0$\",\"body\": {\"contentType\": \"Text\",\"content:\" \"$1$\"}, \"toRecipients\": [{\"emailAddress\": {\"address\": \"$2$\"} }] }}";


            string body = @"{
                ""message"": {
                    ""subject"": ""$0$"",
                    ""body"": {
                        ""contentType"": ""Text"",
                        ""content"": ""$1$""
                    },
                    ""toRecipients"": [
                    {
                        ""emailAddress"": {
                            ""address"": ""$2$""
                        }
                    }
                    ]
                }
            }";
            
            if (values.Count == 3)
            {
                var formattedBody = body.Replace("$0$", values[0].StringValue);
                formattedBody = formattedBody.Replace("$1$", values[1].StringValue);
                formattedBody = formattedBody.Replace("$2$", values[2].StringValue);

                var helper = GetGraphHelper(data);

                var task = helper.PostAsync(url, formattedBody);
                task.Wait();

                return task.Result;
            }

            return false;
        }

        private bool AddTask(CustomListData data, CustomListExecuteFunctionValueCollection values)
        {
            string url = "/todo/lists/{0}/tasks";
            string body = "{\"title\": \"$0$\"}";

            if (values.Count == 2)
            {
                var formattedUrl = String.Format(url, values[0].StringValue);
                var formattedBody = body.Replace("$0$", values[1].StringValue);

                var helper = GetGraphHelper(data);

                var task = helper.PostAsync(formattedUrl, formattedBody);
                task.Wait();

                return task.Result;
            }

            return false;
        }

        private bool AddEvent(CustomListData data, CustomListExecuteFunctionValueCollection values)
        {
            string url = "/events";
            string body = @"{
                ""subject"": ""$0$"",
                ""start"": {
                ""dateTime"": ""$1$"",
                ""timeZone"": ""UTC""
                },
                ""end"": {
                ""dateTime"": ""$2$"",
                ""timeZone"": ""UTC""
                }
            }";
                

            if (values.Count == 3)
            {
                var formattedBody = body.Replace("$0$", values[0].StringValue);
                formattedBody = formattedBody.Replace("$1$", values[1].StringValue);
                formattedBody = formattedBody.Replace("$2$", values[2].StringValue);

                var helper = GetGraphHelper(data);

                var task = helper.PostAsync(url, formattedBody);
                task.Wait();

                return task.Result;
            }

            return false;
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
                    UpdateParameter(helper.GetAccessToken(), helper.GetExpirationTime(), helper.GetMillis(),
                        helper.GetRefreshToken(), data);
                }
            }

            this.Log?.Verbose("Successful initialized GraphHelper");

            return helper;
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

            if (!prepared)
            {
                // no value array -> response contains single object which starts immediately
                reader = new JsonTextReader(new StringReader(response));
            }

            return reader;
        }

        public void UpdateParameter(string accessToken, string expiresIn, long millis, string refreshToken,
            CustomListData data)
        {
            // replace tokens in parameter if renewed
            var values = data.Parameter.Split(';');
            values[3] = accessToken;
            values[4] = expiresIn;
            values[5] = millis.ToString();
            values[6] = refreshToken;
            string result = values[0];

            for (int i = 1; i < values.Length; i++)
            {
                result += $";{values[i]}";
            }

            data.Parameter = result;
        }

        private RequestParameters BuildRequestParameters(CustomListData data)
        {
            string[] paramArr = data.Parameter.Split(';');

            if (paramArr[14] != "")
            {
                // custom call -> no request parameter
                return new RequestParameters()
                {
                    ConsistencyLevelEventual = paramArr[11] == "true"
                };
            }

            int top, skip;

            // try parse strings to int
            try
            {
                top = Int32.Parse(paramArr[12]);
            }
            catch (Exception)
            {
                top = 0;
            }

            try
            {
                skip = Int32.Parse(paramArr[13]);
            }
            catch (Exception)
            {
                skip = 0;
            }

            return new RequestParameters()
            {
                Select = paramArr[8],
                OrderBy = paramArr[9],
                Filter = paramArr[10],
                ConsistencyLevelEventual = paramArr[11] == "true",
                Top = top,
                Skip = skip
            };

            /*
                8   =>  select
                9   =>  order by
                10  =>  filter
                11  =>  consistency level (header)(for filter)
                12  =>  top
                13  =>  skip
                14  =>  custom call
            */
        }

        #endregion
    }
}