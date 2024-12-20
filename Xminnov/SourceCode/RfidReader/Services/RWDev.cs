using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;
using static System.Net.Mime.MediaTypeNames;
namespace RfidReader.Services
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RFIDTag
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

    public delegate void RFIDCallBack(IntPtr p, Int32 nEvt);

    public delegate void RfidTagCallBack(RFIDTag mtag);

    public static class RWDev
    {
        private const string DLLNAME = @"UHFReader288.dll";

        static RfidTagCallBack callback = null;
        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        internal static extern void InitRFIDCallBack(RFIDCallBack t, bool uidBack, int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int OpenNetPort(int Port,
                                             string IPaddr,
                                             ref byte ComAddr,
                                             ref int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int CloseNetPort(int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int OpenComPort(int Port,
                                                 ref byte ComAddr,
                                                 byte Baud,
                                                 ref int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int CloseComPort();

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int AutoOpenComPort(ref int Port,
                                                 ref byte ComAddr,
                                                 byte Baud,
                                                 ref int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int CloseSpecComPort(int Port);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int OpenUSBPort(ref byte ComAddr,
                                                 ref int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int CloseUSBPort(int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetReaderInformation(ref byte ComAdr,              //读写器地址		
                                                      byte[] VersionInfo,           //软件版本
                                                      ref byte ReaderType,              //读写器型号
                                                      ref byte TrType,      //支持的协议
                                                      ref byte dmaxfre,           //当前读写器使用的最高频率
                                                      ref byte dminfre,           //当前读写器使用的最低频率
                                                      ref byte powerdBm,             //读写器的输出功率
                                                      ref byte ScanTime,
                                                      ref byte Ant,
                                                      ref byte BeepEn,
                                                      ref byte OutputRep,
                                                      ref byte CheckAnt,
                                                      int FrmHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetRegion(ref byte ComAdr,
                                           byte dmaxfre,
                                           byte dminfre,
                                           int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetAddress(ref byte ComAdr,
                                             byte ComAdrData,
                                             int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetInventoryScanTime(ref byte ComAdr,
                                               byte ScanTime,
                                               int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetBaudRate(ref byte ComAdr,
                                           byte baud,
                                           int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetRfPower(ref byte ComAdr,
                                             byte powerDbm,
                                             int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int BuzzerAndLEDControl(ref byte ComAdr,
                                                     byte AvtiveTime,
                                                     byte SilentTime,
                                                     byte Times,
                                                     int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetWorkMode(ref byte ComAdr,
                                             byte Read_mode,
                                             int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetAntennaMultiplexing(ref byte ComAdr,
                                            byte Ant,
                                            int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetBeepNotification(ref byte ComAdr,
                                         byte BeepEn,
                                         int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetReal_timeClock(ref byte ComAdr,
                                          byte[] paramer,
                                          int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetTime(ref byte ComAdr,
                                          byte[] paramer,
                                          int frmComPortindex);


        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetRelay(ref byte ComAdr,
                                          byte RelayTime,
                                          int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetGPIO(ref byte ComAdr,
                                         byte OutputPin,
                                         int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetGPIOStatus(ref byte ComAdr,
                                         ref byte OutputPin,
                                         int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetNotificationPulseOutput(ref byte ComAdr,
                                              byte OutputRep,
                                              int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetSystemParameter(ref byte ComAdr,
                                                      ref byte Read_mode,
                                                      ref byte Accuracy,
                                                      ref byte RepCondition,
                                                      ref byte RepPauseTime,
                                                      ref byte ReadPauseTim,
                                                      ref byte TagProtocol,
                                                      ref byte MaskMem,
                                                      byte[] MaskAdr,
                                                      ref byte MaskLen,
                                                      byte[] MaskData,
                                                      ref byte TriggerTime,
                                                      ref byte AdrTID,
                                                      ref byte LenTID,
                                                      int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetEASSensitivity(ref byte ComAdr,
                                             byte Accuracy,
                                             int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetTriggerTime(ref byte ComAdr,
                                             byte TriggerTime,
                                             int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetTIDParameter(ref byte ComAdr,
                                             byte AdrTID,
                                             byte LenTID,
                                             int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetMask(ref byte ComAdr,
                                         byte MaskMem,
                                         byte[] MaskAdr,
                                         byte MaskLen,
                                         byte[] MaskData,
                                         int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetResponsePamametersofAuto_runningMode(ref byte ComAdr,
                                                 byte RepCondition,
                                                 byte RepPauseTime,
                                                 int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetInventoryInterval(ref byte ComAdr,
                                                  byte ReadPauseTim,
                                                  int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SelectTagType(ref byte ComAdr,
                                                byte Protocol,
                                                int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetCommType(ref byte ComAdr,
                                                byte CommType,
                                                int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetTagBufferInfo(ref byte ComAdr,
                                                   byte[] Data,
                                                   ref int dataLength,
                                                   int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int ClearTagBuffer(ref byte ComAdr,
                                             int frmComPortindex);




        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int ReadActiveModeData(byte[] ScanModeData,
                                                    ref int ValidDatalength,
                                                    int frmComPortindex);


        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int Inventory_G2(ref byte ComAdr,
                                              byte QValue,
                                              byte Session,
                                              byte MaskMem,
                                              byte[] MaskAdr,
                                              byte MaskLen,
                                              byte[] MaskData,
                                              byte MaskFlag,
                                              byte AdrTID,
                                              byte LenTID,
                                              byte TIDFlag,
                                              byte Target,
                                              byte InAnt,
                                              byte Scantime,
                                              byte FastFlag,
                                              byte[] pEPCList,
                                              ref byte Ant,
                                              ref int Totallen,
                                              ref int CardNum,
                                              int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int InventoryMix_G2(ref byte ComAdr,
                                              byte QValue,
                                              byte Session,
                                              byte MaskMem,
                                              byte[] MaskAdr,
                                              byte MaskLen,
                                              byte[] MaskData,
                                              byte MaskFlag,
                                              byte ReadMem,
                                              byte[] ReadAdr,
                                              byte ReadLen,
                                              byte[] Psd,
                                              byte Target,
                                              byte InAnt,
                                              byte Scantime,
                                              byte FastFlag,
                                              byte[] pEPCList,
                                              ref byte Ant,
                                              ref int Totallen,
                                              ref int CardNum,
                                              int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int ReadData_G2(ref byte ComAdr,
                                             byte[] EPC,
                                             byte ENum,
                                             byte Mem,
                                             byte WordPtr,
                                             byte Num,
                                             byte[] Password,
                                             byte MaskMem,
                                             byte[] MaskAdr,
                                             byte MaskLen,
                                             byte[] MaskData,
                                             byte[] Data,
                                             ref int errorcode,
                                             int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int WriteData_G2(ref byte ComAdr,
                                              byte[] EPC,
                                              byte WNum,
                                              byte ENum,
                                              byte Mem,
                                              byte WordPtr,
                                              byte[] Wdt,
                                              byte[] Password,
                                              byte MaskMem,
                                              byte[] MaskAdr,
                                              byte MaskLen,
                                              byte[] MaskData,
                                              ref int errorcode,
                                              int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int WriteEPC_G2(ref byte ComAdr,
                                             byte[] Password,
                                             byte[] WriteEPC,
                                             byte ENum,
                                             ref int errorcode,
                                             int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int KillTag_G2(ref byte ComAdr,
                                                byte[] EPC,
                                                byte ENum,
                                                byte[] Password,
                                                byte MaskMem,
                                                byte[] MaskAdr,
                                                byte MaskLen,
                                                byte[] MaskData,
                                                ref int errorcode,
                                                int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int Lock_G2(ref byte ComAdr,
                                                   byte[] EPC,
                                                   byte ENum,
                                                   byte select,
                                                   byte setprotect,
                                                   byte[] Password,
                                                   byte MaskMem,
                                                   byte[] MaskAdr,
                                                   byte MaskLen,
                                                   byte[] MaskData,
                                                   ref int errorcode,
                                                   int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int BlockErase_G2(ref byte ComAdr,
                                              byte[] EPC,
                                              byte ENum,
                                              byte Mem,
                                              byte WordPtr,
                                              byte Num,
                                              byte[] Password,
                                              byte MaskMem,
                                              byte[] MaskAdr,
                                              byte MaskLen,
                                              byte[] MaskData,
                                              ref int errorcode,
                                              int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetPrivacyWithoutEPC_G2(ref byte ComAdr,
                                                          byte[] Password,
                                                          ref int errorcode,
                                                          int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetPrivacyByEPC_G2(ref byte ComAdr,
                                                  byte[] EPC,
                                                  byte ENum,
                                                  byte[] Password,
                                                  byte MaskMem,
                                                  byte[] MaskAdr,
                                                  byte MaskLen,
                                                  byte[] MaskData,
                                                  ref int errorcode,
                                                  int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int ResetPrivacy_G2(ref byte ComAdr,
                                                      byte[] Password,
                                                      ref int errorcode,
                                                      int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int CheckPrivacy_G2(ref byte ComAdr,
                                                      ref byte readpro,
                                                      ref int errorcode,
                                                      int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int EASConfigure_G2(ref byte ComAdr,
                                                  byte[] EPC,
                                                  byte ENum,
                                                  byte[] Password,
                                                  byte EAS,
                                                  byte MaskMem,
                                                  byte[] MaskAdr,
                                                  byte MaskLen,
                                                  byte[] MaskData,
                                                  ref int errorcode,
                                                  int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int EASAlarm_G2(ref byte ComAdr,
                                                  ref int errorcode,
                                                  int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int BlockLock_G2(ref byte ComAdr,
                                                  byte[] EPC,
                                                  byte ENum,
                                                  byte[] Password,
                                                  byte WrdPointer,
                                                  byte MaskMem,
                                                  byte[] MaskAdr,
                                                  byte MaskLen,
                                                  byte[] MaskData,
                                                  ref int errorcode,
                                                  int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int BlockWrite_G2(ref byte ComAdr,
                                              byte[] EPC,
                                              byte WNum,
                                              byte ENum,
                                              byte Mem,
                                              byte WordPtr,
                                              byte[] Wdt,
                                              byte[] Password,
                                              byte MaskMem,
                                              byte[] MaskAdr,
                                              byte MaskLen,
                                              byte[] MaskData,
                                              ref int errorcode,
                                              int frmComPortindex);


        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int ChangeATMode(ref byte ConAddr,
                                               byte ATMode,
                                               int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int TransparentCMD(ref byte ConAddr,
                                               byte timeout,
                                               byte cmdlen,
                                               byte[] cmddata,
                                               ref byte recvLen,
                                               byte[] recvdata,
                                               int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetSeriaNo(ref byte ConAddr,
                                               byte[] SeriaNo,
                                               int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetCheckAnt(ref byte ComAdr,
                                             byte CheckAnt,
                                             int frmComPortindex);


        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int InventorySingle_6B(ref byte ConAddr,
                                                  ref byte ant,
                                                  byte[] ID_6B,
                                                  int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int InventoryMultiple_6B(ref byte ConAddr,
                                               byte Condition,
                                               byte StartAddress,
                                               byte mask,
                                               byte[] ConditionContent,
                                               ref byte ant,
                                               byte[] ID_6B,
                                               ref int Cardnum,
                                               int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int ReadData_6B(ref byte ConAddr,
                                               byte[] ID_6B,
                                               byte StartAddress,
                                               byte Num,
                                               byte[] Data,
                                               ref int errorcode,
                                               int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int WriteData_6B(ref byte ConAddr,
                                               byte[] ID_6B,
                                               byte StartAddress,
                                               byte[] Writedata,
                                               byte Writedatalen,
                                               ref int writtenbyte,
                                               ref int errorcode,
                                               int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int Lock_6B(ref byte ConAddr,
                                               byte[] ID_6B,
                                               byte Address,
                                               ref int errorcode,
                                               int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int CheckLock_6B(ref byte ConAddr,
                                               byte[] ID_6B,
                                               byte Address,
                                               ref byte ReLockState,
                                               ref int errorcode,
                                               int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetQS(ref byte ConAddr,
                                               byte Qvalue,
                                               byte Session,
                                               int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetQS(ref byte ConAddr,
                                       ref byte Qvalue,
                                       ref byte Session,
                                       int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetFlashRom(ref byte ConAddr,
                                       int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetModuleVersion(ref byte ConAddr,
                                               byte[] Version,
                                               int PortHandle);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int ExtReadData_G2(ref byte ComAdr,
                                             byte[] EPC,
                                             byte ENum,
                                             byte Mem,
                                             byte[] WordPtr,
                                             byte Num,
                                             byte[] Password,
                                             byte MaskMem,
                                             byte[] MaskAdr,
                                             byte MaskLen,
                                             byte[] MaskData,
                                             byte[] Data,
                                             ref int errorcode,
                                             int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int ExtWriteData_G2(ref byte ComAdr,
                                              byte[] EPC,
                                              byte WNum,
                                              byte ENum,
                                              byte Mem,
                                              byte[] WordPtr,
                                              byte[] Wdt,
                                              byte[] Password,
                                              byte MaskMem,
                                              byte[] MaskAdr,
                                              byte MaskLen,
                                              byte[] MaskData,
                                              ref int errorcode,
                                              int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int InventoryBuffer_G2(ref byte ComAdr,
                                              byte QValue,
                                              byte Session,
                                              byte MaskMem,
                                              byte[] MaskAdr,
                                              byte MaskLen,
                                              byte[] MaskData,
                                              byte MaskFlag,
                                              byte AdrTID,
                                              byte LenTID,
                                              byte TIDFlag,
                                              byte Target,
                                              byte InAnt,
                                              byte Scantime,
                                              byte FastFlag,
                                              ref int BufferCount,
                                              ref int TagNum,
                                              int frmComPortindex);


        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetSaveLen(ref byte ComAdr,
                                              byte SaveLen,
                                              int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetSaveLen(ref byte ComAdr,
                                            ref byte SaveLen,
                                            int frmComPortindex);


        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int ReadBuffer_G2(ref byte ComAdr,
                                              ref int Totallen,
                                              ref int CardNum,
                                              byte[] pEPCList,
                                              int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int ClearBuffer_G2(ref byte ComAdr,
                                              int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetBufferCnt_G2(ref byte ComAdr,
                                               ref int Count,
                                              int frmComPortindex);


        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetReadMode(ref byte ComAdr,
                                             byte ReadMode,
                                              int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetReadParameter(ref byte ComAdr,
                                              byte[] Parameter,
                                              byte MaskMem,
                                              byte[] MaskAdr,
                                              byte MaskLen,
                                              byte[] MaskData,
                                              byte MaskFlag,
                                              byte AdrTID,
                                              byte LenTID,
                                              byte TIDFlag,
                                              int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetReadParameter(ref byte ComAdr,
                                             byte[] Parameter,
                                              int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int WriteRfPower(ref byte ComAdr,
                                             byte powerDbm,
                                             int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int ReadRfPower(ref byte ComAdr,
                                             ref byte powerDbm,
                                             int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int RetryTimes(ref byte ComAdr,
                                             ref byte Times,
                                             int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetDRM(ref byte ComAdr,
                                             byte DRM,
                                             int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetDRM(ref byte ComAdr,
                                             ref byte DRM,
                                             int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetReaderTemperature(ref byte ComAdr,
                                             ref byte PlusMinus,
                                             ref byte Temperature,
                                             int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int MeasureReturnLoss(ref byte ComAdr,
                                             byte[] TestFreq,
                                             byte Ant,
                                             ref byte ReturnLoss,
                                             int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetProfile(ref byte ComAdr,
                                             ref byte Profile,
                                             int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetAntenna(ref byte ComAdr,
                                             byte SetOnce,
                                            byte AntCfg1,
                                            byte AntCfg0,
                                            int frmComPortindex);


        /********国军标API**********/
        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int Inventory_JB(ref byte ComAdr,
                                              byte Algo,
                                              byte MaskMem,
                                              byte[] MaskAdr,
                                              byte MaskLen,
                                              byte[] MaskData,
                                              byte MaskFlag,
                                              byte InAnt,
                                              byte Scantime,
                                              byte FastFlag,
                                              byte[] pEPCList,
                                              ref byte Ant,
                                              ref int Totallen,
                                              ref int CardNum,
                                              int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int InventoryMix_JB(ref byte ComAdr,
                                              byte Algo,
                                              byte MaskMem,
                                              byte[] MaskAdr,
                                              byte MaskLen,
                                              byte[] MaskData,
                                              byte MaskFlag,
                                              byte ReadMem,
                                              byte[] ReadAdr,
                                              byte ReadLen,
                                              byte[] Psd,
                                              byte InAnt,
                                              byte Scantime,
                                              byte FastFlag,
                                              byte[] pEPCList,
                                              ref byte Ant,
                                              ref int Totallen,
                                              ref int CardNum,
                                              int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int ReadData_JB(ref byte ComAdr,
                                             byte[] TagID,
                                             byte TNum,
                                             byte ReadMem,
                                             byte[] WordPtr,
                                             byte WordNum,
                                             byte[] Password,
                                             byte MaskMem,
                                             byte[] MaskAdr,
                                             byte MaskLen,
                                             byte[] MaskData,
                                             byte[] Data,
                                             ref int errorcode,
                                             int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int WriteData_JB(ref byte ComAdr,
                                              byte[] TagID,
                                              byte WNum,
                                              byte TNum,
                                              byte WMem,
                                              byte[] WordPtr,
                                              byte[] Wdt,
                                              byte[] Password,
                                              byte MaskMem,
                                              byte[] MaskAdr,
                                              byte MaskLen,
                                              byte[] MaskData,
                                              ref int errorcode,
                                              int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int BlockErase_JB(ref byte ComAdr,
                                              byte[] TagID,
                                              byte TNum,
                                              byte EMem,
                                              byte[] WordPtr,
                                              byte[] ENum,
                                              byte[] Password,
                                              byte MaskMem,
                                              byte[] MaskAdr,
                                              byte MaskLen,
                                              byte[] MaskData,
                                              ref int errorcode,
                                              int frmComPortindex);



        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int Lock_JB(ref byte ComAdr,
                                                   byte[] TagID,
                                                   byte TNum,
                                                   byte LockMem,
                                                   byte Cfg,
                                                   byte Action,
                                                   byte[] Password,
                                                   byte MaskMem,
                                                   byte[] MaskAdr,
                                                   byte MaskLen,
                                                   byte[] MaskData,
                                                   ref int errorcode,
                                                   int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int KillTag_JB(ref byte ComAdr,
                                                byte[] TagID,
                                                byte TNum,
                                                byte[] Killpwd,
                                                byte MaskMem,
                                                byte[] MaskAdr,
                                                byte MaskLen,
                                                byte[] MaskData,
                                                ref int errorcode,
                                                int frmComPortindex);


        /********国标API**********/
        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int Inventory_GB(ref byte ComAdr,
                                              byte MaskMem,
                                              byte[] MaskAdr,
                                              byte MaskLen,
                                              byte[] MaskData,
                                              byte MaskFlag,
                                              byte InAnt,
                                              byte Scantime,
                                              byte FastFlag,
                                              byte[] pEPCList,
                                              ref byte Ant,
                                              ref int Totallen,
                                              ref int CardNum,
                                              int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int InventoryMix_GB(ref byte ComAdr,
                                              byte MaskMem,
                                              byte[] MaskAdr,
                                              byte MaskLen,
                                              byte[] MaskData,
                                              byte MaskFlag,
                                              byte ReadMem,
                                              byte[] ReadAdr,
                                              byte ReadLen,
                                              byte[] Psd,
                                              byte InAnt,
                                              byte Scantime,
                                              byte FastFlag,
                                              byte[] pEPCList,
                                              ref byte Ant,
                                              ref int Totallen,
                                              ref int CardNum,
                                              int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int ReadData_GB(ref byte ComAdr,
                                             byte[] TagID,
                                             byte TNum,
                                             byte ReadMem,
                                             byte[] WordPtr,
                                             byte WordNum,
                                             byte[] Password,
                                             byte MaskMem,
                                             byte[] MaskAdr,
                                             byte MaskLen,
                                             byte[] MaskData,
                                             byte[] Data,
                                             ref int errorcode,
                                             int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int WriteData_GB(ref byte ComAdr,
                                              byte[] TagID,
                                              byte WNum,
                                              byte TNum,
                                              byte WMem,
                                              byte[] WordPtr,
                                              byte[] Wdt,
                                              byte[] Password,
                                              byte MaskMem,
                                              byte[] MaskAdr,
                                              byte MaskLen,
                                              byte[] MaskData,
                                              ref int errorcode,
                                              int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int BlockErase_GB(ref byte ComAdr,
                                              byte[] TagID,
                                              byte TNum,
                                              byte EMem,
                                              byte[] WordPtr,
                                              byte[] ENum,
                                              byte[] Password,
                                              byte MaskMem,
                                              byte[] MaskAdr,
                                              byte MaskLen,
                                              byte[] MaskData,
                                              ref int errorcode,
                                              int frmComPortindex);



        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int Lock_GB(ref byte ComAdr,
                                                   byte[] TagID,
                                                   byte TNum,
                                                   byte LockMem,
                                                   byte Cfg,
                                                   byte Action,
                                                   byte[] Password,
                                                   byte MaskMem,
                                                   byte[] MaskAdr,
                                                   byte MaskLen,
                                                   byte[] MaskData,
                                                   ref int errorcode,
                                                   int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int KillTag_GB(ref byte ComAdr,
                                                byte[] TagID,
                                                byte TNum,
                                                byte[] Killpwd,
                                                byte MaskMem,
                                                byte[] MaskAdr,
                                                byte MaskLen,
                                                byte[] MaskData,
                                                ref int errorcode,
                                                int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetAntennaPower(ref byte ComAdr,
                                             byte[] powerDbm,
                                             int length,
                                             int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetAntennaPower(ref byte ComAdr,
                                             byte[] powerDbm,
                                             ref int length,
                                             int frmComPortindex);


        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int StopImmediately(ref byte ComAdr, int frmComPortindex);


        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetDwellTime(ref byte ComAdr,
                                             byte flat,
                                             byte FCCOnTime, byte S1Time,
                                             int frmComPortindex);


        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetDwellTime(ref byte ComAdr,
                                             ref byte FCCOnTime, ref byte S1Time,
                                             int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetCfgParameter(ref byte ComAdr,
                                             byte opt,
                                             byte cfgNo, byte[] cfgData, int len,
                                             int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetCfgParameter(ref byte ComAdr,
                                             byte cfgNo, byte[] cfgData, ref int len,
                                             int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int StartRead(ref byte ComAdr, byte Target, int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int StopRead(ref byte ComAdr, int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetRfidTagData(ref byte ComAdr, byte[] rfiddata, ref int len, int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SelectCmdWithCarrier(ref byte ComAdr,
                                              byte Antenna,
                                              byte Session,
                                              byte SelAction,
                                              byte MaskMem,
                                              byte[] MaskAdr,
                                              byte MaskLen,
                                              byte[] MaskData,
                                              byte Truncate,
                                              byte CarrierTime,
                                              int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SelectCMDByAntenna(ref byte ComAdr,
                                              int Antenna, int portNum,
                                              byte Session,
                                              byte SelAction,
                                              byte MaskMem,
                                              byte[] MaskAdr,
                                              byte MaskLen,
                                              byte[] MaskData,
                                              byte Truncate,
                                              int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetCustomRegion(ref byte ComAdr,
                                              byte opt,
                                              byte freSpace,
                                              byte freNum,
                                              byte[] freStart,
                                              int frmComPortindex);

        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetCustomRegion(ref byte ComAdr,
                                              ref byte freSpace,
                                              ref byte freNum,
                                              byte[] freStart,
                                              int frmComPortindex);


        [DllImport(DLLNAME, CallingConvention = CallingConvention.StdCall)]
        public static extern int GetBxTagTemperature(ref byte ComAdr,
                                             byte ENum,
                                             byte[] EPC,
                                             byte[] TempData,
                                             ref int Errorcode,
                                             int frmComPortindex);
        /// <summary>
        /// 16进制数组字符串转换
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        #region
        public static byte[] HexStringToByteArray(string s)
        {
            if (s == "" || s == null) return null;
            s = s.Replace(" ", "");
            byte[] buffer = new byte[s.Length / 2];
            for (int i = 0; i < s.Length; i += 2)
                buffer[i / 2] = (byte)Convert.ToByte(s.Substring(i, 2), 16);
            return buffer;
        }

        public static string ByteArrayToHexString(byte[] data)
        {
            StringBuilder sb = new StringBuilder(data.Length * 3);
            foreach (byte b in data)
                sb.Append(Convert.ToString(b, 16).PadLeft(2, '0'));
            return sb.ToString().ToUpper();

        }
        #endregion

        private static volatile bool toStopThread = false;
        private static Thread mythread = null;
        private static byte ComAddr = 255;
        private static int FrmHandle = -1;
        public static int StartInventory(ref byte ComAdr, byte Target, RfidTagCallBack t, int frmComPortindex)
        {
            int fCmdRet = StartRead(ref ComAdr, Target, frmComPortindex);
            if (fCmdRet == 0)
            {
                ComAddr = ComAdr;
                FrmHandle = frmComPortindex;
                callback = t;
                if (mythread == null)
                {
                    toStopThread = false;
                    mythread = new Thread(workProcess);
                    mythread.IsBackground = true;
                    mythread.Start();
                }
            }
            return 0;
        }


        public static int StopInventory(ref byte ComAdr, int frmComPortindex)
        {
            toStopThread = true;

            Thread.Sleep(15);
            int fCmdRet = StopRead(ref ComAdr, frmComPortindex);
            return fCmdRet;
        }

        public static bool CheckCRC(string s)
        {
            int i, j;
            int current_crc_value;
            byte crcL, crcH;
            byte[] data = HexStringToByteArray(s);
            current_crc_value = 0xFFFF;
            for (i = 0; i <= (data.Length - 1); i++)
            {
                current_crc_value = current_crc_value ^ (data[i]);
                for (j = 0; j < 8; j++)
                {
                    if ((current_crc_value & 0x01) != 0)
                        current_crc_value = (current_crc_value >> 1) ^ 0x8408;
                    else
                        current_crc_value = (current_crc_value >> 1);
                }
            }
            crcL = Convert.ToByte(current_crc_value & 0xFF);
            crcH = Convert.ToByte((current_crc_value >> 8) & 0xFF);
            if (crcH == 0 && crcL == 0)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        public static void workProcess()
        {
            string fInventory_EPC_List = ""; //存贮询查列表（如果读取的数据没有变化，则不进行刷新） 
            long startTime = System.Environment.TickCount;
            while (!toStopThread)
            {
                byte[] rfidData = new byte[4096];
                int nLen, NumLen;
                string temp1 = "";
                string RSSI = "";
                string AntStr = "";
                string lenstr = "";
                string EPCStr = "";
                int ValidDatalength;
                string temp;
                ValidDatalength = 0;
                int fCmdRet = GetRfidTagData(ref ComAddr, rfidData, ref ValidDatalength, FrmHandle);
                if (fCmdRet == 0)
                {
                    startTime = System.Environment.TickCount;
                    try
                    {
                        byte[] daw = new byte[ValidDatalength];
                        Array.Copy(rfidData, 0, daw, 0, ValidDatalength);
                        temp = ByteArrayToHexString(daw);
                        fInventory_EPC_List = fInventory_EPC_List + temp;//把字符串存进列表
                        nLen = fInventory_EPC_List.Length;
                        while (fInventory_EPC_List.Length > 18)
                        {
                            string FlagStr = "EE00";//查找头位置标志字符串
                            int nindex = fInventory_EPC_List.IndexOf(FlagStr);
                            if (nindex > 3)
                                fInventory_EPC_List = fInventory_EPC_List.Substring(nindex - 4);
                            else
                            {
                                fInventory_EPC_List = fInventory_EPC_List.Substring(2);
                                continue;
                            }
                            NumLen = Convert.ToInt32(fInventory_EPC_List.Substring(0, 2), 16) * 2 + 2;//取第一个帧的长度
                            if (fInventory_EPC_List.Length < NumLen)
                            {
                                break;
                            }
                            temp1 = fInventory_EPC_List.Substring(0, NumLen);
                            fInventory_EPC_List = fInventory_EPC_List.Substring(NumLen);
                            if (!CheckCRC(temp1)) continue;
                            AntStr = temp1.Substring(8, 2);
                            lenstr = Convert.ToString(Convert.ToInt32(temp1.Substring(10, 2), 16), 10);
                            int length = Convert.ToInt32(lenstr, 10);
                            bool m_phase = false;
                            int phase_begin = 0;
                            int phase_end = 0;
                            int freqkhz = 0;
                            if ((length & 0x40) > 0) m_phase = true;
                            EPCStr = temp1.Substring(12, (length & 0x3F) * 2);
                            RSSI = temp1.Substring(12 + (length & 0x3F) * 2, 2);
                            if (m_phase)
                            {
                                string temp_phase = temp1.Substring(temp1.Length - 18, 14);
                                phase_begin = Convert.ToInt32(temp_phase.Substring(0, 4), 16);
                                phase_end = Convert.ToInt32(temp_phase.Substring(4, 4), 16);
                                freqkhz = Convert.ToInt32(temp_phase.Substring(8, 6), 16);


                            }
                            if (callback != null)
                            {
                                RFIDTag t = new RFIDTag();
                                t.ANT = Convert.ToByte(AntStr, 16);
                                t.LEN = (byte)(EPCStr.Length / 2);
                                t.RSSI = Convert.ToByte(RSSI, 16);
                                t.UID = EPCStr;
                                t.phase_begin = phase_begin;
                                t.phase_end = phase_end;
                                t.Handles = FrmHandle;
                                t.Freqkhz = freqkhz;
                                callback(t);
                            }

                        }
                    }
                    catch (System.Exception ex)
                    {
                        ex.ToString();
                    }
                }
                else
                {
                    if (System.Environment.TickCount - startTime > 10000)
                    {
                        byte TrType = 0;
                        byte[] VersionInfo = new byte[2];
                        byte ReaderType = 0;
                        byte ScanTime = 0;
                        byte dmaxfre = 0;
                        byte dminfre = 0;
                        byte powerdBm = 0;
                        //byte FreBand = 0;
                        byte AntCfg0 = 0;
                        byte BeepEn = 0;
                        byte AntCfg1 = 0;
                        //byte OutputRep = 0;
                        byte CheckAnt = 0;
                        startTime = System.Environment.TickCount;
                        fCmdRet = RWDev.GetReaderInformation(ref ComAddr, VersionInfo, ref ReaderType, ref TrType, ref dmaxfre, ref dminfre, ref powerdBm, ref ScanTime, ref AntCfg0, ref BeepEn, ref AntCfg1, ref CheckAnt, FrmHandle);

                    }
                }
            }
            mythread = null;
        }

    }
}
