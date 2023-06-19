using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Peakboard.ExtensionKit;
using PeakboardExtensionGraph.Settings;

namespace PeakboardExtensionGraph.UserAuth
{
    [CustomListIcon("PeakboardExtensionGraph.graph_clean.png")]
    [Serializable]
    public class MsGraphCustomList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = $"MsGraphUserAuthCustomList",
                Name = "Microsoft Graph User-Delegated Access",
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

        protected override void CheckDataOverride(CustomListData data)
        {
            if (String.IsNullOrEmpty(data.Parameter))
            {
                throw new InvalidOperationException("Settings for Graph Connection not found");
            }

            UserAuthSettings settings;
            try
            {
                //settings = JsonConvert.DeserializeObject<UserAuthSettings>(data.Parameter);
                settings = UserAuthSettings.GetSettingsFromParameterString(data.Parameter);
                if (settings == null) throw new InvalidOperationException("Invalid parameter format");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Error getting settings for graph: {ex.Message}");
            }
            
            if (settings.Parameters == null && string.IsNullOrEmpty(settings.CustomCall))
            {
                this.Log?.Verbose("No Query Parameters available. Extracting entire objects");
            }
            if (String.IsNullOrEmpty(settings.ClientId))
            {
                throw new InvalidOperationException("Client ID is missing");
            }
            if (String.IsNullOrEmpty(settings.TenantId))
            {
                throw new InvalidOperationException("Tenant ID is missing");
            }
            if (String.IsNullOrEmpty(settings.RefreshToken))
            {
                throw new InvalidOperationException("Tokens are missing. User did not Authenticate");
            }
            if (String.IsNullOrEmpty(settings.EndpointUri) && String.IsNullOrEmpty(settings.CustomCall))
            {
                throw new InvalidOperationException("Query is missing");
            }
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            // get graph settings
            UserAuthSettings settings;
            try{
                //settings = JsonConvert.DeserializeObject<UserAuthSettings>(data.Parameter);
                settings = UserAuthSettings.GetSettingsFromParameterString(data.Parameter);
                if (settings == null) throw new InvalidOperationException("Invalid parameter format");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Error getting settings for graph: {ex.Message}");
            }
            
            // get a helper
            var helper = GetGraphHelper(settings, data);

            // make graph call
            string request = settings.EndpointUri; //data.Parameter.Split(';')[7];
            string customCall = settings.CustomCall; //data.Parameter.Split(';')[14];
            
            GraphResponse response;
            //if (customCall != "") request = customCall;
            try
            {
                
                if (customCall == "")
                {
                    response = helper.ExtractAsync(request, settings.Parameters/*BuildRequestParameters(data)*/).Result;
                }
                else
                {
                    response = helper.ExtractAsync(customCall, settings.RequestBody).Result;
                }
                //task.Wait();
            }
            catch (AggregateException aex)
            {
                if(aex.InnerException is MsGraphException mex)
                {
                    throw new InvalidOperationException(
                        $"Microsoft Graph Error\n Code: {mex.ErrorCode}\nMessage: {mex.Message}");
                }
                else
                {
                    throw new InvalidOperationException($"Error receiving response from Graph: {aex.InnerException?.Message ?? aex.Message}");
                }
                
            }
            //response = task.Result;

            var cols = new CustomListColumnCollection();

            if(response.Type == GraphContentType.Json){
                // parse json to PB Columns
                try
                {
                    JsonTextReader reader = PreparedReader(response.Content);

                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonToken.StartObject)
                        {
                            JsonHelper.ColumnsWalkThroughObject(reader, "root", cols);
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Error while reading Json response: {ex.Message}");
                }
            }
            else if (response.Type == GraphContentType.OctetStream)
            {
                var reader = new StringReader(response.Content);
                string[] colNames = reader.ReadLine()?.Split(',');

                if (colNames == null)
                {
                    throw new InvalidOperationException("Response is empty");
                }

                foreach (var colName in colNames)
                {
                    cols.Add(new CustomListColumn()
                    {
                        Name = colName,
                        Type = CustomListColumnTypes.String
                    });
                }
            }

            return cols;
        }


        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            // get graph settings
            UserAuthSettings settings;
            try{
                //settings = JsonConvert.DeserializeObject<UserAuthSettings>(data.Parameter);
                settings = UserAuthSettings.GetSettingsFromParameterString(data.Parameter);
                if (settings == null) throw new InvalidOperationException("Invalid parameter format");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Error getting settings for graph: {ex.Message}");
            }
            
            // create an item with empty values
            var expectedKeys = GetColumnsOverride(data);
            var emptyItem = new CustomListObjectElement();
            SetKeys(emptyItem, expectedKeys);

            // get a graph helper
            var helper = GetGraphHelper(settings, data);
            
            // make graph call
            string request = settings.EndpointUri; //data.Parameter.Split(';')[7];
            string customCall = settings.CustomCall; //data.Parameter.Split(';')[14];
            
            GraphResponse response;
            //if (customCall != "") request = customCall;

            try{
                
                if (customCall == "")
                {
                    response = helper.ExtractAsync(request, settings.Parameters/*BuildRequestParameters(data)*/).Result;
                }
                else
                {
                    response = helper.ExtractAsync(customCall, settings.RequestBody).Result;
                }
                //task.Wait();
            }
            catch (AggregateException aex)
            {
                if(aex.InnerException is MsGraphException mex)
                {
                    throw new InvalidOperationException(
                        $"Microsoft Graph Error\n Code: {mex.ErrorCode}\nMessage: {mex.Message}");
                }
                else
                {
                    throw new InvalidOperationException($"Error receiving response from Graph: {aex.InnerException?.Message ?? aex.Message}");
                }
            }
            //response = task.Result;

