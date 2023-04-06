using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PeakboardExtensionGraph;
using PeakboardExtensionGraph.AppOnly;
using PeakboardExtensionGraph.UserAuth;

namespace ConsoleApplication1
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            /*var helper = new GraphHelperAppOnly("067207ed-41a4-4402-b97f-b977babe0ec9",
                "b4ff9807-402f-42b8-a89d-428363c55de7", "4Sa8Q~kl8UcvQPUrLrMkVudIeIb6XHJ4l8K95cr6");

            helper.InitGraph().Wait();

            var task = helper.MakeGraphCall("/sites", new RequestParameters()
            {
                Top = 1,
                OrderBy = "name"
            });
            task.Wait();

            var response = task.Result;

            JsonTextReader reader = new JsonTextReader(new StringReader(response));

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.String || reader.TokenType == JsonToken.Float ||
                    reader.TokenType == JsonToken.Boolean || reader.TokenType == JsonToken.PropertyName ||
                    reader.TokenType == JsonToken.Integer || reader.TokenType == JsonToken.Null)
                {
                    Console.WriteLine($"Token: {reader.TokenType} Value: {reader.Value}");
                }
                else
                {
                    Console.WriteLine($"Token: {reader.TokenType}");
                }
            }*/

            long time1 = DateTimeOffset.Now.ToUnixTimeSeconds();
            string str =
                "{\r\n    \"message\": {\r\n        \"subject\": \"$0$\",\r\n        \"body\": {\r\n            \"contentType\": \"Text\",\r\n            \"content\": \"$1$\"\r\n        },\r\n        \"toRecipients\": [\r\n            {\r\n                \"emailAddress\": {\r\n                    \"address\": \"$2$\"\r\n                }\r\n            }\r\n        ]\r\n    }\r\n}|{\r\n    \"title\": \"$0$\"\r\n}";
            string[] arr = str.Split('|');
            long time2 = DateTimeOffset.Now.ToUnixTimeSeconds();
            
            
            Console.WriteLine(arr[0]);
            Console.WriteLine();
            Console.WriteLine(arr[1]);
            Console.WriteLine(time2-time1);
            
        }
    }
}