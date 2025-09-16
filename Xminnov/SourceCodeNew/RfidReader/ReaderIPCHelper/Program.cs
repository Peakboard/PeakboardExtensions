using Newtonsoft.Json;
using ReaderIPCHelper.RequestObjects;
using ReaderIPCHelper.Models;
using ReaderIPCHelper.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using RFIDTag = ReaderIPCHelper.Services.RFIDTag;
using System.Threading.Tasks;

namespace ReaderIPCHelper
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.InputEncoding = System.Text.Encoding.UTF8;
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            bool toStopThread = false;
            Thread mythread = null;
            byte ComAddr = 0x00;
            int FrmHandle = 555;
            LedService ledService = null;
            List<RfidResponceObject> curList = new List<RfidResponceObject>();
            NamedPipeServerStream tagPipeServer;
            NamedPipeServerStream ledPipeServer;
            int fCmdRet;
            StreamWriter tagWriter = null;
            StreamWriter ledWriter = null;
            StreamReader ledReader = null;
            StreamReader tagReader = null;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            Task.Run(() =>
            {
                while (true)
                {
                    tagPipeServer = new NamedPipeServerStream("TagPipe", PipeDirection.InOut);
                    tagPipeServer.WaitForConnection();
                    tagReader = new StreamReader(tagPipeServer);
                    tagWriter = new StreamWriter(tagPipeServer) { AutoFlush = true };
                    toStopThread = false;
                    try
                    {
                        while (!tagReader.EndOfStream)
                        {
                            string request = tagReader.ReadLine();
                            if (request != null && request.Length > 0)
                            {
                                string[] data = request.Split(' ');
                                switch (data[0])
                                {
                                    case "start":
                                        var openPortRequst = JsonConvert.DeserializeObject<OpenPortRequestObject>(data[1]);
                                        if (openPortRequst != null)
                                        {
                                            fCmdRet = RWDev.OpenNetPort(openPortRequst.Port, openPortRequst.Ip, ref ComAddr, ref FrmHandle);
                                            if (fCmdRet != 0)
                                            {
                                                tagWriter.Write($"error connection error: error code {fCmdRet}");
                                                return;
                                            }
                                            tagWriter.WriteLine("message Device successfully connected. Initializing...");
                                            fCmdRet = RWDev.StartInventory(ref ComAddr, 0x00, GetEPC, FrmHandle);
                                            if (fCmdRet != 0)
                                            {
                                                tagWriter.WriteLine($"error Device initialization error: error code {fCmdRet}");
                                                return;
                                            }
                                            tagWriter.WriteLine("message Device successfully initialized. Waiting for tags...");
                                            mythread = new Thread(workProcess);
                                            mythread.IsBackground = true;
                                            mythread.Start();
                                            Console.ReadKey();
                                        }
                                        break;
                                    case "stop":
                                        if (mythread != null)
                                        {
                                            toStopThread = true;
                                            mythread.Join();
                                            fCmdRet = RWDev.StopInventory(ref ComAddr, FrmHandle);
                                            fCmdRet = RWDev.CloseNetPort(FrmHandle);
                                        }
                                        tagReader.Dispose();
                                        tagWriter.Dispose();
                                        tagPipeServer.Disconnect();
                                        break;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (!toStopThread)
                        {
                            toStopThread = true;
                            mythread.Join();
                            Console.WriteLine(ex);
                            fCmdRet = RWDev.StopInventory(ref ComAddr, FrmHandle);
                            fCmdRet = RWDev.CloseNetPort(FrmHandle);
                            Console.WriteLine("Stopped");
                            Cleanup(ref tagReader, ref tagWriter, ref tagPipeServer);
                        }
                    }
                }
            });

            Task.Run(() =>
            {
                while (true)
                {
                    ledPipeServer = new NamedPipeServerStream("LedPipe", PipeDirection.InOut, NamedPipeServerStream.MaxAllowedServerInstances, PipeTransmissionMode.Message, PipeOptions.Asynchronous);
                    ledPipeServer.WaitForConnection();
                    ledReader = new StreamReader(ledPipeServer);
                    ledWriter = new StreamWriter(ledPipeServer) { AutoFlush = true };
                    try
                    {
                        while (!ledReader.EndOfStream)
                        {
                            string request = ledReader.ReadLine();

                            if (request != null && request.Length > 0)
                            {
                                string[] data = request.Split(' ');
                                switch (data[0])
                                {
                                    case "startleds":

                                        // RWDev.CloseNetPort(1);
                                        if (mythread != null)
                                        {
                                            toStopThread = true;
                                            mythread.Join();
                                            fCmdRet = RWDev.StopInventory(ref ComAddr, FrmHandle);
                                            fCmdRet = RWDev.CloseNetPort(FrmHandle);
                                            ledService = new LedService("192.168.20.112", 27001, FrmHandle);
                                            var ledsRequestObject = JsonConvert.DeserializeObject<LedsRequestObject>(data[1]);
                                            Thread.Sleep(700);
                                            int res = ledService.Start(ledsRequestObject.CloseRf, ledsRequestObject.DelayTime, ledsRequestObject.Qvalue, ledsRequestObject.Session, ledsRequestObject.Target, ledsRequestObject.TargetTimes, ledsRequestObject.IntervalTime, ledsRequestObject.AntList, new List<string>(), ledsRequestObject.IsPhase, ledsRequestObject.LedTagType, ref FrmHandle, ledsRequestObject.Epcs);
                                            ledWriter.WriteLine($"message {res}");
                                            toStopThread = false;
                                            fCmdRet = RWDev.CloseNetPort(FrmHandle);
                                            fCmdRet = RWDev.StopInventory(ref ComAddr, FrmHandle);
                                            fCmdRet = RWDev.OpenNetPort(ledsRequestObject.Port, ledsRequestObject.Ip, ref ComAddr, ref FrmHandle);
                                            if (fCmdRet != 0)
                                            {
                                                tagWriter.Write($"error connection error: error code {fCmdRet}");
                                            }
                                            tagWriter.WriteLine("message Device successfully connected. Initializing...");
                                            fCmdRet = RWDev.StartInventory(ref ComAddr, 0x00, GetEPC, FrmHandle);
                                            mythread = new Thread(workProcess)
                                            {
                                                IsBackground = true
                                            };
                                            mythread.Start();
                                        }
                                        else
                                        {
                                            var ledsRequestObject = JsonConvert.DeserializeObject<LedsRequestObject>(data[1]);
                                            fCmdRet = RWDev.OpenNetPort(ledsRequestObject.Port, ledsRequestObject.Ip, ref ComAddr, ref FrmHandle);
                                            fCmdRet = RWDev.StopInventory(ref ComAddr, FrmHandle);
                                            fCmdRet = RWDev.CloseNetPort(FrmHandle);
                                            ledService = new LedService("192.168.20.112", 27001, FrmHandle);
                                            Thread.Sleep(700);
                                            int res = ledService.Start(ledsRequestObject.CloseRf, ledsRequestObject.DelayTime, ledsRequestObject.Qvalue, ledsRequestObject.Session, ledsRequestObject.Target, ledsRequestObject.TargetTimes, ledsRequestObject.IntervalTime, ledsRequestObject.AntList, new List<string>(), ledsRequestObject.IsPhase, ledsRequestObject.LedTagType, ref FrmHandle, ledsRequestObject.Epcs);
                                            ledWriter.WriteLine($"message {res}");
                                        }
                                        break;
                                }
                            }

                        }
                    }
                    catch (Exception ex)
                    {
                        fCmdRet = RWDev.StopInventory(ref ComAddr, FrmHandle);
                        fCmdRet = RWDev.CloseNetPort(FrmHandle);
                        toStopThread = true;
                        Cleanup(ref ledReader, ref ledWriter, ref ledPipeServer);
                    }
                }
            });
            Console.ReadLine();
            void Cleanup(ref StreamReader reader, ref StreamWriter writer, ref NamedPipeServerStream server)
            {
                try
                {
                    reader.Dispose();
                    writer.Dispose();
                    server.Disconnect();
                    reader = null;
                    writer = null;
                    server = null;
                }
                catch (Exception ex)
                {
                }

            }
            void GetEPC(RFIDTag mtag)
            {
                try
                {
                    RfidResponceObject rfidResponceObject = new RfidResponceObject();
                    rfidResponceObject.ANT = mtag.ANT;
                    rfidResponceObject.LEN = mtag.LEN;
                    rfidResponceObject.PhaseBegin = mtag.phase_begin;
                    rfidResponceObject.PhaseEnd = mtag.phase_end;
                    rfidResponceObject.Freqkhz = mtag.Freqkhz;
                    rfidResponceObject.Handles = mtag.Handles;
                    rfidResponceObject.RSSI = mtag.RSSI;
                    rfidResponceObject.UID = mtag.UID;
                    rfidResponceObject.PacketParam = mtag.PacketParam;
                    lock (curList)
                    {
                        var tag = curList.FirstOrDefault(t => t.UID == mtag.UID);
                        if (tag != null)
                        {
                            bool isChanged = false;

                            if (tag != rfidResponceObject)
                            {
                                tag.ANT = rfidResponceObject.ANT;
                                tag.LEN = mtag.LEN;
                                tag.PhaseBegin = rfidResponceObject.PhaseBegin;
                                tag.PhaseEnd = rfidResponceObject.PhaseEnd;
                                tag.Freqkhz = rfidResponceObject.Freqkhz;
                                tag.Handles = rfidResponceObject.Handles;
                                tag.RSSI = rfidResponceObject.RSSI;
                                tag.UID = rfidResponceObject.UID;
                                tag.PacketParam = rfidResponceObject.PacketParam;
                                isChanged = true;
                            }
                            if (isChanged)
                            {
                                tag = rfidResponceObject;
                            }
                        }
                        else if (tag == null)
                        {
                            curList.Add(rfidResponceObject);
                        }
                        string jsonObject = JsonConvert.SerializeObject(rfidResponceObject);
                        tagWriter.WriteLine("responceobject " + jsonObject);
                    }
                }
                catch (Exception ex)
                {
                    if (!toStopThread)
                    {
                        toStopThread = true;
                        mythread.Join();
                        fCmdRet = RWDev.StopInventory(ref ComAddr, FrmHandle);
                        fCmdRet = RWDev.CloseNetPort(FrmHandle);
                    }
                }
            }
            void workProcess()
            {
                string fInventory_EPC_List = "";
                long startTime = System.Environment.TickCount;
                while (!toStopThread)
                {
                    byte[] rfidData = new byte[4096];
                    int ValidDatalength = 0;
                    fCmdRet = RWDev.GetRfidTagData(ref ComAddr, rfidData, ref ValidDatalength, FrmHandle);
                    if (fCmdRet == 0)
                    {
                        byte[] daw = new byte[ValidDatalength];
                        Array.Copy(rfidData, 0, daw, 0, ValidDatalength);
                        string temp = ByteArrayToHexString(daw);
                        fInventory_EPC_List += temp;

                        while (fInventory_EPC_List.Length > 18)
                        {
                            string FlagStr = "EE00";
                            int nindex = fInventory_EPC_List.IndexOf(FlagStr);
                            if (nindex > 3)
                                fInventory_EPC_List = fInventory_EPC_List.Substring(nindex - 4);
                            else
                            {
                                fInventory_EPC_List = fInventory_EPC_List.Substring(2);
                                continue;
                            }
                            int NumLen = Convert.ToInt32(fInventory_EPC_List.Substring(0, 2), 16) * 2 + 2;
                            if (fInventory_EPC_List.Length < NumLen) break;
                            string temp1 = fInventory_EPC_List.Substring(0, NumLen);
                            fInventory_EPC_List = fInventory_EPC_List.Substring(NumLen);
                            if (!CheckCRC(temp1)) continue;
                            string EPCStr = temp1.Substring(12, Convert.ToInt32(temp1.Substring(10, 2), 16) * 2);
                            string RSSI = temp1.Substring(12 + EPCStr.Length, 2);
                            string AntStr = temp1.Substring(8, 2);
                            RFIDTag tag = new RFIDTag
                            {
                                UID = EPCStr,
                                RSSI = Convert.ToByte(RSSI, 16),
                                ANT = Convert.ToByte(AntStr, 16),
                                Handles = FrmHandle
                            };
                            GetEPC(tag);
                        }
                    }
                }
            }
            string ByteArrayToHexString(byte[] data)
            {
                StringBuilder sb = new StringBuilder(data.Length * 3);
                foreach (byte b in data)
                    sb.Append(Convert.ToString(b, 16).PadLeft(2, '0'));
                return sb.ToString().ToUpper();
            }
            bool CheckCRC(string s)
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
                return crcH == 0 && crcL == 0;
            }
            byte[] HexStringToByteArray(string s)
            {
                if (string.IsNullOrEmpty(s)) return null;
                s = s.Replace(" ", "");
                byte[] buffer = new byte[s.Length / 2];
                for (int i = 0; i < s.Length; i += 2)
                    buffer[i / 2] = (byte)Convert.ToByte(s.Substring(i, 2), 16);
                return buffer;
            }
            void CurrentDomain_ProcessExit(object sender, EventArgs e)
            {
                fCmdRet = RWDev.StopInventory(ref ComAddr, FrmHandle);
                fCmdRet = RWDev.CloseNetPort(FrmHandle);
            }
        }
    }
}


