using System.Drawing;
using System.IO;
using System.Text;

namespace POSPrinter.Helper
{
    public class SeikoQRCode
    {
        public byte[] QRCode(string data)
        {
            MemoryStream stream = new MemoryStream();
            byte[] initQr = new byte[] { 0x1D, 0x70, 0x01, 0x02, 0x4C, 0x00, 0x42 };
            stream.Write(initQr, 0, initQr.Length);

            byte[] dataLength = GetLengthWithSwappedBytes(data);
            stream.Write(dataLength, 0, dataLength.Length);

            // Store QR Code Data (add correct size of the command based on data length)
            byte[] dataBytes = Encoding.ASCII.GetBytes(data);
            stream.Write(dataBytes, 0, dataBytes.Length);

            // Line feed (optional but recommended)
            byte[] lineFeed = new byte[] { 0x0A };
            stream.Write(lineFeed, 0, lineFeed.Length);

            return stream.ToArray();
        }

        private static byte[] GetLengthWithSwappedBytes(string input)
        {
            // Schritt 1: Länge des Strings ermitteln
            int length = input.Length;
            // Schritt 2: Länge in zwei Bytes aufteilen
            byte lowByte = (byte)(length & 0xFF); // Low-Byte (niedriges Byte)
            byte highByte = (byte)((length >> 8) & 0xFF); // High-Byte (höheres Byte)
            // Schritt 3: Low- und High-Byte vertauschen
            byte[] byteArray = new byte[2];
            byteArray[0] = lowByte; // Hier kommt eigentlich das High-Byte hin (vertauscht)
            byteArray[1] = highByte; // Hier kommt eigentlich das Low-Byte hin (vertauscht)
            // Schritt 4: Byte-Array zurückgeben
            return byteArray;
        }
    }
}
