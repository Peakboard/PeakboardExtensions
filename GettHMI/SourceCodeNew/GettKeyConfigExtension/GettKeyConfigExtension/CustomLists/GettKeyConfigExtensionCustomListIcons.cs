using GETT_DisplayDeviceLib;
using GETT_DisplayDeviceLib.Parameter;
using Peakboard.ExtensionKit;
using System.Drawing;
using System.Reflection;

namespace GettKeyConfigExtension.CustomLists
{
    [CustomListIcon("GettKeyConfigExtension.pb_datasource_gett.png")]
    class GettKeyConfigExtensionCustomListIcons : CustomListBase
    {
        private static readonly SemaphoreSlim _deviceSemaphore = new SemaphoreSlim(1, 1);

        private const int TransferDelayMs = 150;

        private readonly Dictionary<string, DeviceState> _states = new();

        private DeviceState GetState(string listName)
        {
            if (!_states.TryGetValue(listName, out var s))
            {
                s = new DeviceState();
                _states[listName] = s;
            }
            return s;
        }

        private class DeviceState
        {
            public GETT_DisplayDevice? Device { get; set; }
            public byte LastButtonState { get; set; } = 0;
        }

        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "GettKeyConfigExtensionIconsList",
                Name = "Gett HMI Display Keys",
                Description = "Steuert die neue Tastenleiste mit Display/Icons",
                PropertyInputPossible = true,
                SupportsPushOnly = true,
                Functions = new CustomListFunctionDefinitionCollection
                {
                    new CustomListFunctionDefinition
                    {
                        Name = "SetKeyImageOff",
                        Description = "Setzt das Bild für den inaktiven Zustand (OFF) einer Taste. Blockiert bis der Transfer abgeschlossen ist.",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "KeyNumber",
                                Description = "Tastennummer (1-6)",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.Number
                            },
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "Base64Image",
                                Description = "Bild als Base64 String",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.String
                            }
                        }
                    },
                    new CustomListFunctionDefinition
                    {
                        Name = "SetKeyImageOn",
                        Description = "Setzt das Bild für den aktiven Zustand (ON) einer Taste. Blockiert bis der Transfer abgeschlossen ist.",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                        {
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "KeyNumber",
                                Description = "Tastennummer (1-6)",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.Number
                            },
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "Base64Image",
                                Description = "Bild als Base64 String",
                                Optional = false,
                                Type = CustomListFunctionParameterTypes.String
                            }
                        }
                    },
                    new CustomListFunctionDefinition
                    {
                        Name = "IsDevicePresent",
                        Description = "Prueft, ob die Display-Leiste (PID 9040) am USB vorhanden ist. Gibt true/false zurueck.",
                        InputParameters = new CustomListFunctionInputParameterDefinitionCollection { },
                        ReturnParameters = new CustomListFunctionReturnParameterDefinitionCollection
                        {
                            new CustomListFunctionReturnParameterDefinition
                            {
                                Name = "Present",
                                Type = CustomListFunctionParameterTypes.Boolean
                            }
                        }
                    }
                }
            };
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            return new CustomListColumnCollection
            {
                new CustomListColumn("Timestamp", CustomListColumnTypes.Number),
                new CustomListColumn("KeyNumber", CustomListColumnTypes.Number)
            };
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            return new CustomListObjectElementCollection
            {
                new CustomListObjectElement
                {
                    ["Timestamp"] = (double)DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    ["KeyNumber"] = 0.0
                }
            };
        }

        protected override void SetupOverride(CustomListData data)
        {
            if (!IsDeviceOnUsb())
            {
                Log?.Info("GETT HMI: Display-Leiste (PID 9040) nicht am USB - Setup uebersprungen.");
                return;
            }

            var state = GetState(data.ListName);
            state.Device = GETT_DisplayDevice.Instance;
            state.Device.OnButtonStateChanged += (sender, buttonState)
                => DeviceHandle_OnButtonStateChanged(data.ListName, buttonState);
            state.Device.OnConnected += (sender)
                => DeviceHandle_OnConnected(data.ListName);

            if (state.Device.IsConnected)
            {
                state.Device.SetKeyboardOutputStatus(true);
                Log?.Info("GETT HMI: Setup abgeschlossen – HID unterdrückt.");
            }
        }

        protected override void CleanupOverride(CustomListData data)
        {
            if (_states.TryGetValue(data.ListName, out var state) && state.Device != null)
            {
                if (state.Device.IsConnected)
                    state.Device.SetKeyboardOutputStatus(false);
                _states.Remove(data.ListName);
            }
        }

        private void DeviceHandle_OnConnected(string listName)
        {
            Log?.Info("GETT HMI: OnConnected Event empfangen.");
            var state = GetState(listName);
            state.Device?.SetKeyboardOutputStatus(true);
        }

        private void DeviceHandle_OnButtonStateChanged(string listName, byte newState)
        {
            Task.Run(() =>
            {
                if (Data == null) return;

                var state = GetState(listName);

                byte pressedKeys = (byte)((state.LastButtonState ^ newState) & newState);
                state.LastButtonState = newState;

                if (pressedKeys == 0) return;

                int keyNumber = 0;
                for (int i = 0; i < 6; i++)
                {
                    if ((pressedKeys & (1 << i)) != 0)
                    {
                        keyNumber = i + 1;
                        break;
                    }
                }

                if (keyNumber == 0) return;

                Log?.Info($"GETT HMI: Taste {keyNumber} gedrückt.");

                long unixTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var element = new CustomListObjectElement
                {
                    ["Timestamp"] = (double)unixTime,
                    ["KeyNumber"] = (double)keyNumber
                };

                Data.Push(listName).Update(0, element);
            });
        }

        protected override CustomListExecuteReturnContext ExecuteFunctionOverride(
            CustomListData data, CustomListExecuteParameterContext context)
        {
            if (context.TryExecute("SetKeyImageOff", data, SetKeyImageOffFunc, out var r1)) return r1;
            if (context.TryExecute("SetKeyImageOn", data, SetKeyImageOnFunc, out var r2)) return r2;
            if (context.TryExecute("IsDevicePresent", data, IsDevicePresentFunc, out var r3)) return r3;
            return new CustomListExecuteReturnContext();
        }

        private const ushort DISPLAY_VID = 8741;
        private const ushort DISPLAY_PID = 9040;

        private CustomListExecuteReturnContext IsDevicePresentFunc(
            CustomListData data, CustomListExecuteParameterContext context)
        {
            bool present = CheckDevicePresent();
            Log?.Info($"GETT HMI: IsDevicePresent = {present}");

            var ret = context.CreateReturnContext();
            ret.Add(present);
            return ret;
        }

        private bool CheckDevicePresent()
        {
            return IsDeviceOnUsb();
        }

        private bool IsDeviceOnUsb()
        {
            try
            {
                Assembly libAsm = typeof(GETT_DisplayDevice).Assembly;
                Type? hidType = libAsm.GetType("GETT_DisplayDeviceLib.HID.HIDInterface");
                if (hidType == null) return false;

                MethodInfo? method = null;
                foreach (var m in hidType.GetMethods(BindingFlags.Public | BindingFlags.Static))
                {
                    if (m.Name == "getConnectedDevices" && m.GetParameters().Length == 2)
                    {
                        method = m;
                        break;
                    }
                }
                if (method == null) return false;

                object? result = method.Invoke(null, new object[] { DISPLAY_VID, DISPLAY_PID });
                if (result is System.Collections.IEnumerable list)
                {
                    foreach (var _ in list) return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Log?.Error($"GETT HMI: IsDeviceOnUsb Fehler: {ex.GetType().Name} - {ex.Message}");
                return false;
            }
        }

        private void EnsureDeviceConnected(string listName)
        {
            var state = GetState(listName);
            if (state.Device == null || !state.Device.IsConnected)
                throw new DataErrorException("GETT Display Device ist aktuell nicht verbunden.");
        }

        private CustomListExecuteReturnContext SetKeyImageOffFunc(
            CustomListData data, CustomListExecuteParameterContext context)
        {
            EnsureDeviceConnected(data.ListName);
            var state = GetState(data.ListName);

            int keyIndex = (int)context.Values[0].GetValueOrDefault<double>() - 1;
            string base64 = DecodeBase64Param(context.Values[1].GetValueOrDefault<string>() ?? "");

            if (string.IsNullOrWhiteSpace(base64))
                throw new DataErrorException("Kein gültiger Base64-String für Base64Image übergeben.");

            Bitmap bmp = LoadBitmapFromBase64(base64);

            _deviceSemaphore.Wait();
            try
            {
                state.Device!.SetImage(keyIndex, eDisplayDevice_KeyState.OFF, bmp);
                Log?.Info($"GETT HMI: Taste {keyIndex + 1} OFF-Bild gesendet.");
                Thread.Sleep(TransferDelayMs); 
            }
            catch (Exception ex)
            {
                throw new DataErrorException($"Fehler bei SetKeyImageOff Taste {keyIndex + 1}: {ex.Message}");
            }
            finally
            {
                bmp.Dispose();
                _deviceSemaphore.Release();
            }

            return context.CreateReturnContext();
        }

        private CustomListExecuteReturnContext SetKeyImageOnFunc(
            CustomListData data, CustomListExecuteParameterContext context)
        {
            EnsureDeviceConnected(data.ListName);
            var state = GetState(data.ListName);

            int keyIndex = (int)context.Values[0].GetValueOrDefault<double>() - 1;
            string base64 = DecodeBase64Param(context.Values[1].GetValueOrDefault<string>() ?? "");

            if (string.IsNullOrWhiteSpace(base64))
                throw new DataErrorException("Kein gültiger Base64-String für Base64Image übergeben.");

            Bitmap bmp = LoadBitmapFromBase64(base64);

            _deviceSemaphore.Wait();
            try
            {
                state.Device!.SetImage(keyIndex, eDisplayDevice_KeyState.ON, bmp);
                Log?.Info($"GETT HMI: Taste {keyIndex + 1} ON-Bild gesendet.");
                Thread.Sleep(TransferDelayMs); 
            }
            catch (Exception ex)
            {
                throw new DataErrorException($"Fehler bei SetKeyImageOn Taste {keyIndex + 1}: {ex.Message}");
            }
            finally
            {
                bmp.Dispose();
                _deviceSemaphore.Release();
            }

            return context.CreateReturnContext();
        }

        private static string DecodeBase64Param(string input)
        {
            int comma = input.IndexOf(',');
            return comma >= 0 ? input.Substring(comma + 1) : input;
        }

        private static Bitmap LoadBitmapFromBase64(string base64)
        {
            byte[] bytes;
            try { bytes = Convert.FromBase64String(base64); }
            catch (FormatException ex)
            { throw new DataErrorException($"Ungültiger Base64-String: {ex.Message}"); }

            using var ms = new MemoryStream(bytes);
            using var tempBmp = new Bitmap(ms);
            return new Bitmap(tempBmp);
        }
    }
}