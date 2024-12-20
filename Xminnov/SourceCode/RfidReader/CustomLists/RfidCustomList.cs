using Newtonsoft.Json;
using Peakboard.ExtensionKit;
using ReaderIPCHelper.Services;
using RfidReader.RequestObjects;
using RfidReader.ResponceObject;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RfidReader.CustomLists
{
   
    public class RfidCustomList : CustomListBase
    {
        private Task _startReadTags = null;
        List<RFIDTagResponceObject> curTags = new List<RFIDTagResponceObject>();
        NamedPipeClientStream client = null;
        private bool _started = false;
        int _startOrStop = 0;
        string _healperPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "ReaderIPCHelper.exe");
        StreamReader reader = null;
        StreamWriter writer = null;
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "Rfid Readr",
                Name = "Rfid Reader",
                Description = "test",
                PropertyInputPossible = true,
                PropertyInputDefaults =
                {
                    new CustomListPropertyDefinition { Name = "Ip",Value ="192.168.20.112" },
                    new CustomListPropertyDefinition { Name = "Port",Value ="27001" }
                },
                Functions =
                {
                    new CustomListFunctionDefinition()
                    {
                        Name = "StartReadTags",
                       
                    },
                    new CustomListFunctionDefinition()
                    {
                        Name = "StopReadTags"
                    },
                     new CustomListFunctionDefinition()
                    {
                         Name = "LightAllLeds",
                         InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "ledTagType",
                                Description = "Led leight type",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.Number
                            },
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "LightAllLeds",
                                Description = "Light all leds",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.Boolean
                            },
                            
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "delayTime",
                                Description = "Text to display on line 2",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.Number
                            },new CustomListFunctionInputParameterDefinition
                            {
                                Name = "intervalTime",
                                Description = "Text to display on line 2",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.Number
                            },new CustomListFunctionInputParameterDefinition
                            {
                                Name = "Qvalue",
                                Description = "Text to display on line 2",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.Number
                            },new CustomListFunctionInputParameterDefinition
                            {
                                Name = "Session",
                                Description = "Text to display on line 2",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.Number
                            },new CustomListFunctionInputParameterDefinition
                            {
                                Name = "Target",
                                Description = "Text to display on line 2",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.Number
                            },new CustomListFunctionInputParameterDefinition
                            {
                                Name = "targettimes",
                                Description = "Text to display on line 2",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.Number
                            },new CustomListFunctionInputParameterDefinition
                            {
                                Name = "antlist",
                                Description = "Type ants so, 1,2,3,4,5,6",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.String
                            },new CustomListFunctionInputParameterDefinition
                            {
                                Name = "CloseRf",
                                Description = "Type ants so, 1,2,3,4,5,6",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.Boolean
                            }


                        }
                    },
                   
                    new CustomListFunctionDefinition()
                    {
                        Name = "LightLedswithEpc"
                    },

                }

            };
        }
        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            return new CustomListColumnCollection
            {
                new CustomListColumn("ANT",CustomListColumnTypes.Number),
                new CustomListColumn("UID",CustomListColumnTypes.String),
                new CustomListColumn("LEN",CustomListColumnTypes.Number),
                new CustomListColumn("PacketParam",CustomListColumnTypes.Number),
                new CustomListColumn("RSSI",CustomListColumnTypes.Number),
                new CustomListColumn("phase_begin",CustomListColumnTypes.Number),
                new CustomListColumn("phase_end",CustomListColumnTypes.Number),
                new CustomListColumn("Freqkhz",CustomListColumnTypes.Number),
                new CustomListColumn("Handles",CustomListColumnTypes.Number),
            };
        }
        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            return new CustomListObjectElementCollection();
        }
        protected override CustomListExecuteReturnContext ExecuteFunctionOverride(CustomListData data, CustomListExecuteParameterContext context)
        {
            try
            {
                string ipAddress = data.Properties["Ip"].ToString();
                int tcpPort = int.Parse(data.Properties["Port"]);
                string functionName = context.FunctionName;
                switch (functionName)
                {
                    case "StartReadTags":
                        if (!_started)
                        {
                            client = new NamedPipeClientStream("ReaederPipe");
                            _startReadTags = new Task(() => StartReadTags(data));
                            _startReadTags.Start();
                            _started = true;
                        }
                        Log.Info("Start Read Tags");
                        break;
                    case "StopReadTags":
                        StoptReadTags(data);
                        Log.Info("Stop Read Tags");
                        break;
                    case "LightAllLeds":
                        string ledTagType = context.Values[0].StringValue;
                        string isPhase = context.Values[1].StringValue;
                        string delayTime = context.Values[2].StringValue;
                        string intervalTime = context.Values[3].StringValue;
                        string Qvalue = context.Values[4].StringValue;
                        string session = context.Values[5].StringValue;
                        string target = context.Values[6].StringValue;
                        string targettimes = context.Values[7].StringValue;
                        string antlist = context.Values[8].StringValue;
                        string closeRf = context.Values[9].StringValue;
                        LedsRequestObject ledsRequestObject = new LedsRequestObject();
                        ledsRequestObject.Ip = ipAddress;
                        ledsRequestObject.Port = tcpPort;
                        ledsRequestObject.LedTagType = int.Parse(ledTagType);
                        ledsRequestObject.IsPhase = bool.Parse(isPhase);
                        ledsRequestObject.DelayTime = byte.Parse(delayTime);
                        ledsRequestObject.IntervalTime = byte.Parse(intervalTime);
                        ledsRequestObject.Qvalue = byte.Parse(Qvalue);
                        ledsRequestObject.Session = byte.Parse(session);
                        ledsRequestObject.Target = byte.Parse(target);
                        ledsRequestObject.TargetTimes = byte.Parse(targettimes);
                        ledsRequestObject.CloseRf = bool.Parse(closeRf);
                        bool[] antList = new bool[16];
                        string[] ants = antlist.Split(',');
                        foreach (string ant in ants)
                        {
                            int res;
                            bool parse = int.TryParse(ant,out res);
                            if (parse)
                            {
                                if (res - 1 <= 15)
                                {
                                    antList[res-1] = true;
                                }
                                else
                                {
                                    Log.Error("Max value of ant is 16");
                                }
                            }
                            else
                            {
                                Log.Error("Please weire only numbers!");
                            }

                        }
                        ledsRequestObject.AntList = antList;
                        StartLightLeds(data, ledsRequestObject);
                        break;
                    case "LightLedswithEpc":
                        Log.Info("Light Leds with Epc");
                        break;
                    default:
                        Log.Info($"function {functionName}");
                        break;
                }
                return new CustomListExecuteReturnContext
                {
                    new CustomListObjectElement
                    {
                        { "Result", "test" }
                    }
                };
            }
            catch (Exception ex)
            {

                Log.Error(ex.ToString());
                return null;
            }
           
        }
        private void StartReadTags(CustomListData data)
        {
            try
            {
                ClearTable(data);
                AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
                string ipAddress = data.Properties["Ip"].ToString();
                int tcpPort = int.Parse(data.Properties["Port"]);
               
                Log.Info(_healperPath);
                //if (File.Exists(_healperPath))
                //{
                //    Process[] processes = Process.GetProcessesByName("ReaderIPCHelper");
                //    if (processes.Length > 0)
                //    {
                //        foreach (Process process in processes)
                //        {
                //            process.Kill();
                //            process.WaitForExit();
                //        }
                //    }
                //    else if (processes.Length == 0)
                //    {
                //        ProcessStartInfo processStartInfo = new ProcessStartInfo()
                //        {
                //            FileName = _healperPath,
                //            WindowStyle = ProcessWindowStyle.Hidden
                //        };
                //        Process process = Process.Start(processStartInfo);
                //    }

                //}
                //else
                //{
                //    Log.Error("File ReaderIPCHelper.exe not exists in current Directory");
                //}
               
                client.Connect();
                
                 reader = new StreamReader(client);
                 writer = new StreamWriter(client) { AutoFlush = true };
                OpenPortRequestObject openPortRequest = new OpenPortRequestObject();
                openPortRequest.Port = tcpPort;
                openPortRequest.Ip = ipAddress;
                string jsonData = JsonConvert.SerializeObject(openPortRequest);
                writer.WriteLine($"start {jsonData}");
                Task.Run(() =>
                {
                    while (true)
                    {
                        string serverMessage = reader.ReadLine();
                        if (serverMessage != null && serverMessage.Length > 0)
                        {
                            string[] responce = serverMessage.Split(new char[] { ' ' }, 2);
                            switch (responce[0])
                            {
                                case "responceobject":
                                    var dataObject = JsonConvert.DeserializeObject<RFIDTagResponceObject>(responce[1]);
                                    var itemInList = curTags.FirstOrDefault(i => i.UID == dataObject.UID);
                                    if (itemInList != null)
                                    {
                                        bool isChanged = false;
                                        if (itemInList != dataObject)
                                        {
                                            itemInList.ANT = dataObject.ANT;
                                            itemInList.LEN = dataObject.LEN;
                                            itemInList.phase_begin = dataObject.phase_begin;
                                            itemInList.phase_end = dataObject.phase_end;
                                            itemInList.Freqkhz = dataObject.Freqkhz;
                                            itemInList.Handles = dataObject.Handles;
                                            itemInList.RSSI = dataObject.RSSI;
                                            itemInList.UID = dataObject.UID;
                                            itemInList.PacketParam = dataObject.PacketParam;
                                            isChanged = true;
                                        }
                                        if (isChanged)
                                        {
                                            var item = new CustomListObjectElement
                                                {
                                                    { "ANT", itemInList.ANT },
                                                    { "UID", itemInList.UID},
                                                    { "LEN", itemInList.LEN},
                                                    { "PacketParam", itemInList.PacketParam},
                                                    { "RSSI", itemInList.RSSI},
                                                    { "phase_begin", itemInList.phase_begin},
                                                    { "phase_end", itemInList.phase_end},
                                                    { "Freqkhz", itemInList.Freqkhz},
                                                    { "Handles", itemInList.Handles},
                                                };
                                            int index = curTags.FindIndex(i => i.UID == itemInList.UID);
                                            Data?.Push(data.ListName).Update(index, item);
                                        }
                                    }
                                    else if (itemInList == null)
                                    {
                                        curTags.Add(dataObject);
                                        Log.Info($"added 1 dataObject curr count f list = {curTags.Count}");
                                        var item = new CustomListObjectElement
                                            {
                                                { "ANT", dataObject.ANT },
                                                { "UID", dataObject.UID},
                                                { "LEN", dataObject.LEN},
                                                { "PacketParam", dataObject.PacketParam},
                                                { "RSSI", dataObject.RSSI},
                                                { "phase_begin", dataObject.phase_begin},
                                                { "phase_end", dataObject.phase_end},
                                                { "Freqkhz", dataObject.Freqkhz},
                                                { "Handles", dataObject.Handles},
                                            };
                                        Data?.Push(data.ListName).Add(item);
                                    }
                                    break;
                                case "error":
                                    Log.Error(responce[1]);
                                    StoptReadTags(data);
                                    break;
                                case "message":
                                    Log.Info(responce[1]);
                                    break;

                                default:
                                    break;
                            }
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

        private void StoptReadTags(CustomListData data)
        {
            client.Close();
            Process[] processes = Process.GetProcessesByName("ReaderIPCHelper");
            if (processes.Length > 0)
            {
                foreach (Process process in processes)
                {
                    process.Kill();
                    process.WaitForExit();
                }
            }
            _started = false;
        }

        private void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            Process[] processes = Process.GetProcessesByName("ReaderIPCHelper");
            if (processes.Length > 0)
            {
                foreach (Process process in processes)
                {
                    process.Kill();
                    process.WaitForExit();
                }
            }
        }
        private void ClearTable(CustomListData data)
        {
            for (int i = 0; i < curTags.Count; i++)
            {
                Data?.Push(data.ListName).Remove(0);
            }
            curTags.Clear();
        }
        private void StartLightLeds(CustomListData data,LedsRequestObject ledsRequestObject)
        {
            try
            {
                if (client == null)
                {
                    if (File.Exists(_healperPath))
                    {
                        Process[] processes = Process.GetProcessesByName("ReaderIPCHelper");
                        //if (processes.Length > 0)
                        //{
                        //    foreach (Process process in processes)
                        //    {
                        //        process.Kill();
                        //        process.WaitForExit();
                        //    }
                        //}
                        // if (processes.Length == 0)
                        //{
                        //    ProcessStartInfo processStartInfo = new ProcessStartInfo()
                        //    {
                        //        FileName = _healperPath,
                        //       // WindowStyle = ProcessWindowStyle.Hidden
                        //    };
                        //    Process process = Process.Start(processStartInfo);
                        //}

                    }
                    else
                    {
                        Log.Error("File ReaderIPCHelper.exe not exists in current Directory");
                    }
                    client = new NamedPipeClientStream("ReaederPipe");
                    client.Connect();
                    reader = new StreamReader(client);
                    writer = new StreamWriter(client) { AutoFlush = true };

                }
               

                string jsonData = JsonConvert.SerializeObject(ledsRequestObject);
                writer.WriteLine($"startleds {jsonData}");
                string serverMessage = reader.ReadLine();
                if (serverMessage != null && serverMessage.Length > 0)
                {
                    string[] responce = serverMessage.Split(new char[] { ' ' }, 2);
                    switch (responce[0])
                    {
                        case "error":
                            Log.Error(responce[1]);
                            StoptReadTags(data);
                            break;
                        case "message":
                            Log.Info(responce[1]);
                            break;

                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {

                Log.Error(ex.ToString());
            }
        }
    }
}
