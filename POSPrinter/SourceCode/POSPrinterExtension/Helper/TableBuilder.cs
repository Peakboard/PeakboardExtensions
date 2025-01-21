using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HtmlAgilityPack;


namespace POSPrinter.Helper
{
    public class TableBuilder
    {
        public byte[] GenerateEscPosFromHtml(string htmlTable, int[] columnWidths)
        {
            // HTML analysieren
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlTable);

            // Tabellenzeilen extrahieren
            var rows = htmlDoc.DocumentNode.SelectNodes("//tr");
            if (rows == null || rows.Count == 0)
                throw new ArgumentException("Ungültige oder leere HTML-Tabelle.");

            // ESC/POS-Byte-Array erstellen
            var byteList = new List<byte>();

            // Drucker initialisieren
            byteList.AddRange(Encoding.ASCII.GetBytes("\x1b\x40")); // ESC @
            byteList.AddRange(Encoding.ASCII.GetBytes("\x1b\x33\x18")); // ESC 3 - Zeilenabstand setzen
            byteList.AddRange(Encoding.ASCII.GetBytes("\x1b\x74\x10")); // ESC t 16 - Codepage 858 für €-Symbol

            // Verarbeitung der Tabelle
            foreach (var row in rows)
            {
                var cells = row.SelectNodes("th|td");
                if (cells == null) continue;

                // Unterscheide zwischen Kopfzeile (<th>) und Datenzeile (<td>)
                bool isHeader = cells.All(cell => cell.Name == "th");

                if (isHeader)
                {
                    // Kopfzeile hervorheben
                    byteList.AddRange(Encoding.ASCII.GetBytes("\x1b\x45\x01")); // ESC E 1 - Fettdruck an
                    byteList.AddRange(FormatRow(cells, columnWidths));
                    byteList.AddRange(Encoding.ASCII.GetBytes("\x1b\x45\x00")); // ESC E 0 - Fettdruck aus

                    // Leerzeile nach der Kopfzeile hinzufügen
                    byteList.AddRange(Encoding.ASCII.GetBytes("\n"));
                }
                else
                {
                    // Datenzeilen
                    byteList.AddRange(FormatRow(cells, columnWidths));
                }
            }

            return byteList.ToArray();
        }

        private byte[] FormatRow(HtmlNodeCollection cells, int[] columnWidths)
        {
            var rowBuilder = new StringBuilder();

            for (int col = 0; col < columnWidths.Length; col++)
            {
                string cellValue = col < cells.Count ? cells[col].InnerText.Trim() : string.Empty;

                // Spalte mit Leerzeichen auffüllen, um die Breite zu erreichen
                rowBuilder.Append(cellValue.PadRight(columnWidths[col]));
            }

            // Zeilenumbruch hinzufügen
            rowBuilder.Append("\n");
            return Encoding.ASCII.GetBytes(rowBuilder.ToString());
        }
    }
}
