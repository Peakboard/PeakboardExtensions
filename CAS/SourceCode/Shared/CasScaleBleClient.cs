using InTheHand.Bluetooth;
using System.Text;
using System.Text.RegularExpressions;


namespace CASScaleExtension.Shared
{
    public class ScaleMeasurement
    {
        public double Weight { get; set; }
        public string Status { get; set; } = "";
    }

    public class CasScaleBleClient : IDisposable
    {
        // UUIDs as per documentation
        private static readonly Guid ServiceUuid = Guid.Parse("00002b00-0000-1000-8000-00805f9b34fb");
        private static readonly Guid WriteCharUuid = Guid.Parse("00002b11-0000-1000-8000-00805f9b34fb");
        private static readonly Guid ReadCharUuid = Guid.Parse("00002b10-0000-1000-8000-00805f9b34fb");

        private BluetoothDevice? _device;
        private GattCharacteristic? _writeCharacteristic;
        private GattCharacteristic? _readCharacteristic;
        private readonly List<byte> _incomingBuffer = new List<byte>();

        private readonly SemaphoreSlim _connectLock = new SemaphoreSlim(1, 1);
        private volatile bool _nakReceived = false;

        public event EventHandler<ScaleMeasurement>? MeasurementReceived;
        public event EventHandler<string>? BatteryReceived;
        public event EventHandler<string>? CommandStatusReceived;
        public event EventHandler<string>? ConnectionStatusChanged;

        private readonly Action<string> _logger;

        public CasScaleBleClient(Action<string> logger)
        {
            _logger = logger;
        }

        public async Task<bool> ConnectAsync(string targetNameOrId)
        {
            await _connectLock.WaitAsync();

            try
            {
                if (_device != null && _device.Gatt.IsConnected)
                {
                    return true;
                }

                _logger($"[BLE] Connecting to '{targetNameOrId}'...");
                string searchId = targetNameOrId.Replace(":", "").Replace("-", "").ToLower();

                CleanupInternal();

                for (int i = 1; i <= 3; i++)
                {
                    try
                    {
                        var pairedDevices = await Bluetooth.GetPairedDevicesAsync();
                        var targetDevice = pairedDevices.FirstOrDefault(d =>
                            d.Id.Replace(":", "").ToLower().Contains(searchId) ||
                            d.Name.Contains(targetNameOrId, StringComparison.OrdinalIgnoreCase));

                        if (targetDevice == null && i == 1)
                        {
                            var options = new RequestDeviceOptions { AcceptAllDevices = true };
                            var scanned = await Bluetooth.ScanForDevicesAsync(options);
                            targetDevice = scanned.FirstOrDefault(d =>
                                d.Id.Replace(":", "").ToLower().Contains(searchId) ||
                                d.Name.Contains(targetNameOrId, StringComparison.OrdinalIgnoreCase));
                        }

                        if (targetDevice == null)
                        {
                            await Task.Delay(1000);
                            continue;
                        }

                        _device = targetDevice;
                        await _device.Gatt.ConnectAsync();

                        _device.GattServerDisconnected += OnGattServerDisconnected;

                        var service = await _device.Gatt.GetPrimaryServiceAsync(ServiceUuid);
                        if (service == null) throw new Exception("Service not found");

                        _writeCharacteristic = await service.GetCharacteristicAsync(WriteCharUuid);
                        _readCharacteristic = await service.GetCharacteristicAsync(ReadCharUuid);

                        if (_writeCharacteristic == null || _readCharacteristic == null) throw new Exception("Characteristics missing");

                        _readCharacteristic.CharacteristicValueChanged += OnCharacteristicValueChanged;
                        await _readCharacteristic.StartNotificationsAsync();

                        _logger("[BLE] Connected.");
                        ConnectionStatusChanged?.Invoke(this, "Connected");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger($"[BLE] Error attempt {i}: {ex.Message}");
                        CleanupInternal();
                        if (i < 3) await Task.Delay(2000);
                    }
                }
                return false;
            }
            finally
            {
                _connectLock.Release();
            }
        }

        private void OnGattServerDisconnected(object? sender, EventArgs e)
        {
            _logger("[BLE] Connection lost.");
            ConnectionStatusChanged?.Invoke(this, "Disconnected");
        }

        public async Task RequestWeightAsync() => await SendCommandAsync("RW", null);
        public async Task RequestBatteryAsync() => await SendCommandAsync("RP", null);

