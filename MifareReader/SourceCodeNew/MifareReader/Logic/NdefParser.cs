using System;
using System.Collections.Generic;
using System.Linq;
using MifareReaderApp.Models;
using NdefLibrary.Ndef;
using Peakboard.ExtensionKit;

namespace MifareReaderApp.Logic
{
    public static class NdefParser
    {
        public static NdefMessageModel Parse(List<MifareClassicSector> sectors, ILoggingService log)
        {
            try
            {
                var fullData = new List<byte>();
                foreach (var sector in sectors.Where(s => s.Authenticated && s.Sector > 0))
                {
                    foreach (var block in sector.Blocks)
                    {
                        fullData.AddRange(Utils.HexStringToByteArray(block.DataHex));
                    }
                }

                for (int i = 0; i < fullData.Count - 1; i++)
                {
                    if (fullData[i] == 0x03)
                    {
                        int length = fullData[i + 1];
                        if (length == 0xFF)
                        {
                            if (i + 3 < fullData.Count) { length = (fullData[i + 2] << 8) | fullData[i + 3]; i += 3; }
                            else continue;
                        }
                        else { i += 1; }

                        var ndefData = fullData.Skip(i + 1).Take(length).ToArray();
                        var message = NdefMessage.FromByteArray(ndefData);

                        var ndefModel = new NdefMessageModel();
                        foreach (var record in message)
                        {
                            var parsedRecord = new NdefRecordModel();
                            if (NdefTextRecord.IsRecordType(record))
                            {
                                var textRecord = new NdefTextRecord(record);
                                parsedRecord.Type = "Text";
                                parsedRecord.Content = $"'{textRecord.Text}' (Lang: {textRecord.LanguageCode})";
                            }
                            else if (NdefUriRecord.IsRecordType(record))
                            {
                                var uriRecord = new NdefUriRecord(record);
                                parsedRecord.Type = "URI";
                                parsedRecord.Content = uriRecord.Uri;
                            }
                            else
                            {
                                parsedRecord.Type = record.GetType().Name;
                                parsedRecord.Content = BitConverter.ToString(record.Payload).Replace("-", " ");
                            }
                            ndefModel.Records.Add(parsedRecord);
                        }
                        return ndefModel;
                    }
                    if (fullData[i] == 0xFE) break;
                }
            }
            catch (Exception ex)
            {
                log?.Warning("Failed to parse NDEF message.", ex);
            }
            return null;
        }
    }
}
