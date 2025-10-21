using System;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Peakboard.ExtensionKit;

namespace WheelMe
{   
    class WheelMeHelper
    {
        private static Dictionary<string, string> myPositionList = new Dictionary<string, string>();

        public static HttpClient GetHttpClient()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (request, cert, chain, errors) =>
                {
                    Console.WriteLine("SSL error skipped");
                    return true;
                }
            };
            return new HttpClient(handler);
        }

        public static string GetPositionNameFromID(HttpClient client, CustomListData data, string ID)
        {
            if (string.IsNullOrEmpty(ID))
                return "";

            if (myPositionList.Count == 0)
            { 
                HttpResponseMessage response = client.GetAsync(data.Properties["BaseURL"] + $"api/public/maps/{data.Properties["FloorID"]}/positions").Result;
                var responseBody = response.Content.ReadAsStringAsync().Result;

                if (response.IsSuccessStatusCode)
                {

                    JArray rawPositionList = JArray.Parse(responseBody);

                    foreach (var row in rawPositionList)
                    {
                        if (!myPositionList.ContainsKey(row["id"]?.ToString()))
                        {
                            myPositionList.Add(row["id"]?.ToString(), row["name"]?.ToString());
                        }
                    }
                }
                else
                {
                    throw new Exception("Error during call of api/public/maps\r\n" + response.StatusCode + response.ReasonPhrase + "\r\n" + responseBody.ToString());
                }
            }
            if (myPositionList.ContainsKey((string)ID))
            {
                return myPositionList[ID].ToString();
            }
            return ID;
        }
    }
}
