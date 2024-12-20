using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RfidReader.ResponceObject
{
    public class RFIDTagResponceObject
    {
        public byte PacketParam;
        public byte LEN;
        public string UID;
        public int phase_begin;
        public int phase_end;
        public byte RSSI;
        public int Freqkhz;
        public byte ANT;
        public Int32 Handles;
    }
}
