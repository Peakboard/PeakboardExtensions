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

            var str = "{test: $0$,\ntemp: $1$ }";

            for (int i = 0; i < 2; i++)
            {
                str = str.Replace($"${i}$", "abc");
            }

            Console.WriteLine(str);
        }
    }
}