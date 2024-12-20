using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReaderIPCHelper.RequestObjects
{
    public class OpenPortRequestObject
    {
        public int Port { get; set; }
        public string Ip { get; set; }
    }
}
