using System;
using System.Collections.Generic;
using System.Text;

namespace POSPrinter.Helper
{
    public class PureESCPosBuilder
    {
        public byte[] BuildEscPosArray(string escPosCommands)
        {
            // Teile den String in einzelne Befehle auf
            string[] commandParts = escPosCommands.Split(new[] { "\\x" }, StringSplitOptions.RemoveEmptyEntries);

            // Liste zur Speicherung der Bytes
            var byteList = new List<byte>();

            foreach (string part in commandParts)
            {
                if (part.Length >= 2 && byte.TryParse(part.Substring(0, 2), System.Globalization.NumberStyles.HexNumber, null, out byte parsedByte))
                {
                    // Füge das Byte hinzu
                    byteList.Add(parsedByte);

                    // Falls noch Text nach dem Byte kommt, füge diesen direkt als ASCII hinzu
                    if (part.Length > 2)
                    {
                        string remainingText = part.Substring(2);
                        byteList.AddRange(Encoding.ASCII.GetBytes(remainingText));
                    }
                }
                else
                {
                    // Falls kein Byte-Format erkannt wurde, betrachte es als normalen Text
                    byteList.AddRange(Encoding.ASCII.GetBytes(part));
                }
            }

            return byteList.ToArray();
        }
    }
}