            var items = new CustomListObjectElementCollection();

            if(response.Type == GraphContentType.Json){
                try
                {
                    // parse response to PB table
                    JsonTextReader reader = PreparedReader(response.Content);
                    JObject jObject = JObject.Parse(response.Content);

                    while (reader.Read())
                    {
                        if (reader.TokenType == JsonToken.StartObject)
                        {
                            var item = CloneItem(emptyItem);
                            JsonHelper.ItemsWalkThroughObject(reader, "root", item, jObject);
                            items.Add(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Error while reading Json response: {ex.Message}");
                }
            }
            else if (response.Type == GraphContentType.OctetStream)
            {
                var reader = new StringReader(response.Content);
                string[] colNames = reader.ReadLine()?.Split(',');

                if (colNames == null)
                {
                    throw new InvalidOperationException("Response is empty");
                }

                string row = reader.ReadLine();
                while(row != null)
                {
                    string[] values = row.Split(',');
                    var item = new CustomListObjectElement();
                    for (int i = 0; i < values.Length; i++)
                    {
                        item.Add(colNames[i], values[i]);
                    }
                    items.Add(item);
                    row = reader.ReadLine();
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
            var settings = UserAuthSettings.GetSettingsFromParameterString(data.Parameter);
            
            string url = "https://graph.microsoft.com/v1.0/me/sendMail";
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

                var helper = GetGraphHelper(settings, data);

                var task = helper.PostAsync(url, formattedBody);
                task.Wait();

                return task.Result;
            }

            return false;
        }

        private bool AddTask(CustomListData data, CustomListExecuteFunctionValueCollection values)
        {
            var settings = UserAuthSettings.GetSettingsFromParameterString(data.Parameter);
            
            string url = "https://graph.microsoft.com/v1.0/me/todo/lists/{0}/tasks";
            string body = "{\"title\": \"$0$\"}";

            if (values.Count == 2)
            {
                var formattedUrl = String.Format(url, values[0].StringValue);
                var formattedBody = body.Replace("$0$", values[1].StringValue);

                var helper = GetGraphHelper(settings, data);

                var task = helper.PostAsync(formattedUrl, formattedBody);
                task.Wait();

                return task.Result;
            }

            return false;
        }

        private bool AddEvent(CustomListData data, CustomListExecuteFunctionValueCollection values)
        {
            var settings = UserAuthSettings.GetSettingsFromParameterString(data.Parameter);
            
            string url = "https://graph.microsoft.com/v1.0/me/events";
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

                var helper = GetGraphHelper(settings, data);

                var task = helper.PostAsync(url, formattedBody);
                task.Wait();

                return task.Result;
            }

            return false;
        }

        #endregion

        #region HelperMethods

        private GraphHelperUserAuth GetGraphHelper(UserAuthSettings settings, CustomListData data)
        {
            this.Log?.Verbose("Initializing GraphHelper");
            GraphHelperUserAuth helper;
            // get refresh token
            string refreshToken = settings.RefreshToken; //data.Parameter.Split(';')[6];

            // check if refresh token is available
            if (string.IsNullOrEmpty(refreshToken))
            {
                // if refresh token isn't available -> user did not authenticate
                throw new InvalidOperationException("Refresh token not initialized: User did not authenticate");
            }
            else
            {
                // get parameters for azure app
                string clientId = settings.ClientId; //data.Parameter.Split(';')[0];
                string tenantId = settings.TenantId;//data.Parameter.Split(';')[1];
                string permissions = settings.Scope; //data.Parameter.Split(';')[2];
                string accessToken = settings.AccessToken; //data.Parameter.Split(';')[3];
                string expiresIn = settings.ExpirationTime;//data.Parameter.Split(';')[4];
                long millis = settings.Millis; //Int64.Parse(data.Parameter.Split(';')[5]);

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
            /*var values = data.Parameter.Split(';');
            values[3] = accessToken;
            values[4] = expiresIn;
            values[5] = millis.ToString();
            values[6] = refreshToken;
            string result = values[0];

            for (int i = 1; i < values.Length; i++)
            {
                result += $";{values[i]}";
            }*/
            var settings = UserAuthSettings.GetSettingsFromParameterString(data.Parameter);
            settings.AccessToken = accessToken;
            settings.ExpirationTime = expiresIn;
            settings.Millis = millis;
            settings.RefreshToken = refreshToken;

            //var result = JsonConvert.SerializeObject(settings);
            var result = settings.GetParameterStringFromSettings();

            data.Parameter = result;
        }

        /*private RequestParameters BuildRequestParameters(CustomListData data)
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
            
        }*/
        
        private void SetKeys(CustomListObjectElement item, CustomListColumnCollection columns)
        {
            foreach (var column in columns)
            {
                var key = column.Name;
                
                switch (column.Type)
                {
                    case CustomListColumnTypes.Boolean:
                        item.Add(key, false); 
                        break;
                    case CustomListColumnTypes.Number:
                        item.Add(key, Double.NaN); 
                        break;
                    case CustomListColumnTypes.String:
                        item.Add(key, "");
                        break;
                }
                
            }
        }

        private CustomListObjectElement CloneItem(CustomListObjectElement item)
        {
            var newItem = new CustomListObjectElement();

            foreach (var key in item.Keys)
            {
                newItem.Add(key, item[key]);
            }

            return newItem;
        }

        #endregion
    }
}