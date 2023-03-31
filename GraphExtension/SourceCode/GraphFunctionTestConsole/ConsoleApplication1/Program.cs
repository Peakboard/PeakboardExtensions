using System;
using System.Collections.Generic;
using PeakboardExtensionGraph;

namespace ConsoleApplication1
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            var template =
                "(\"message\":(\"subject\":\"{0}\",\"body\":(\"contentType\":null,\"content\":\"{1}\"),\"toRecipients\":[(\"emailAddress\":(\"name\":null,\"address\":\"{2}\"))]))";

            var recipient = "yh@email.com";
            var header = "test";
            var body = "body";

            var requestBody = String.Format(template, header, body, recipient);
            requestBody = requestBody.Replace('(', '{');
            requestBody = requestBody.Replace(')', '}');

            //Console.WriteLine(requestBody);

            var str = "{test:$test$,\ntemp:$temp$}";

            int begin = str.IndexOf('$');
            int end;
            var values = new List<string>();
            
            while (begin != -1)
            {
                end = str.IndexOf('$', begin + 1);
                if (end == -1) throw new Exception("End of placeholder not defined");
                string value = "";
                
                for (int i = begin+1; i < end; i++)
                {
                    value += str[i];
                    //str.Replace()
                }
                

            }
            
            Console.WriteLine(str);
        }
    }
}