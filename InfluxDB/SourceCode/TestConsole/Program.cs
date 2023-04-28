using System;
using System.Globalization;

namespace TestConsole
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            string s = "4.5";
            
            Console.WriteLine(Double.Parse(s, CultureInfo.InvariantCulture));

        }
    }
}