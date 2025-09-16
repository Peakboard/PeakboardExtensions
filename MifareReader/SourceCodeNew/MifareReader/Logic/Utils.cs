using System;
using System.Collections.Generic;
using System.Linq;

namespace MifareReaderApp.Logic
{
    public static class Utils
    {
        public static byte[] HexStringToByteArray(string hex)
        {
            if (hex.Length % 2 != 0) throw new ArgumentException("Hex-String muss eine gerade Anzahl von Zeichen haben.");
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }

        public static string SanitizeAscii(string input) => new string(input.Select(c => char.IsControl(c) || c > 127 ? '.' : c).ToArray());

        public class ByteArrayEqualityComparer : IEqualityComparer<byte[]>
        {
            public bool Equals(byte[] x, byte[] y)
            {
                if (x == null || y == null) return x == y;
                return x.SequenceEqual(y);
            }
            public int GetHashCode(byte[] obj)
            {
                if (obj == null) return 0;
                return obj.Aggregate(0, (a, b) => a ^ b);
            }
        }
    }
}