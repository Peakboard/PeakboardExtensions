
using System.Collections.Generic;

namespace ReaderIPCHelper.RequestObjects
{
    public class LedsRequestObject
    {
        public int Port { get; set; }
        public int LedTagType { get; set; }
        public bool IsPhase { get; set; }
        public byte DelayTime { get; set; }
        public byte IntervalTime { get; set; }
        public byte Qvalue { get; set; }
        public byte Session { get; set; }
        public byte Target { get; set; }
        public byte TargetTimes { get; set; }
        public bool[] AntList { get; set; }
        public string Ip { get; set; }
        public bool CloseRf { get; set; }
        public List<string> Epcs { get; set; }
    }
}
