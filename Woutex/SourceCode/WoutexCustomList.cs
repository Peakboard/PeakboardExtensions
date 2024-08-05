using System;
using System.Net.Http;
using System.Collections.Generic;
using Peakboard.ExtensionKit;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Net;

namespace Woutex
{
    [Serializable]
    [CustomListIcon("Woutex.Woutex.png")]
    
    class WoutexCustomList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "Woutex",
                Name = "Woutex e-ink displays",
                Description = "Fetches data from Woutex API",
                PropertyInputPossible = true,
                PropertyInputDefaults = {
                new CustomListPropertyDefinition() { Name = "BaseURL", Value = "https://ec2-54-175-232-22.compute-1.amazonaws.com/" },
                new CustomListPropertyDefinition() { Name = "UserName", Value = "wtx" },
                new CustomListPropertyDefinition() { Name = "Password", Masked = true, Value="1a7cf68fa7ecc6fef376dfa44999b89f"  }
                    },
                Functions = new CustomListFunctionDefinitionCollection
                {
                    new CustomListFunctionDefinition
                    {
                        Name = "ChangeContent",
                        Description = "Sends Content to a display",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "DisplayID",
                                Type = CustomListFunctionParameterTypes.String,
                                Optional = false,
                                Description = "ID of the display"
                            },
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "Content",
                                Type = CustomListFunctionParameterTypes.String,
                                Optional = false,
                                Description = "The actual content"
                            },
                        },
                    },
                                        new CustomListFunctionDefinition
                    {
                        Name = "ResetContent",
                        Description = "Resets the content of a display",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "DisplayID",
                                Type = CustomListFunctionParameterTypes.String,
                                Optional = false,
                                Description = "ID of the display"
                            },
                        },
                    }
                }     
            };
        }

        protected override void CheckDataOverride(CustomListData data)
        {
            if (string.IsNullOrWhiteSpace(data.Properties["BaseURL"]))
            {
                throw new InvalidOperationException("Invalid BaseURL");
            }
            if (string.IsNullOrWhiteSpace(data.Properties["UserName"]))
            {
                throw new InvalidOperationException("Invalid UserName");
            }
            if (string.IsNullOrWhiteSpace(data.Properties["Password"]))
            {
                throw new InvalidOperationException("Invalid Password");
            }
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            var columns = new CustomListColumnCollection();
            columns.Add(new CustomListColumn("Dummy", CustomListColumnTypes.String));
            return columns;
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            var items = new CustomListObjectElementCollection();
            var item = new CustomListObjectElement();
            item.Add("Dummy", "Dummy");
            return items;
        }


        protected override CustomListExecuteReturnContext ExecuteFunctionOverride(CustomListData data, CustomListExecuteParameterContext context)
        {
            var returnContext = default(CustomListExecuteReturnContext);
            string BaseURL = data.Properties["BaseURL"];

            if (BaseURL.EndsWith("/"))
                BaseURL += "/";

            if (context.FunctionName.Equals("ChangeContent", StringComparison.InvariantCultureIgnoreCase))
            {
                string DisplayID = context.Values[0].StringValue;

                using (HttpClient client = new HttpClient())
                {
                    var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes(data.Properties["UserName"] + ":" + data.Properties["Password"]));
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);

                    this.Log?.Info(authString);

                    ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => { return true; };

                    var content = new StringContent(context.Values[1].StringValue, System.Text.Encoding.UTF8, "application/json");
                    HttpResponseMessage response = client.PutAsync(BaseURL + "flex/v2/displays/" + DisplayID + "/content", content).Result;
                    var responseBody = response.Content.ReadAsStringAsync().Result;

                    if (response.IsSuccessStatusCode)
                    {
                        this.Log?.Info($"Display {DisplayID} succesfully set to content " + context.Values[1].StringValue);
                    }
                    else
                    {
                        this.Log?.Info("Error during call -> " + response.StatusCode + response.ReasonPhrase);
                        this.Log?.Info(responseBody.ToString());
                    }
                }
            }
            else if (context.FunctionName.Equals("ResetContent", StringComparison.InvariantCultureIgnoreCase))
            {
                string DisplayID = context.Values[0].StringValue;

                using (HttpClient client = new HttpClient())
                {
                    var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes(data.Properties["UserName"] + ":" + data.Properties["Password"]));
                    client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);

                    this.Log?.Info(authString);

                    ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => { return true; };

                    HttpResponseMessage response = client.DeleteAsync(BaseURL + "flex/v2/displays/" + DisplayID + "/content").Result;
                    var responseBody = response.Content.ReadAsStringAsync().Result;

                    if (response.IsSuccessStatusCode)
                    {
                        this.Log?.Info($"Display {DisplayID} succesfully cleared");
                    }
                    else
                    {
                        this.Log?.Info("Error during call -> " + response.StatusCode + response.ReasonPhrase);
                        this.Log?.Info(responseBody.ToString());
                    }
                }
            }
            else
            {
                throw new DataErrorException("Function is not supported in this version.");
            }

            return returnContext;
        }
    }
}
