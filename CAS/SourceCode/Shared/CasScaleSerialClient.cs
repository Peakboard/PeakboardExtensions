using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.IO.Ports; // Needed for Parity/StopBits enums

namespace CASScaleExtension.Shared
{
    public class CasScaleSerialClient : IDisposable
    {
        private SerialPortService? _serialService;
        private readonly List<byte> _incomingBuffer = new List<byte>();

        // Lock to prevent overlapping commands
        private readonly SemaphoreSlim _commandLock = new SemaphoreSlim(1, 1);

        // Flag for optimistic feedback
        private volatile bool _nakReceived = false;

        // Same events as BLE for consistency
        public event EventHandler<ScaleMeasurement>? MeasurementReceived;
        public event EventHandler<string>? BatteryReceived;
        public event EventHandler<string>? CommandStatusReceived;
        public event EventHandler<string>? ConnectionStatusChanged;

        private readonly Action<string> _logger;

        public CasScaleSerialClient(Action<string> logger)
        {
            _logger = logger;
        }

        public bool Connect(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            try
            {
                Disconnect(); 

                _logger($"[Serial] Opening {portName} ({baudRate})...");

                _serialService = new SerialPortService(portName, baudRate, parity, dataBits, stopBits);
                _serialService.DataReceived += OnDataReceived;
                _serialService.Open();

                ConnectionStatusChanged?.Invoke(this, "Connected");
                _logger("[Serial] Port opened.");
                return true;
            }
            catch (Exception ex)
            {
                _logger($"[Serial] Connection failed: {ex.Message}");
                ConnectionStatusChanged?.Invoke(this, "Error");
                return false;
            }
        }

        public void Disconnect()
        {
            if (_serialService != null)
            {
                try
                {
                    _serialService.Close();
                    _serialService.DataReceived -= OnDataReceived;
                }
                catch { }
                _serialService = null;
            }
            ConnectionStatusChanged?.Invoke(this, "Disconnected");
        }

        public async Task RequestWeightAsync() => await SendCommandAsync("RW", null);
        public async Task RequestBatteryAsync() => await SendCommandAsync("RP", null);
        public async Task ZeroScaleAsync() => await SendCommandAsync("KZ", "Zero Set");
        public async Task TareScaleAsync() => await SendCommandAsync("KT", "Tare Set");

        private async Task SendCommandAsync(string commandString, string? successMessage)
        {
            if (_serialService == null) return;

            await _commandLock.WaitAsync();
            try
            {
                _nakReceived = false;
                byte[] packet = BuildProtocolPacket(commandString);

                await _serialService.SendDataAsync(packet);

                if (!string.IsNullOrEmpty(successMessage))
                {
                    await Task.Delay(300); 
                    if (!_nakReceived)
                    {
                        CommandStatusReceived?.Invoke(this, successMessage);
                    }
                    else
                    {
                        _logger("[Serial] Success message suppressed due to NAK.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger($"[Serial] Send error: {ex.Message}");
                if (!string.IsNullOrEmpty(successMessage))
                {
                    CommandStatusReceived?.Invoke(this, "Send Error");
                }
            }
            finally
            {
                _commandLock.Release();
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

        private void OnDataReceived(object? sender, byte[] data)
        {
            lock (_incomingBuffer)
            {
                _incomingBuffer.AddRange(data);

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
                    else
                    {
                        _incomingBuffer.RemoveRange(0, etx + 1);
                    }
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

        public void Dispose()
        {
            Disconnect();
            _commandLock.Dispose();
        }
    }
}