using Newtonsoft.Json.Linq;
using PeakboardExtensionYarooms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace YaroomsTestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            // Docu: https://www.yarooms.com/help/api/#get-all-meetings

            List<YaroomsMeeting> mymeetings = YaroomsHelper.GetAllMeetings("", "", "");
                
            Console.ReadLine();

        }

    }
}
