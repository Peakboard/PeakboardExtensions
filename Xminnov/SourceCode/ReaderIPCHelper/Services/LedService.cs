using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace ReaderIPCHelper.Services
{
    public class LedService
    {
        private readonly string ip;
        private readonly int port;
        private readonly int frmHandle;
        int frmcomportindex;
        List<string> ledmlist = new List<string>();
        bool StopLedThread = false;
        Thread ledthread = null;
        int ledTagType = 0;
        bool isPhase = false;
        byte delayTime = 0;
        byte intervalTime = 0;
        byte rf_switch = 0;
        byte Qvalue = 4;
        byte Session = 0;
        byte Target = 0;
        byte targettimes = 5;
        byte[] antlist = new byte[16];
        int AA_times = 0;
        int fCmdRet;
        int workType = 0;
        byte fComAdr = 0xff;
        byte readerAddr = 255;
        private int total_tagnum = 0;
        byte InAnt = 0;
        bool closeRf =false;
        int CurConType = 0;
        RFIDCallBack elegateRFIDCallBack;
        private int CardNum = 0;
        List<RFIDTag> curList = new List<RFIDTag>();
        List<string> epcs = new List<string>();
        public LedService(string ip,int port,int frmHandle)
        {
            this.ip = ip;
            this.port = port;
            this.frmHandle = frmHandle;
            elegateRFIDCallBack = new RFIDCallBack(GetUid);
        }
        public void GetUid(IntPtr p, Int32 nEvt)
        {
            RFIDTag ce = (RFIDTag)Marshal.PtrToStructure(p, typeof(RFIDTag));
            curList.Add(ce);
            total_tagnum++;
            CardNum++;
        }
        public int Start(bool closeRf, byte delayTime,byte Qvalue, byte Session, byte Target, byte targettimes,byte intervalTim, bool[] ledAnt , List<string> ledmlist, bool inventory,int ledTagType, ref int frmhandle,List<string> epcs)
        {
            int res = 555;
            this.closeRf = closeRf;
            this.delayTime = delayTime;
            this.Qvalue = Qvalue;
            this.Session = Session;
            this.Target = Target;
            this.targettimes = targettimes;
            this.intervalTime = intervalTim;
            this.ledmlist = ledmlist;
            this.epcs = epcs;
            if(frmHandle != 0)
            {
                fCmdRet = RWDev.OpenNetPort(port, ip, ref fComAdr, ref frmhandle);
            }
            else
            {
                fCmdRet = 0;
            }
            if (fCmdRet == 0)
            {
                frmcomportindex = frmhandle;
                if (closeRf)
                {
                    rf_switch = 1;
                }
                else
                {
                    rf_switch = 0;
                }
                Array.Clear(antlist, 0, 16);
                int SelectAntenna = 0;
                for (int i = 0; i < 16; i++)
                {
                    if (ledAnt[i])
                    {
                        antlist[i] = 1;
                        InAnt = (byte)(0x80 + i);
                        SelectAntenna |= (1 << i);
                    }
                }
                if (inventory)
                {
                    workType = 0;
                }
                else
                {
                    workType = 1;
                }
                this.ledTagType = ledTagType;
                res = startLedTag();
            }
             return res;
        }
        byte MaskFlag = 0;
        int startLedTag()
        {
            int res = 555;
            byte opt = 1;
            byte cfgNo = 7;
            byte[] cfgData = new byte[256];
            int len = 3;
            cfgData[0] = 0;
            cfgData[1] = 50;
            cfgData[2] = 0;
            fCmdRet = RWDev.SetCfgParameter(ref fComAdr, opt, cfgNo, cfgData, len, frmcomportindex);
            cfgNo = 0x15;
            len = 1;
            cfgData[0] = delayTime;
            fCmdRet = RWDev.SetCfgParameter(ref fComAdr, opt, cfgNo, cfgData, len, frmcomportindex);
            cfgNo = 0x16;
            len = 1;
            cfgData[0] = rf_switch;
            fCmdRet = RWDev.SetCfgParameter(ref fComAdr, opt, cfgNo, cfgData, len, frmcomportindex);
            AA_times = 0;
            if (workType == 0)
            {
               res = LightAllLeds();
            }
            else
            {
                foreach (var epc in epcs)
                {
                    res = lightLedsWithIds(epc);
                }
            }
            return res;
        }
        int LightAllLeds()
        {
            int res = 555;
            MaskFlag = 0;
            byte MaskMem = 2;
            byte[] MaskAdr = new byte[2];
            MaskAdr[0] = MaskAdr[1] = 0;
            byte MaskLen = 24;
            byte[] MaskData = new byte[20];
            for (int m = 0; m < 16; m++)
            {
                if (antlist[m] == 1)
                {
                    byte antenna = (byte)(m | 0x80);
                    res = scanled(MaskMem, MaskAdr, MaskLen, MaskData, antenna);
                    if (StopLedThread) break;
                }
            }
            return res;
        }
        int lightLedsWithIds(string tagUid)
        {
            int res = 555;
            MaskFlag = 1;
            for (int m = 0;m < epcs.Count; m++)
            {
                string temp = epcs[m];
                byte[] Password = new byte[4];
                Password[0] = Password[1] = Password[2] = Password[3] = 0;
                byte MaskMem = 1;
                byte[] MaskAdr = new byte[2];
                MaskAdr[0] = 0x00;
                MaskAdr[1] = 0x20;
                byte MaskLen = (byte)(temp.Length * 4);
                byte[] MaskData = new byte[100];
                MaskData = HexStringToByteArray(temp);
                for (int p = 0; p < 16; p++)
                {
                    if (antlist[p] == 1)
                    {
                        byte antenna = (byte)(p | 0x80);
                         res = scanled(MaskMem, MaskAdr, MaskLen, MaskData, antenna);
                    }
                }
            }
            return res;
        }

        byte[] HexStringToByteArray(string s)
        {
            s = s.Replace(" ", "");
            byte[] buffer = new byte[s.Length / 2];
            for (int i = 0; i < s.Length; i += 2)
                buffer[i / 2] = (byte)Convert.ToByte(s.Substring(i, 2), 16);
            return buffer;
        }
        int scanled(byte MaskMem, byte[] MaskAdr, byte MaskLen, byte[] MaskData, byte curantenna)
        {
            byte FastFlag = 0;
            byte Ant = 0;
            int TagNum = 0;
            int Totallen = 0;
            byte[] EPC = new byte[50000];
            int CardNum = 0;
            int NewCardNum = 0;
            byte ReadMem = 0;
            byte ReadLen = 1;
            byte[] ReadAdr = new byte[2];
            byte[] Psd = new byte[4];
            byte Scantime = 0;
            int cbtime = System.Environment.TickCount; CardNum = 0;
            FastFlag = 1;
            if (isPhase) Qvalue |= 0x10;
            if (ledTagType == 0)
            {
                ReadMem = 0;
                ReadLen = 1;
                ReadAdr[0] = 0;
                ReadAdr[1] = 4;
            }
            else if (ledTagType == 1)
            {
                ReadMem = 3;
                ReadLen = 1;
                ReadAdr[0] = 0;
                ReadAdr[1] = 112;
            }
            else if (ledTagType == 2)
            {
                ReadMem = 0;
                ReadLen = 1;
                ReadAdr[0] = 0;
                ReadAdr[1] = 5;
            }
            else
            {
                ReadMem = 0;
                ReadLen = 1;
                ReadAdr[0] = 0;
                ReadAdr[1] = 6;
            }   
            Psd[0] = Psd[1] = Psd[2] = Psd[3] = 0;
            int res = fCmdRet = RWDev.InventoryMix_G2(ref fComAdr, Qvalue, Session, MaskMem, MaskAdr, MaskLen, MaskData, MaskFlag, ReadMem, ReadAdr, ReadLen, Psd, Target, curantenna, Scantime, FastFlag, EPC, ref Ant, ref Totallen, ref TagNum, frmcomportindex);
            int cmdTime = System.Environment.TickCount - cbtime;
            if ((fCmdRet != 0x01) && (fCmdRet != 0x02) && (fCmdRet != 0xF8) && (fCmdRet != 0xF9) && (fCmdRet != 0xEE) && (fCmdRet != 0xFF))
            {
                if (CurConType == 1)
                {
                    fCmdRet = RWDev.CloseNetPort(frmcomportindex);
                    if (fCmdRet == 0) frmcomportindex = -1;
                    fComAdr = 255;
                    string ipAddress = ip;
                    int nPort = port;
                    fCmdRet = RWDev.OpenNetPort(nPort, ipAddress, ref fComAdr, ref frmcomportindex);
                    if (fCmdRet == 0)
                    {
                        if (frmcomportindex > 0)
                            RWDev.InitRFIDCallBack(elegateRFIDCallBack, true, frmcomportindex);
                    }
                }
            }
            if (CardNum == 0)
            {
                AA_times++;
                if (AA_times > targettimes)
                {
                    AA_times = 0;
                    if (Session > 1)
                        Target = (byte)(1 - Target);
                    else
                        Target = 0;
                }
            }
            else
            {
                AA_times = 0;
            }
            return res;
        }
    }
}
