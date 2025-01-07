using System;

namespace RfidReader.ResponceObject
{
    public class RFIDTagResponceObject
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
