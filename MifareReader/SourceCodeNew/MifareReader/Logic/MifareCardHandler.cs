using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MifareReaderApp.Models;
using Peakboard.ExtensionKit;
using PCSC;
using PCSC.Iso7816;

namespace MifareReaderApp.Logic
{
    public class MifareCardHandler
    {
        private readonly ICardReader _reader;
        private readonly byte[] _atr;
        private readonly string _readerName;
        private readonly ILoggingService _log;
        private static readonly List<byte[]> DefaultKeys = new List<byte[]>
        {
            new byte[] { 0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5 }, new byte[] { 0xD3, 0xF7, 0xD3, 0xF7, 0xD3, 0xF7 },
            new byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF }, new byte[] { 0xB0, 0xB1, 0xB2, 0xB3, 0xB4, 0xB5 },
            new byte[] { 0x4D, 0x3A, 0x99, 0xC3, 0x51, 0xDD }, new byte[] { 0x1A, 0x98, 0x2C, 0x7E, 0x45, 0x9A },
            new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }
        };

        public MifareCardHandler(ICardReader reader, byte[] atr, string readerName, ILoggingService logger)
        {
            _reader = reader;
            _atr = atr;
            _readerName = readerName;
            _log = logger;
        }

        public string GetUid()
        {
            var apdu = new CommandApdu(IsoCase.Case2Short, SCardProtocol.Any) { CLA = 0xFF, Instruction = InstructionCode.GetData, P1 = 0x00, P2 = 0x00, };
            var receiveBuffer = new byte[258];
            var command = apdu.ToArray();
            var bytesReceived = _reader.Transmit(command, receiveBuffer);
            if (bytesReceived >= 2 && receiveBuffer[bytesReceived - 2] == 0x90 && receiveBuffer[bytesReceived - 1] == 0x00)
            {
                var data = new byte[bytesReceived - 2];
                Array.Copy(receiveBuffer, data, data.Length);
                return BitConverter.ToString(data);
            }
            return "N/A";
        }

        public CardReadResult FullCardRead(List<int> sectorsToRead, string customKeyA, string customKeyB)
        {
            var cardInfo = GetBasicCardInfo();
            var result = new CardReadResult
            {
                Timestamp = DateTime.UtcNow.ToString("o"),
                ReaderName = _readerName,
                CardInfo = cardInfo,
                CardData = new CardData()
            };

            if (cardInfo.CardType.Contains("Mifare Classic"))
            {
                result.CardData.MifareClassic = ReadMifareClassicData(sectorsToRead, customKeyA, customKeyB);
                if (result.CardData.MifareClassic.Any(s => s.Authenticated))
                {
                    result.CardInfo.Status = "Success";
                    result.CardData.NdefMessage = NdefParser.Parse(result.CardData.MifareClassic, _log);
                }
                else
                {
                    result.CardInfo.Status = "AuthenticationError";
                    result.ErrorDetails = "Could not authenticate any sector with the provided keys.";
                }
            }
            else
            {
                result.CardInfo.Status = "CardNotSupported";
                result.ErrorDetails = "Only MIFARE Classic cards are currently supported for full data reads.";
            }

            return result;
        }

        private CardInfo GetBasicCardInfo()
        {
            var cardInfo = new CardInfo { Uid = GetUid(), Atr = BitConverter.ToString(_atr), CardType = "Unknown" };
            if (cardInfo.Atr.Contains("00-01")) cardInfo.CardType = "Mifare Classic 1K";
            else if (cardInfo.Atr.Contains("00-02")) cardInfo.CardType = "Mifare Classic 4K";
            else if (!string.IsNullOrWhiteSpace(cardInfo.Uid) && cardInfo.Uid != "N/A") cardInfo.CardType = "Mifare Classic (Generic)";
            return cardInfo;
        }

        private List<MifareClassicSector> ReadMifareClassicData(List<int> sectorsToRead, string customKeyA, string customKeyB)
        {
            var sectorsToIterate = sectorsToRead ?? Enumerable.Range(0, 16).ToList();
            var allSectors = new List<MifareClassicSector>();
            byte[] lastSuccessfulKeyA = null, lastSuccessfulKeyB = null;
            byte[] customKeyABytes = !string.IsNullOrEmpty(customKeyA) ? Utils.HexStringToByteArray(customKeyA) : null;
            byte[] customKeyBBytes = !string.IsNullOrEmpty(customKeyB) ? Utils.HexStringToByteArray(customKeyB) : null;

            foreach (var sectorIndex in sectorsToIterate)
            {
                var sector = new MifareClassicSector { Sector = sectorIndex, Authenticated = false };
                var firstBlockOfSector = sectorIndex * 4;

                var keysToTryA = new List<byte[]>();
                if (sectorIndex == 0) keysToTryA.Add(new byte[] { 0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5 });
                if (customKeyABytes != null) keysToTryA.Add(customKeyABytes);
                if (lastSuccessfulKeyA != null) keysToTryA.Add(lastSuccessfulKeyA);
                keysToTryA.AddRange(DefaultKeys);
                keysToTryA = keysToTryA.Distinct(new Utils.ByteArrayEqualityComparer()).ToList();

                foreach (var key in keysToTryA)
                {
                    if (Authenticate(firstBlockOfSector, MifareKeyType.KeyA, key))
                    {
                        lastSuccessfulKeyA = key;
                        sector.Authenticated = true;
                        sector.AuthenticationKey = "A";
                        sector.KeyUsed = BitConverter.ToString(key).Replace("-", "");
                        ReadSectorBlocks(sector, firstBlockOfSector);
                        break;
                    }
                }

                if (!sector.Authenticated)
                {
                    var keysToTryB = new List<byte[]>();
                    if (customKeyBBytes != null) keysToTryB.Add(customKeyBBytes);
                    if (lastSuccessfulKeyB != null) keysToTryB.Add(lastSuccessfulKeyB);
                    keysToTryB.AddRange(DefaultKeys);
                    keysToTryB = keysToTryB.Distinct(new Utils.ByteArrayEqualityComparer()).ToList();

                    foreach (var key in keysToTryB)
                    {
                        if (Authenticate(firstBlockOfSector, MifareKeyType.KeyB, key))
                        {
                            lastSuccessfulKeyB = key;
                            sector.Authenticated = true;
                            sector.AuthenticationKey = "B";
                            sector.KeyUsed = BitConverter.ToString(key).Replace("-", "");
                            ReadSectorBlocks(sector, firstBlockOfSector);
                            break;
                        }
                    }
                }
                allSectors.Add(sector);
            }
            return allSectors;
        }

        private void ReadSectorBlocks(MifareClassicSector sector, int firstBlockOfSector)
        {
            for (var block = 0; block < 3; block++)
            {
                var currentBlock = firstBlockOfSector + block;
                var blockData = ReadBlock(currentBlock);
                if (blockData != null)
                {
                    sector.Blocks.Add(new MifareBlock { BlockIndex = currentBlock, DataHex = BitConverter.ToString(blockData).Replace("-", ""), DataAscii = Utils.SanitizeAscii(Encoding.ASCII.GetString(blockData)) });
                }
            }
        }

        private bool Authenticate(int blockNumber, MifareKeyType keyType, byte[] key)
        {
            var loadKeyApdu = new CommandApdu(IsoCase.Case3Short, SCardProtocol.Any) { CLA = 0xFF, Instruction = (InstructionCode)0x82, P1 = 0x00, P2 = 0x00, Data = key };
            var receiveBuffer = new byte[2];
            var command = loadKeyApdu.ToArray();
            var bytesReceived = _reader.Transmit(command, receiveBuffer);
            if (bytesReceived != 2 || receiveBuffer[0] != 0x90 || receiveBuffer[1] != 0x00) return false;

            var authApdu = new CommandApdu(IsoCase.Case3Short, SCardProtocol.Any) { CLA = 0xFF, Instruction = (InstructionCode)0x86, P1 = 0x00, P2 = 0x00, Data = new byte[] { 0x01, 0x00, (byte)blockNumber, (byte)keyType, 0x00 } };
            command = authApdu.ToArray();
            bytesReceived = _reader.Transmit(command, receiveBuffer);
            return bytesReceived == 2 && receiveBuffer[0] == 0x90 && receiveBuffer[1] == 0x00;
        }

        private byte[] ReadBlock(int blockNumber)
        {
            var apdu = new CommandApdu(IsoCase.Case2Short, SCardProtocol.Any) { CLA = 0xFF, Instruction = InstructionCode.ReadBinary, P1 = 0x00, P2 = (byte)blockNumber, Le = 16 };
            var command = apdu.ToArray();
            var receiveBuffer = new byte[18];
            var bytesReceived = _reader.Transmit(command, receiveBuffer);
            if (bytesReceived == 18 && receiveBuffer[16] == 0x90 && receiveBuffer[17] == 0x00)
            {
                var data = new byte[16];
                Array.Copy(receiveBuffer, data, 16);
                return data;
            }
            return null;
        }
    }
}