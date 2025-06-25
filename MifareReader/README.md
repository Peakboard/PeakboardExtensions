# Peakboard Extension for Mifare Card Reader

This extension provides the ability to read Mifare Classic 1k / 4k cards with Peakboard. To use this extension, you need to connect a USB NFC reader to your Peakboard Box.
Your card data is provided as a JSON string, which can be processed inside Peakboard.

## Tested Readers
* uTrust 3700F
* ACR122U NFC Reader

## Features

* Directly listen for data from any PCSC-compatible card reader.
* Access card data within your Peakboard application logic.
* Full configuration of card reader properties (Reader Index, KeyA, etc.).

## Configuration

The following parameters can be set in the extension's settings panel.

| Parameter | Description | Example Value |
| :--- | :--- | :--- |
| `ReaderIndex` | The index of the reader, starting at 0. Only change this if more than one is connected. | `0` |
| `UidOnly` | If `true`, only the card's UID is read. Options: `true`, `false`. | `false` |
| `NdefOnly` | If `true`, only NDEF data is read and provided as plain text. Options: `true`, `false`. | `false` |
| `Sectors` | Specify sectors to be read (e.g., `0-3,5,10`). Leave empty to read all. | `0-3,5,10` |
| `CustomKeyA` | Define a custom KeyA as a 12-character hex string. If left empty, a list of standard keys will be tested automatically. | `A0A1A2A3A4A5` |
| `CustomKeyB` | Define a custom KeyB as a 12-character hex string. If left empty, a list of standard keys will be tested automatically. | `B0B1B2B3B4B5` |
| `ClearOnCardRemoved` | If `true`, clears the data source when the card is removed. Options: `true`, `false`. | `true` |

## Hints

* When using this extension, you should set the data source's **Reload** option to **Manual**. The extension will automatically push new data when a card is scanned.
* You can use a Peakboard variable to dynamically set the `ReaderIndex`, like this: `#[ReaderNo]#`
* If you are switching between a full read and `UidOnly` and/or `NdefOnly`, you need to reload the data source because the data structure will change accordingly.
* The card UID is provided in a hyphenated hex format (e.g., `3A-BA-5A-C5`). You can convert it to a decimal number in Lua with the following snippet:
  ```lua
  local uid_hex = "3A-BA-5A-C5"
  local uid_decimal = tonumber(uid_hex:gsub("-", ""), 16)
  peakboard.log(uid_decimal)

## JSON Example
```json
{
  "Timestamp": "2025-06-25T13:06:19.3621719Z",
  "ReaderName": "Identiv uTrust 3700 F CL Reader 0",
  "CardInfo": {
    "Status": "Success",
    "Uid": "3A-BA-5A-C5",
    "CardType": "Mifare Classic 1K",
    "Atr": "3B-8F-80-01-80-4F-0C-A0-00-00-03-06-03-00-01-00-00-00-00-6A"
  },
  "CardData": {
    "MifareClassic": [
      {
        "Sector": 0,
        "Authenticated": true,
        "AuthenticationKey": "A",
        "KeyUsed": "FFFFFFFFFFFF",
        "Blocks": [
          {
            "BlockIndex": 0,
            "DataHex": "3ABA5AC51F880400C838002000000022",
            "DataAscii": ":?Z?.?..?8. ...\""
          },
          {
            "BlockIndex": 1,
            "DataHex": "00000000000000000000000000000000",
            "DataAscii": "................"
          },
          {
            "BlockIndex": 2,
            "DataHex": "00000000000000000000000000000000",
            "DataAscii": "................"
          }
        ]
      },
      {
        "Sector": 1,
        "Authenticated": true,
        "AuthenticationKey": "A",
        "KeyUsed": "FFFFFFFFFFFF",
        "Blocks": [
          {
            "BlockIndex": 4,
            "DataHex": "00000000000000000000000000000000",
            "DataAscii": "................"
          },
          {
            "BlockIndex": 5,
            "DataHex": "00000000000000000000000000000000",
            "DataAscii": "................"
          },
          {
            "BlockIndex": 6,
            "DataHex": "00000000000000000000000000000000",
            "DataAscii": "................"
          }
        ]
      }
    ]
  }
}
```