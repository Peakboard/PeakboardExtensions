using System.IO.Ports;
using System.Text;


namespace CASScaleExtension.Shared
{
    class SerialPortService
    {
        private readonly SerialPort _serialPort;
        private readonly StringBuilder _buffer = new StringBuilder();

        public event EventHandler<string> LineReceived;

        public event EventHandler<byte[]> DataReceived;

        public SerialPortService(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
        {
            _serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits)
            {
                Encoding = Encoding.ASCII,
                NewLine = "\r"
            };
            _serialPort.DataReceived += SerialPort_DataReceived;
        }

        public void Open()
        {
            if (!_serialPort.IsOpen)
            {
                _serialPort.Open();
            }
        }

        public void Start()
        {
            Open();
        }

        public void Close()
        {
            if (_serialPort.IsOpen)
            {
                _serialPort.Close();
            }
        }

        public async Task SendDataAsync(string data)
        {
            if (!_serialPort.IsOpen) Open();
            await _serialPort.BaseStream.WriteAsync(Encoding.ASCII.GetBytes(data), 0, data.Length);
        }

        public async Task SendDataAsync(byte[] data)
        {
            if (!_serialPort.IsOpen) Open();
            await _serialPort.BaseStream.WriteAsync(data, 0, data.Length);
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                int bytesToRead = _serialPort.BytesToRead;
                if (bytesToRead == 0) return;

                byte[] byteBuffer = new byte[bytesToRead];
                _serialPort.Read(byteBuffer, 0, bytesToRead);

                DataReceived?.Invoke(this, byteBuffer);

                string incomingData = _serialPort.Encoding.GetString(byteBuffer);
                _buffer.Append(incomingData);

                string bufferContent = _buffer.ToString();
                int newlineIndex;

                while ((newlineIndex = bufferContent.IndexOfAny(new char[] { '\r', '\n' })) > -1)
                {
                    string line = bufferContent.Substring(0, newlineIndex).Trim();
                    bufferContent = bufferContent.Substring(newlineIndex + 1);

                    if (!string.IsNullOrEmpty(line))
                    {
                        LineReceived?.Invoke(this, line);
                    }
                }

                _buffer.Clear();
                _buffer.Append(bufferContent);
            }
            catch (Exception ex)
            {

            }
        }
    }
}
