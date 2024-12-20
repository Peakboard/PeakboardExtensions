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

namespace ReaderIPCHelper
{
    public class Program
    {

        static void Main(string[] args)
        {
            //Console.InputEncoding = System.Text.Encoding.UTF8;
            //Console.OutputEncoding = System.Text.Encoding.UTF8;
            //bool toStopThread = false;
            //Thread mythread;
            //byte ComAddr = 0x00;
            //int FrmHandle = 0;
            //LedService ledService = null;
            //List<RfidResponceObject> curList = new List<RfidResponceObject>();
            //var server = new NamedPipeServerStream("ReaederPipe", PipeDirection.InOut);
            //StreamWriter writer;
            //while (true)
            //{
            //    server.WaitForConnection();
            //    var reader = new StreamReader(server);
            //    writer = new StreamWriter(server) { AutoFlush = true };
            //    try
            //    {
            //        while (server.IsConnected)
            //        {
            //            string request = reader.ReadLine();

            //            if (request != null && request.Length > 0)
            //            {
            //                string[] data = request.Split(' ');
            //                switch (data[0])
            //                {
            //                    case "start":
            //                        var openPortRequst = JsonConvert.DeserializeObject<OpenPortRequestObject>(data[1]);
            //                        if (openPortRequst != null)
            //                        {
            //                            int fCmdRet = RWDev.OpenNetPort(openPortRequst.Port, openPortRequst.Ip, ref ComAddr, ref FrmHandle);
            //                            if (fCmdRet != 0)
            //                            {
            //                                Console.WriteLine($"connection error: error code {fCmdRet}");
            //                                writer.Write($"error connection error: error code {fCmdRet}");
            //                                return;
            //                            }
            //                            writer.WriteLine("message Device successfully connected. Initializing...");
            //                            fCmdRet = RWDev.StartInventory(ref ComAddr, 0x00, GetEPC, FrmHandle);
            //                            if (fCmdRet != 0)
            //                            {
            //                                Console.WriteLine($"Device initialization error: error code {fCmdRet}");
            //                                writer.WriteLine($"error Device initialization error: error code {fCmdRet}");
            //                                return;
            //                            }
            //                            Console.WriteLine("Device successfully initialized. Waiting for tags...");
            //                            writer.WriteLine("message Device successfully initialized. Waiting for tags...");
            //                            mythread = new Thread(workProcess);
            //                            mythread.IsBackground = true;
            //                            mythread.Start();
            //                            Console.WriteLine("Press any key to terminate...");
            //                            Console.ReadKey();
            //                            toStopThread = true;
            //                            mythread.Join();
            //                            Console.WriteLine("Operation completed.");
            //                        }
            //                        break;
            //                     case "startleds":
            //                        if (ledService == null)
            //                        {
            //                          // RWDev.CloseNetPort(1);

            //                            ledService = new LedService("192.168.20.112", 27001,FrmHandle);
            //                        }


            //                        var ledsRequestObject = JsonConvert.DeserializeObject<LedsRequestObject>(data[1]);
            //                        Thread.Sleep(1000);
            //                        int res = ledService.Start(ledsRequestObject.CloseRf, ledsRequestObject.DelayTime, ledsRequestObject.Qvalue, ledsRequestObject.Session, ledsRequestObject.Target, ledsRequestObject.TargetTimes, ledsRequestObject.IntervalTime, ledsRequestObject.AntList, new List<string>(), ledsRequestObject.IsPhase, ledsRequestObject.LedTagType);
            //                        writer.WriteLine($"message {res}");
            //                        break;
            //                }
            //            }
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        Console.WriteLine(ex.Message);
            //    }
            //}

            //void GetEPC(RFIDTag mtag)
            //{
            //    RfidResponceObject rfidResponceObject = new RfidResponceObject();
            //    rfidResponceObject.ANT = mtag.ANT;
            //    rfidResponceObject.LEN = mtag.LEN;
            //    rfidResponceObject.phase_begin = mtag.phase_begin;
            //    rfidResponceObject.phase_end = mtag.phase_end;
            //    rfidResponceObject.Freqkhz = mtag.Freqkhz;
            //    rfidResponceObject.Handles = mtag.Handles;
            //    rfidResponceObject.RSSI = mtag.RSSI;
            //    rfidResponceObject.UID = mtag.UID;
            //    rfidResponceObject.PacketParam = mtag.PacketParam;
            //    lock (curList)
            //    {
            //        var tag = curList.FirstOrDefault(t => t.UID == mtag.UID);
            //        if (tag != null)
            //        {
            //            bool isChanged = false;

            //            if (tag != rfidResponceObject)
            //            {
            //                tag.ANT = rfidResponceObject.ANT;
            //                tag.LEN = mtag.LEN;
            //                tag.phase_begin = rfidResponceObject.phase_begin;
            //                tag.phase_end = rfidResponceObject.phase_end;
            //                tag.Freqkhz = rfidResponceObject.Freqkhz;
            //                tag.Handles = rfidResponceObject.Handles;
            //                tag.RSSI = rfidResponceObject.RSSI;
            //                tag.UID = rfidResponceObject.UID;
            //                tag.PacketParam = rfidResponceObject.PacketParam;
            //                isChanged = true;
            //            }
            //            if (isChanged)
            //            {
            //                tag = rfidResponceObject;
            //            }
            //        }
            //        else if (tag == null)
            //        {
            //            curList.Add(rfidResponceObject);
            //        }
            //        string jsonObject = JsonConvert.SerializeObject(rfidResponceObject);
            //        writer.WriteLine("responceobject " + jsonObject);
            //    }
            //}

            //void workProcess()
            //{
            //    string fInventory_EPC_List = "";
            //    long startTime = System.Environment.TickCount;
            //    while (!toStopThread)
            //    {
            //        byte[] rfidData = new byte[4096];
            //        int ValidDatalength = 0;
            //        int fCmdRet = RWDev.GetRfidTagData(ref ComAddr, rfidData, ref ValidDatalength, FrmHandle);
            //        if (fCmdRet == 0)
            //        {
            //            byte[] daw = new byte[ValidDatalength];
            //            Array.Copy(rfidData, 0, daw, 0, ValidDatalength);
            //            string temp = ByteArrayToHexString(daw);
            //            fInventory_EPC_List += temp;

            //            while (fInventory_EPC_List.Length > 18)
            //            {
            //                string FlagStr = "EE00";
            //                int nindex = fInventory_EPC_List.IndexOf(FlagStr);
            //                if (nindex > 3)
            //                    fInventory_EPC_List = fInventory_EPC_List.Substring(nindex - 4);
            //                else
            //                {
            //                    fInventory_EPC_List = fInventory_EPC_List.Substring(2);
            //                    continue;
            //                }

            //                int NumLen = Convert.ToInt32(fInventory_EPC_List.Substring(0, 2), 16) * 2 + 2;
            //                if (fInventory_EPC_List.Length < NumLen) break;

            //                string temp1 = fInventory_EPC_List.Substring(0, NumLen);
            //                fInventory_EPC_List = fInventory_EPC_List.Substring(NumLen);
            //                if (!CheckCRC(temp1)) continue;

            //                string EPCStr = temp1.Substring(12, Convert.ToInt32(temp1.Substring(10, 2), 16) * 2);
            //                string RSSI = temp1.Substring(12 + EPCStr.Length, 2);
            //                string AntStr = temp1.Substring(8, 2);

            //                RFIDTag tag = new RFIDTag
            //                {
            //                    UID = EPCStr,
            //                    RSSI = Convert.ToByte(RSSI, 16),
            //                    ANT = Convert.ToByte(AntStr, 16),
            //                    Handles = FrmHandle
            //                };

            //                GetEPC(tag);
            //            }
            //        }
            //    }
            //}
            //string ByteArrayToHexString(byte[] data)
            //{
            //    StringBuilder sb = new StringBuilder(data.Length * 3);
            //    foreach (byte b in data)
            //        sb.Append(Convert.ToString(b, 16).PadLeft(2, '0'));
            //    return sb.ToString().ToUpper();
            //}
            //bool CheckCRC(string s)
            //{
            //    int i, j;
            //    int current_crc_value;
            //    byte crcL, crcH;
            //    byte[] data = HexStringToByteArray(s);
            //    current_crc_value = 0xFFFF;
            //    for (i = 0; i <= (data.Length - 1); i++)
            //    {
            //        current_crc_value = current_crc_value ^ (data[i]);
            //        for (j = 0; j < 8; j++)
            //        {
            //            if ((current_crc_value & 0x01) != 0)
            //                current_crc_value = (current_crc_value >> 1) ^ 0x8408;
            //            else
            //                current_crc_value = (current_crc_value >> 1);
            //        }
            //    }
            //    crcL = Convert.ToByte(current_crc_value & 0xFF);
            //    crcH = Convert.ToByte((current_crc_value >> 8) & 0xFF);
            //    return crcH == 0 && crcL == 0;
            //}
            //byte[] HexStringToByteArray(string s)
            //{
            //    if (string.IsNullOrEmpty(s)) return null;
            //    s = s.Replace(" ", "");
            //    byte[] buffer = new byte[s.Length / 2];
            //    for (int i = 0; i < s.Length; i += 2)
            //        buffer[i / 2] = (byte)Convert.ToByte(s.Substring(i, 2), 16);
            //    return buffer;
            //}



            bool[] ledAnts = new bool[16];
            ledAnts[0] = true;


            LedService ledService = new LedService("192.168.20.112", 27001, 0);
            while (true)
            {
                Console.ReadLine();
               
                 int res = ledService.Start(false, 20, 6, 2, 0, Convert.ToByte("1", 10), 0, ledAnts, new List<string>(), true, 2);
                  Thread.Sleep(2000);
                 Console.WriteLine(res);
            }
            

            // Console.ReadKey();

        }
    }
}




