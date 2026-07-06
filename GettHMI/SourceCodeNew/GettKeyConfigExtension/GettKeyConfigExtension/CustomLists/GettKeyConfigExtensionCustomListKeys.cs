using System.Globalization;
using System.Reflection;
using GETT_CapDeviceLib;
using GETT_CapDeviceLib.Parameter;
using Peakboard.ExtensionKit;

namespace GettKeyConfigExtension.CustomLists
{
    [Serializable]
    [CustomListIcon("GettKeyConfigExtension.pb_datasource_gett.png")]
    class GettKeyConfigExtensionCustomListKeys : CustomListBase
    {
        private GETT_CapDevice? _deviceHandle;
        private string? _listName;
        private byte _lastState = 0;

        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "GettKeyConfigExtensionList",
                Name = "Gett HMI Keys",
                PropertyInputPossible = true,
                SupportsPushOnly = true,
                Functions = new CustomListFunctionDefinitionCollection
                {
                    new CustomListFunctionDefinition() { Name = "SetKeyColor", InputParameters = new CustomListFunctionInputParameterDefinitionCollection { new CustomListFunctionInputParameterDefinition { Name = "Key Number", Type = CustomListFunctionParameterTypes.Number }, new CustomListFunctionInputParameterDefinition { Name = "HexCode", Type = CustomListFunctionParameterTypes.String } } },
                    new CustomListFunctionDefinition() { Name = "SetMultipleKeysColor", InputParameters = new CustomListFunctionInputParameterDefinitionCollection { new CustomListFunctionInputParameterDefinition { Name = "HexCodeKey1", Optional = true, Type = CustomListFunctionParameterTypes.String }, new CustomListFunctionInputParameterDefinition { Name = "HexCodeKey2", Optional = true, Type = CustomListFunctionParameterTypes.String }, new CustomListFunctionInputParameterDefinition { Name = "HexCodeKey3", Optional = true, Type = CustomListFunctionParameterTypes.String }, new CustomListFunctionInputParameterDefinition { Name = "HexCodeKey4", Optional = true, Type = CustomListFunctionParameterTypes.String }, new CustomListFunctionInputParameterDefinition { Name = "HexCodeKey5", Optional = true, Type = CustomListFunctionParameterTypes.String }, new CustomListFunctionInputParameterDefinition { Name = "HexCodeKey6", Optional = true, Type = CustomListFunctionParameterTypes.String } } },
                    new CustomListFunctionDefinition() { Name = "SetBlinkMode_Click", InputParameters = new CustomListFunctionInputParameterDefinitionCollection { new CustomListFunctionInputParameterDefinition { Name = "Key Number", Type = CustomListFunctionParameterTypes.Number }, new CustomListFunctionInputParameterDefinition { Name = "HexOff", Type = CustomListFunctionParameterTypes.String }, new CustomListFunctionInputParameterDefinition { Name = "HexDelay", Type = CustomListFunctionParameterTypes.String }, new CustomListFunctionInputParameterDefinition { Name = "Delay", Type = CustomListFunctionParameterTypes.Number } } },
                    new CustomListFunctionDefinition() { Name = "StopBlinkMode_Click", InputParameters = new CustomListFunctionInputParameterDefinitionCollection { new CustomListFunctionInputParameterDefinition { Name = "Key Number", Type = CustomListFunctionParameterTypes.Number } } },
                    new CustomListFunctionDefinition() { Name = "ResetSettings_Click", InputParameters = new CustomListFunctionInputParameterDefinitionCollection { } },
                    new CustomListFunctionDefinition() { Name = "SetSwitchMode_Click", InputParameters = new CustomListFunctionInputParameterDefinitionCollection { new CustomListFunctionInputParameterDefinition { Name = "Key Number", Type = CustomListFunctionParameterTypes.Number }, new CustomListFunctionInputParameterDefinition { Name = "HexOff", Type = CustomListFunctionParameterTypes.String }, new CustomListFunctionInputParameterDefinition { Name = "HexOn", Type = CustomListFunctionParameterTypes.String } } },
                    new CustomListFunctionDefinition() { Name = "SetButtonMode_Click", InputParameters = new CustomListFunctionInputParameterDefinitionCollection { new CustomListFunctionInputParameterDefinition { Name = "Key Number", Type = CustomListFunctionParameterTypes.Number } } },
                    new CustomListFunctionDefinition() { Name = "SetOnDelay_Click", InputParameters = new CustomListFunctionInputParameterDefinitionCollection { new CustomListFunctionInputParameterDefinition { Name = "Key Number", Type = CustomListFunctionParameterTypes.Number }, new CustomListFunctionInputParameterDefinition { Name = "HexOff", Type = CustomListFunctionParameterTypes.String }, new CustomListFunctionInputParameterDefinition { Name = "HexDelay", Type = CustomListFunctionParameterTypes.String }, new CustomListFunctionInputParameterDefinition { Name = "Delay", Type = CustomListFunctionParameterTypes.Number }, new CustomListFunctionInputParameterDefinition { Name = "HexOn", Type = CustomListFunctionParameterTypes.String } } },
                    new CustomListFunctionDefinition() { Name = "RestOnDelay_Click", InputParameters = new CustomListFunctionInputParameterDefinitionCollection { new CustomListFunctionInputParameterDefinition { Name = "Key Number", Type = CustomListFunctionParameterTypes.Number } } },
                    new CustomListFunctionDefinition() { Name = "IsDevicePresent", ReturnParameters = new CustomListFunctionReturnParameterDefinitionCollection { new CustomListFunctionReturnParameterDefinition { Name = "Present", Type = CustomListFunctionParameterTypes.Boolean } } }
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
                    ["Timestamp"] = (double)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    ["KeyNumber"] = 0.0
                }
            };
        }

        protected override void SetupOverride(CustomListData data)
        {
            try
            {
                _listName = data.ListName;

                if (!IsDeviceOnUsb())
                {
                    Log?.Info("GETT Keys: Cap-Leiste (PID 12820) nicht am USB - Setup uebersprungen.");
                    return;
                }

                _deviceHandle = GETT_CapDevice.Instance;

                _deviceHandle.OnButtonStateChanged -= DeviceHandle_OnButtonStateChanged;
                _deviceHandle.OnButtonStateChanged += DeviceHandle_OnButtonStateChanged;

                _deviceHandle.OnConnected -= DeviceHandle_OnConnected;
                _deviceHandle.OnConnected += DeviceHandle_OnConnected;

                Log?.Info($"GETT Keys: SetupOverride. IsConnected={_deviceHandle.IsConnected}");
            }
            catch (Exception ex)
            {
                Log?.Error($"GETT Keys: SetupOverride Fehler: {ex.GetType().Name} - {ex.Message}");
                throw;
            }
        }

        protected override void CleanupOverride(CustomListData data)
        {
            if (_deviceHandle != null)
            {
                _deviceHandle.OnButtonStateChanged -= DeviceHandle_OnButtonStateChanged;
                _deviceHandle.OnConnected -= DeviceHandle_OnConnected;
            }
        }

        private void DeviceHandle_OnConnected(object sender)
        {
            Log?.Info("GETT Keys: OnConnected Event empfangen.");
        }

        private void DeviceHandle_OnButtonStateChanged(object sender, byte state)
        {
            Log?.Info($"GETT Keys: OnButtonStateChanged. state=0x{state:X2}, last=0x{_lastState:X2}");

            byte pressedKeys = (byte)((_lastState ^ state) & state);
            _lastState = state;

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

            long unixTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            try
            {
                if (Data == null)
                {
                    Log?.Warning("GETT Keys: Data Service ist NULL - Push abgebrochen.");
                    return;
                }

                Data.Push(_listName).Update(0, new CustomListObjectElement
                {
                    ["Timestamp"] = (double)unixTime,
                    ["KeyNumber"] = (double)keyNumber
                });

                Log?.Info($"GETT Keys: PUSH erfolgreich. Taste={keyNumber}");
            }
            catch (Exception ex)
            {
                Log?.Error($"GETT Keys: Push fehlgeschlagen: {ex.Message}");
            }
        }

        protected override CustomListExecuteReturnContext ExecuteFunctionOverride(
            CustomListData data, CustomListExecuteParameterContext context)
        {
            if (context.TryExecute("SetKeyColor", data, SetKeyColorFunc, out var res1)) return res1;
            if (context.TryExecute("SetMultipleKeysColor", data, SetMultipleKeysColorFunc, out var res2)) return res2;
            if (context.TryExecute("SetBlinkMode_Click", data, SetBlinkModeFunc, out var res3)) return res3;
            if (context.TryExecute("StopBlinkMode_Click", data, StopBlinkModeFunc, out var res4)) return res4;
            if (context.TryExecute("ResetSettings_Click", data, ResetSettingsFunc, out var res5)) return res5;
            if (context.TryExecute("SetSwitchMode_Click", data, SetSwitchModeFunc, out var res6)) return res6;
            if (context.TryExecute("SetButtonMode_Click", data, SetButtonModeFunc, out var res7)) return res7;
            if (context.TryExecute("SetOnDelay_Click", data, SetOnDelayFunc, out var res8)) return res8;
            if (context.TryExecute("RestOnDelay_Click", data, RestOnDelayFunc, out var res9)) return res9;
            if (context.TryExecute("IsDevicePresent", data, IsDevicePresentFunc, out var res10)) return res10;

            return new CustomListExecuteReturnContext();
        }

        private const ushort CAP_VID = 8741;
        private const ushort CAP_PID = 12820;

        private CustomListExecuteReturnContext IsDevicePresentFunc(CustomListData data, CustomListExecuteParameterContext context)
        {
            bool present = CheckDevicePresent();
            var ret = context.CreateReturnContext();
            ret.Add(present);
            return ret;
        }

        private bool CheckDevicePresent()
        {
            if (_deviceHandle != null && _deviceHandle.IsConnected) return true;
            return IsDeviceOnUsb();
        }

        private bool IsDeviceOnUsb()
        {
            try
            {
                Assembly libAsm = typeof(GETT_CapDevice).Assembly;
                Type? hidType = libAsm.GetType("GETT_CapDeviceLib.HID.HIDInterface");
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

                object? result = method.Invoke(null, new object[] { CAP_VID, CAP_PID });
                if (result is System.Collections.IEnumerable list)
                {
                    foreach (var _ in list) return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private void EnsureDeviceConnected()
        {
            if (_deviceHandle == null || !_deviceHandle.IsConnected)
                throw new DataErrorException("GETT HMI Device ist aktuell nicht verbunden.");
        }

        private CustomListExecuteReturnContext SetKeyColorFunc(CustomListData data, CustomListExecuteParameterContext context)
        {
            EnsureDeviceConnected();
            int keyIndex = int.Parse(context.Values[0].StringValue ?? "1") - 1;
            byte[] rgb = CalculateRGB(context.Values[1].StringValue);
            _deviceHandle?.SetColor(keyIndex, eCapDevice_KeyState.OFF, new CapDevice_RGBColor(rgb[0], rgb[1], rgb[2]));
            return context.CreateReturnContext();
        }

        private CustomListExecuteReturnContext SetMultipleKeysColorFunc(CustomListData data, CustomListExecuteParameterContext context)
        {
            EnsureDeviceConnected();
            for (int i = 0; i < context.Values.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(context.Values[i].StringValue)) continue;
                byte[] rgb = CalculateRGB(context.Values[i].StringValue);
                _deviceHandle?.SetColor(i, eCapDevice_KeyState.OFF, new CapDevice_RGBColor(rgb[0], rgb[1], rgb[2]));
            }
            return context.CreateReturnContext();
        }

        private CustomListExecuteReturnContext SetBlinkModeFunc(CustomListData data, CustomListExecuteParameterContext context)
        {
            EnsureDeviceConnected();
            int keyIndex = int.Parse(context.Values[0].StringValue ?? "1") - 1;
            byte[] rgbOff = CalculateRGB(context.Values[1].StringValue);
            byte[] rgbDelay = CalculateRGB(context.Values[2].StringValue);
            int delay = int.Parse(context.Values[3].StringValue ?? "0");

            _deviceHandle?.SetColor(keyIndex, eCapDevice_KeyState.OFF, new CapDevice_RGBColor(rgbOff[0], rgbOff[1], rgbOff[2]));
            _deviceHandle?.SetColor(keyIndex, eCapDevice_KeyState.DELAY, new CapDevice_RGBColor(rgbDelay[0], rgbDelay[1], rgbDelay[2]));
            _deviceHandle?.SetFlashDuration(keyIndex, eCapDevice_KeyState.OFF, TimeSpan.FromMilliseconds(delay));

            return context.CreateReturnContext();
        }

        private CustomListExecuteReturnContext StopBlinkModeFunc(CustomListData data, CustomListExecuteParameterContext context)
        {
            EnsureDeviceConnected();
            int keyIndex = int.Parse(context.Values[0].StringValue ?? "1") - 1;
            _deviceHandle?.SetFlashDuration(keyIndex, eCapDevice_KeyState.OFF, TimeSpan.Zero);
            return context.CreateReturnContext();
        }

        private CustomListExecuteReturnContext ResetSettingsFunc(CustomListData data, CustomListExecuteParameterContext context)
        {
            EnsureDeviceConnected();
            _deviceHandle?.FactoryReset();
            return context.CreateReturnContext();
        }

        private CustomListExecuteReturnContext SetSwitchModeFunc(CustomListData data, CustomListExecuteParameterContext context)
        {
            EnsureDeviceConnected();
            int keyIndex = int.Parse(context.Values[0].StringValue ?? "1") - 1;
            byte[] rgbOff = CalculateRGB(context.Values[1].StringValue);
            byte[] rgbOn = CalculateRGB(context.Values[2].StringValue);

            _deviceHandle?.SetColor(keyIndex, eCapDevice_KeyState.OFF, new CapDevice_RGBColor(rgbOff[0], rgbOff[1], rgbOff[2]));
            _deviceHandle?.SetColor(keyIndex, eCapDevice_KeyState.ON, new CapDevice_RGBColor(rgbOn[0], rgbOn[1], rgbOn[2]));
            _deviceHandle?.SetKeyMode(keyIndex, eKeyMode.SWITCH);

            return context.CreateReturnContext();
        }

        private CustomListExecuteReturnContext SetButtonModeFunc(CustomListData data, CustomListExecuteParameterContext context)
        {
            EnsureDeviceConnected();
            int keyIndex = int.Parse(context.Values[0].StringValue ?? "1") - 1;
            _deviceHandle?.SetKeyMode(keyIndex, eKeyMode.BUTTON);
            return context.CreateReturnContext();
        }

        private CustomListExecuteReturnContext SetOnDelayFunc(CustomListData data, CustomListExecuteParameterContext context)
        {
            EnsureDeviceConnected();
            int keyIndex = int.Parse(context.Values[0].StringValue ?? "1") - 1;
            byte[] rgbOff = CalculateRGB(context.Values[1].StringValue);
            byte[] rgbDelay = CalculateRGB(context.Values[2].StringValue);
            int delay = int.Parse(context.Values[3].StringValue ?? "0");
            byte[] rgbOn = CalculateRGB(context.Values[4].StringValue);

            _deviceHandle?.SetColor(keyIndex, eCapDevice_KeyState.OFF, new CapDevice_RGBColor(rgbOff[0], rgbOff[1], rgbOff[2]));
            _deviceHandle?.SetColor(keyIndex, eCapDevice_KeyState.DELAY, new CapDevice_RGBColor(rgbDelay[0], rgbDelay[1], rgbDelay[2]));
            _deviceHandle?.SetFlashDuration(keyIndex, eCapDevice_KeyState.DELAY, TimeSpan.FromMilliseconds(delay));
            _deviceHandle?.SetColor(keyIndex, eCapDevice_KeyState.ON, new CapDevice_RGBColor(rgbOn[0], rgbOn[1], rgbOn[2]));
            _deviceHandle?.SetOnDelay(keyIndex, TimeSpan.FromSeconds(1));

            return context.CreateReturnContext();
        }

        private CustomListExecuteReturnContext RestOnDelayFunc(CustomListData data, CustomListExecuteParameterContext context)
        {
            EnsureDeviceConnected();
            int keyIndex = int.Parse(context.Values[0].StringValue ?? "1") - 1;
            _deviceHandle?.SetFlashDuration(keyIndex, eCapDevice_KeyState.DELAY, TimeSpan.Zero);
            _deviceHandle?.SetOnDelay(keyIndex, TimeSpan.Zero);
            return context.CreateReturnContext();
        }

        private byte[] CalculateRGB(string? hexString)
        {
            if (string.IsNullOrWhiteSpace(hexString) || hexString.Length < 6)
                return new byte[] { 0, 0, 0 };

            if (hexString.Contains('#'))
                hexString = hexString.Replace("#", "");

            try
            {
                byte r = byte.Parse(hexString.Substring(0, 2), NumberStyles.AllowHexSpecifier);
                byte g = byte.Parse(hexString.Substring(2, 2), NumberStyles.AllowHexSpecifier);
                byte b = byte.Parse(hexString.Substring(4, 2), NumberStyles.AllowHexSpecifier);
                return new byte[] { r, g, b };
            }
            catch
            {
                return new byte[] { 0, 0, 0 };
            }
        }
    }
}