using System;

namespace ReaderIPCHelper.Models
{
    public class RfidResponceObject
    {
        public byte PacketParam;
        public byte LEN;
        public string UID;
        public int PhaseBegin;
        public int PhaseEnd;
        public byte RSSI;
        public int Freqkhz;
        public byte ANT;
        public Int32 Handles;
    }
}
