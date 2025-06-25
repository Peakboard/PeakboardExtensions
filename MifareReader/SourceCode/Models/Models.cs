using System.Collections.Generic;

namespace MifareReaderApp.Models
{
    public class CardReadResult
    {
        public string Timestamp { get; set; }
        public string ReaderName { get; set; }
        public CardInfo CardInfo { get; set; }
        public CardData CardData { get; set; }
        public string ErrorDetails { get; set; }
    }

    public class CardInfo
    {
        public string Status { get; set; }
        public string Uid { get; set; }
        public string CardType { get; set; }
        public string Atr { get; set; }
    }

    public class CardData
    {
        public List<MifareClassicSector> MifareClassic { get; set; }
        public NdefMessageModel NdefMessage { get; set; }
    }

    public class NdefMessageModel
    {
        public List<NdefRecordModel> Records { get; set; } = new List<NdefRecordModel>();
    }

    public class NdefRecordModel
    {
        public string Type { get; set; }
        public string Content { get; set; }
    }

    public class MifareClassicSector
    {
        public int Sector { get; set; }
        public bool Authenticated { get; set; }
        public string AuthenticationKey { get; set; }
        public string KeyUsed { get; set; }
        public List<MifareBlock> Blocks { get; set; } = new List<MifareBlock>();
    }

    public class MifareBlock
    {
        public int BlockIndex { get; set; }
        public string DataHex { get; set; }
        public string DataAscii { get; set; }
    }

    public enum MifareKeyType
    {
        KeyA = 0x60,
        KeyB = 0x61
    }
}