        public async Task ZeroScaleAsync() => await SendCommandAsync("KZ", "Zero Set");
        public async Task TareScaleAsync() => await SendCommandAsync("KT", "Tare Set");

        private async Task SendCommandAsync(string commandString, string? successMessage)
        {
            if (_writeCharacteristic == null) return;
            try
            {
                _nakReceived = false;

                byte[] packet = BuildProtocolPacket(commandString);
                await _writeCharacteristic.WriteValueWithoutResponseAsync(packet);

                if (!string.IsNullOrEmpty(successMessage))
                {
                    await Task.Delay(300);

                    if (!_nakReceived)
                    {
                        CommandStatusReceived?.Invoke(this, successMessage);
                    }
                    else
                    {
                        _logger("[BLE] Success message suppressed due to NAK.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger($"[BLE] Send Error: {ex.Message}");
                if (!string.IsNullOrEmpty(successMessage))
                {
                    CommandStatusReceived?.Invoke(this, "Send Error");
                }
            }
        }

        private byte[] BuildProtocolPacket(string command)
        {
            const byte STX = 0x02;
            const byte ETX = 0x03;
            byte[] dataBytes = Encoding.ASCII.GetBytes(command);
            byte length = (byte)dataBytes.Length;
            byte bcc = length;
            foreach (byte b in dataBytes) bcc ^= b;

            byte[] packet = new byte[1 + 1 + dataBytes.Length + 1 + 1];
            packet[0] = STX;
            packet[1] = length;
            Array.Copy(dataBytes, 0, packet, 2, dataBytes.Length);
            packet[packet.Length - 2] = bcc;
            packet[packet.Length - 1] = ETX;
            return packet;
        }

        private void OnCharacteristicValueChanged(object? sender, GattCharacteristicValueChangedEventArgs args)
        {
            lock (_incomingBuffer)
            {
                _incomingBuffer.AddRange(args.Value);
                while (_incomingBuffer.Contains(0x03))
                {
                    int etx = _incomingBuffer.IndexOf(0x03);
                    int stx = _incomingBuffer.LastIndexOf(0x02, etx);
                    if (stx != -1)
                    {
                        int len = etx - stx + 1;
                        byte[] packet = _incomingBuffer.GetRange(stx, len).ToArray();
                        ProcessPacket(packet);
                        _incomingBuffer.RemoveRange(0, etx + 1);
                    }
                    else _incomingBuffer.RemoveRange(0, etx + 1);
                }
            }
        }

        private void ProcessPacket(byte[] packet)
        {
            if (packet.Length < 4) return;
            byte len = packet[1];
            if (packet.Length < 2 + len + 2) return;

            byte[] dataBytes = new byte[len];
            Array.Copy(packet, 2, dataBytes, 0, len);

            if (dataBytes.Length > 0 && dataBytes[0] == 0x15)
            {
                _nakReceived = true;
                CommandStatusReceived?.Invoke(this, "Failed (NAK)");
                _logger("[BLE] Command rejected (NAK).");
                return;
            }

            string cleanStr = Encoding.ASCII.GetString(dataBytes).Trim().Replace("\0", "");

            if (cleanStr.StartsWith("RW,"))
            {
                ParseWeight(cleanStr);
            }
            else if (cleanStr.StartsWith("RP,"))
            {
                BatteryReceived?.Invoke(this, cleanStr.Substring(3));
            }
        }

        private void ParseWeight(string content)
        {
            try
            {
                string payload = content.Substring(3);
                if (payload.Length < 1) return;
                char code = payload[0];
                string status = code == 'S' ? "Stable" : (code == 'U' ? "Unstable" : "Unknown");

                string weightPart = payload.Substring(1);
                var match = Regex.Match(weightPart, @"[-+]?[0-9]*[.,]?[0-9]+");

                if (match.Success)
                {
                    string cleanWeight = match.Value.Replace(',', '.');
                    if (double.TryParse(cleanWeight, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double val))
                    {
                        MeasurementReceived?.Invoke(this, new ScaleMeasurement { Weight = val, Status = status });
                    }
                }
            }
            catch { }
        }

        private void CleanupInternal()
        {
            if (_device != null)
            {
                try
                {
                    _device.GattServerDisconnected -= OnGattServerDisconnected;
                    if (_readCharacteristic != null) _readCharacteristic.CharacteristicValueChanged -= OnCharacteristicValueChanged;
                    _device.Gatt.Disconnect();
                }
                catch { }
                _device = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }

        public void Dispose()
        {
            CleanupInternal();
            _connectLock.Dispose();
        }
    }
}