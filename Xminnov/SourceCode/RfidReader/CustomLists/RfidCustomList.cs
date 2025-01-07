using Newtonsoft.Json;
using Peakboard.ExtensionKit;
using RfidReader.RequestObjects;
using RfidReader.ResponceObject;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Threading.Tasks;

namespace RfidReader.CustomLists
{
    public class RfidCustomList : CustomListBase
    {
        private Task _startReadTags = null;
        List<RFIDTagResponceObject> curTags = new List<RFIDTagResponceObject>();
        NamedPipeClientStream _tagClient = null;
        NamedPipeClientStream _ledClient = null;
        private bool isTagReadStarted = false;
        string _healperPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "ReaderIPCHelper.exe");
        StreamReader _tagReader = null;
        StreamWriter _tagWriter = null;
        StreamReader _ledReader = null;
        StreamWriter _ledWriter = null;
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
                SupportsPushOnly = true,
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
                        Name = "LightLedswithEpc",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "EpcIds",
                                Description = "Type EPC so, 1,2,3,4,5,6",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.String
                            },
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "ledTagType",
                                Description = "Led leight type",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.Number
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
                        if (!isTagReadStarted)
                        {
                            _startReadTags = Task.Run(() => StartReadTags(data));
                            isTagReadStarted = true;
                        }
                        else
                        {
                            Log.Error("Stop Tag reading first!");
                        }
                            break;
                    case "StopReadTags":
                        isTagReadStarted = false;
                        StopReadTags(data);
                        Log.Info("Stop Read Tags");
                        break;
                    case "LightAllLeds":
                        string ledTagType = context.Values[0].StringValue;
                        string delayTime = context.Values[1].StringValue;
                        string intervalTime = context.Values[2].StringValue;
                        string Qvalue = context.Values[3].StringValue;
                        string session = context.Values[4].StringValue;
                        string target = context.Values[5].StringValue;
                        string targettimes = context.Values[6].StringValue;
                        string antlist = context.Values[7].StringValue;
                        string closeRf = context.Values[8].StringValue;
                        LedsRequestObject ledsRequestObject = new LedsRequestObject();
                        ledsRequestObject.Ip = ipAddress;
                        ledsRequestObject.Port = tcpPort;
                        ledsRequestObject.LedTagType = int.Parse(ledTagType);
                        ledsRequestObject.IsPhase = true;
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
                            bool parse = int.TryParse(ant, out res);
                            if (parse)
                            {
                                if (res - 1 <= 15)
                                {
                                    antList[res - 1] = true;
                                }
                                else
                                {
                                    Log.Error("Max value of ant is 16");
                                }
                            }
                            else
                            {
                                Log.Error("Please write only numbers in AntList!");
                            }
                        }
                        ledsRequestObject.AntList = antList;
                        StartLightLeds(data, ledsRequestObject);
                        break;
                    case "LightLedswithEpc":
                        Log.Info("Light Leds with Epc");
                        string antss = context.Values[0].StringValue;
                        string ledTagType1 = context.Values[1].StringValue;
                        string delayTime1 = context.Values[2].StringValue;
                        string intervalTime1 = context.Values[3].StringValue;
                        string Qvalue1 = context.Values[4].StringValue;
                        string session1 = context.Values[5].StringValue;
                        string target1 = context.Values[6].StringValue;
                        string targettimes1 = context.Values[7].StringValue;
                        string antlist1 = context.Values[8].StringValue;
                        string closeRf1 = context.Values[9].StringValue;
                        LedsRequestObject ledsRequestObject1 = new LedsRequestObject();
                        ledsRequestObject1.Ip = ipAddress;
                        ledsRequestObject1.Port = tcpPort;
                        ledsRequestObject1.LedTagType = int.Parse(ledTagType1);
                        ledsRequestObject1.IsPhase = false;
                        ledsRequestObject1.DelayTime = byte.Parse(delayTime1);
                        ledsRequestObject1.IntervalTime = byte.Parse(intervalTime1);
                        ledsRequestObject1.Qvalue = byte.Parse(Qvalue1);
                        ledsRequestObject1.Session = byte.Parse(session1);
                        ledsRequestObject1.Target = byte.Parse(target1);
                        ledsRequestObject1.TargetTimes = byte.Parse(targettimes1);
                        ledsRequestObject1.CloseRf = bool.Parse(closeRf1);
                        ledsRequestObject1.Epcs = antss.Split(',').ToList();
                        bool[] antList1 = new bool[16];
                        string[] ants1 = antlist1.Split(',');
                        foreach (string ant in ants1)
                        {
                            int res;
                            bool parse = int.TryParse(ant, out res);
                            if (parse)
                            {
                                if (res - 1 <= 15)
                                {
                                    antList1[res - 1] = true;
                                }
                                else
                                {
                                    Log.Error("Max value of ant is 16");
                                }
                            }
                            else
                            {
                                Log.Error("Please write only numbers in AntList!");
                            }
                        }
                        ledsRequestObject1.AntList = antList1;
                        StartLightLeds(data, ledsRequestObject1);
                        Log.Info($"{antss}");
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
                if (File.Exists(_healperPath))
                {
                    Process[] processes = Process.GetProcessesByName("ReaderIPCHelper");
                   if (processes.Length == 0)
                    {
                        ProcessStartInfo processStartInfo = new ProcessStartInfo()
                        {
                            FileName = _healperPath,
                            WindowStyle = ProcessWindowStyle.Hidden
                        };
                        Process process = Process.Start(processStartInfo);
                    }

                }
                else
                {
                    Log.Error("File ReaderIPCHelper.exe not exists in current Directory");
                    throw new FileNotFoundException("File ReaderIPCHelper.exe not exists in current Directory");
                }
                ClearTable(data);
                string ipAddress = data.Properties["Ip"].ToString();
                int tcpPort = int.Parse(data.Properties["Port"]);
                _tagClient = new NamedPipeClientStream("TagPipe");
                _tagClient.Connect();
                _tagReader = new StreamReader(_tagClient);
                _tagWriter = new StreamWriter(_tagClient) { AutoFlush = true };
                OpenPortRequestObject openPortRequest = new OpenPortRequestObject
                {
                    Port = tcpPort,
                    Ip = ipAddress
                };
                string jsonData = JsonConvert.SerializeObject(openPortRequest);
                _tagWriter.WriteLine($"start {jsonData}");
                while (isTagReadStarted)
                {
                    string serverMessage = _tagReader.ReadLine();
                    if (serverMessage != null && serverMessage.Length > 0)
                    {
                        string[] response = serverMessage.Split(new char[] { ' ' }, 2);
                        switch (response[0])
                        {
                            case "responceobject":
                                var dataObject = JsonConvert.DeserializeObject<RFIDTagResponceObject>(response[1]);
                                var itemInList = curTags.FirstOrDefault(i => i.UID == dataObject.UID);
                                if (itemInList != null)
                                {
                                    bool isChanged = false;
                                    if (itemInList != dataObject)
                                    {
                                        itemInList.ANT = dataObject.ANT;
                                        itemInList.LEN = dataObject.LEN;
                                        itemInList.PhaseBegin = dataObject.PhaseBegin;
                                        itemInList.PhaseEnd = dataObject.PhaseEnd;
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
                                            { "phase_begin", itemInList.PhaseBegin},
                                            { "phase_end", itemInList.PhaseEnd},
                                            { "Freqkhz", itemInList.Freqkhz},
                                            { "Handles", itemInList.Handles},
                                        };
                                        int index = curTags.FindIndex(i => i.UID == itemInList.UID);
                                        Data?.Push(data.ListName).Update(index, item);
                                    }
                                }
                                else
                                {
                                    curTags.Add(dataObject);
                                    Log.Info($"Added 1 dataObject. Current list count: {curTags.Count}");

                                    var item = new CustomListObjectElement
                                    {
                                        { "ANT", dataObject.ANT },
                                        { "UID", dataObject.UID},
                                        { "LEN", dataObject.LEN},
                                        { "PacketParam", dataObject.PacketParam},
                                        { "RSSI", dataObject.RSSI},
                                        { "phase_begin", dataObject.PhaseBegin},
                                        { "phase_end", dataObject.PhaseEnd},
                                        { "Freqkhz", dataObject.Freqkhz},
                                        { "Handles", dataObject.Handles},
                                    };
                                    Data?.Push(data.ListName).Add(item);
                                }
                                break;
                            case "error":
                                Log.Error(response[1]);
                                StopReadTags(data);
                                break;
                            case "message":
                                Log.Info(response[1]);
                                break;

                            default:
                                Log.Info(serverMessage);
                                break;
                        }
                    }
                }
                Log.Info("StartReadTags task completed.");
            }
            catch (Exception ex)
            {
                Log.Error($"Error in StartReadTags: {ex.Message}");
            }
        }
        private void StopReadTags(CustomListData data)
        {
            try
            {
                _tagClient = null;
                _tagReader = null;
                _tagWriter = null;
                if (File.Exists(_healperPath))
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
                else
                {
                    Log.Error("File ReaderIPCHelper.exe not exists in current Directory");
                    throw new FileNotFoundException("File ReaderIPCHelper.exe not exists in current Directory");
                }
            }
            catch (Exception ex)
            {
                Log.Info(ex.Message);
            }
        }
        protected override void SetupOverride(CustomListData data)
        {
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
        }
        private void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
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
        private void StartLightLeds(CustomListData data, LedsRequestObject ledsRequestObject)
        {
            try
            {
                if (File.Exists(_healperPath))
                {
                    Process[] processes = Process.GetProcessesByName("ReaderIPCHelper");
                    if (processes.Length == 0)
                    {
                        ProcessStartInfo processStartInfo = new ProcessStartInfo()
                        {
                            FileName = _healperPath,
                            WindowStyle = ProcessWindowStyle.Hidden
                        };
                        Process process = Process.Start(processStartInfo);
                    }

                }
                else
                {
                    Log.Error("File ReaderIPCHelper.exe not exists in current Directory");
                    throw new FileNotFoundException("File ReaderIPCHelper.exe not exists in current Directory");
                }
                _ledClient = new NamedPipeClientStream("LedPipe");
                _ledClient.Connect();
                _ledReader = new StreamReader(_ledClient);
                _ledWriter = new StreamWriter(_ledClient) { AutoFlush = true };
                string jsonData = JsonConvert.SerializeObject(ledsRequestObject);
                _ledWriter.WriteLine($"startleds {jsonData}");
                Log.Info("request sendet");
                string serverMessage = _ledReader.ReadLine();
                if (serverMessage != null && serverMessage.Length > 0)
                {
                    string[] responce = serverMessage.Split(new char[] { ' ' }, 2);
                    switch (responce[0])
                    {
                        case "error":
                            Log.Error(responce[1]);
                            break;
                        case "message":
                            Log.Info(responce[1]);
                            break;
                        default:
                            break;
                    }
                }
                _ledClient = null;
                _ledReader.Dispose();
                _ledWriter.Dispose();
                _ledClient.Dispose();
            }
            catch (ObjectDisposedException) { }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
    }
}
