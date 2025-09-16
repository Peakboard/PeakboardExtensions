using System;
using System.Collections.Generic;
using System.Linq;
using MifareReaderApp.Logic;
using MifareReaderApp.Models;
using Newtonsoft.Json;
using Peakboard.ExtensionKit;
using PCSC;
using PCSC.Monitoring;

namespace MifareReaderApp.CustomLists
{
    [Serializable]
    [CustomListIcon("MifareReader.pb_datasource_mifare.png")]
    public class MifareCardList : CustomListBase
    {
        private readonly Dictionary<string, SCardMonitor> _listNameToMonitorMap = new Dictionary<string, SCardMonitor>();

        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "MifareCardList",
                Name = "MIFARE Card Data",
                Description = "Pushes data when a MIFARE Classic card is presented.",
                PropertyInputPossible = true,
                PropertyInputDefaults = {
                    new CustomListPropertyDefinition() { Name = "ReaderIndex", Value = "0" },
                    new CustomListPropertyDefinition() { Name = "UidOnly", SelectableValues = ["false", "true"], Value = "false" },
                    new CustomListPropertyDefinition() { Name = "NdefOnly", SelectableValues = ["false", "true"], Value = "false" },
                    new CustomListPropertyDefinition() { Name = "Sectors", Value = "" },
                    new CustomListPropertyDefinition() { Name = "CustomKeyA", Value = "" },
                    new CustomListPropertyDefinition() { Name = "CustomKeyB", Value = "" },
                    new CustomListPropertyDefinition() { Name = "ClearOnCardRemoved", SelectableValues = ["true", "false",],  Value = "true" }
                }
            };
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            var columns = new CustomListColumnCollection();

            data.Properties.TryGetValue("UidOnly", StringComparison.OrdinalIgnoreCase, out var uidOnlyString);
            data.Properties.TryGetValue("NdefOnly", StringComparison.OrdinalIgnoreCase, out var ndefOnlyString);
            bool.TryParse(uidOnlyString, out bool uidOnly);
            bool.TryParse(ndefOnlyString, out bool ndefOnly);

            if (uidOnly)
            {
                columns.Add(new CustomListColumn("UID", CustomListColumnTypes.String));
            }
            if (ndefOnly)
            {
                columns.Add(new CustomListColumn("NDEF_Type", CustomListColumnTypes.String));
                columns.Add(new CustomListColumn("NDEF_Content", CustomListColumnTypes.String));
            }

            if (!uidOnly && !ndefOnly)
            {
                columns.Add(new CustomListColumn("JsonData", CustomListColumnTypes.String));
            }

            return columns;
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            try
            {
                using (var context = ContextFactory.Instance.Establish(SCardScope.System))
                {
                    var readerNames = context.GetReaders();
                    if (!readerNames.Any()) return new CustomListObjectElementCollection();

                    data.Properties.TryGetValue("ReaderIndex", StringComparison.OrdinalIgnoreCase, out var readerIndexString);
                    int.TryParse(readerIndexString, out var readerIndex);

                    string readerToUse = SelectReader(readerNames, readerIndex);
                    if (string.IsNullOrEmpty(readerToUse)) return new CustomListObjectElementCollection();

                    var readerStates = new SCardReaderState[] { new SCardReaderState { ReaderName = readerToUse, CurrentState = SCRState.Unknown } };
                    var error = context.GetStatusChange((IntPtr)10, readerStates); // Short timeout for a quick check

                    if (error == SCardError.Timeout || (readerStates[0].EventState & SCRState.Present) != SCRState.Present)
                    {
                        return new CustomListObjectElementCollection(); // No card or no change
                    }

                    return ReadAndFormatCardData(data, readerToUse, readerStates[0].Atr);
                }
            }
            catch (Exception ex)
            {
                Log?.Error("An error occurred during synchronous card read (GetItemsOverride).", ex);
                return new CustomListObjectElementCollection();
            }
        }

        protected override void SetupOverride(CustomListData data)
        {
            if (_listNameToMonitorMap.ContainsKey(data.ListName)) return;

            try
            {
                var context = ContextFactory.Instance.Establish(SCardScope.System);
                var readerNames = context.GetReaders();
                if (!readerNames.Any())
                {
                    Log?.Warning("No NFC readers found for monitoring.");
                    return;
                }

                data.Properties.TryGetValue("ReaderIndex", StringComparison.OrdinalIgnoreCase, out var readerIndexString);
                int.TryParse(readerIndexString, out var readerIndex);

                string readerToUse = SelectReader(readerNames, readerIndex);
                if (string.IsNullOrEmpty(readerToUse)) return;

                var monitor = new SCardMonitor(ContextFactory.Instance, SCardScope.System);

                monitor.CardInserted += (sender, args) => OnCardInserted(data, args);
                monitor.CardRemoved += (sender, args) => OnCardRemoved(data, args);

                _listNameToMonitorMap.Add(data.ListName, monitor);
                monitor.Start(readerToUse);
                Log?.Info($"Monitoring started for reader '{readerToUse}' on list '{data.ListName}'.");
            }
            catch (Exception ex)
            {
                Log?.Error("Failed to setup MIFARE card monitor.", ex);
            }
        }

        private void OnCardInserted(CustomListData data, CardStatusEventArgs e)
        {
            Log?.Info($"Card inserted in reader '{e.ReaderName}'. Pushing data to list '{data.ListName}'...");
            var items = ReadAndFormatCardData(data, e.ReaderName, e.Atr);
            if (items.Any())
            {
                this.Data.Push(data.ListName).Update(0, items.First());
                Log?.Info($"Pushed card data to list '{data.ListName}'.");
            }
        }

        private void OnCardRemoved(CustomListData data, CardStatusEventArgs e)
        {
            Log?.Info($"Card removed from reader '{e.ReaderName}'.");

            data.Properties.TryGetValue("ClearOnCardRemoved", StringComparison.OrdinalIgnoreCase, out var clearOnRemoveString);
            bool.TryParse(clearOnRemoveString, out bool clearOnRemove);

            if (clearOnRemove)
            {
                this.Data.Push(data.ListName).Remove(0);
                Log?.Info($"Data cleared for list '{data.ListName}'.");
            }
        }

        private CustomListObjectElementCollection ReadAndFormatCardData(CustomListData data, string readerName, byte[] atr)
        {
            var items = new CustomListObjectElementCollection();
            try
            {
                using (var context = ContextFactory.Instance.Establish(SCardScope.System))
                using (var rfidReader = context.ConnectReader(readerName, SCardShareMode.Shared, SCardProtocol.Any))
                {
                    var item = new CustomListObjectElement();

                    data.Properties.TryGetValue("UidOnly", StringComparison.OrdinalIgnoreCase, out var uidOnlyString);
                    data.Properties.TryGetValue("NdefOnly", StringComparison.OrdinalIgnoreCase, out var ndefOnlyString);
                    data.Properties.TryGetValue("Sectors", StringComparison.OrdinalIgnoreCase, out var sectors);
                    data.Properties.TryGetValue("CustomKeyA", StringComparison.OrdinalIgnoreCase, out var keyA);
                    data.Properties.TryGetValue("CustomKeyB", StringComparison.OrdinalIgnoreCase, out var keyB);
                    bool.TryParse(uidOnlyString, out bool uidOnly);
                    bool.TryParse(ndefOnlyString, out bool ndefOnly);

                    var mifareHandler = new MifareCardHandler(rfidReader, atr, readerName, Log);

                    if (uidOnly || ndefOnly)
                    {
                        if (uidOnly) { item.Add("UID", mifareHandler.GetUid()); }
                        if (ndefOnly)
                        {
                            var cardResultForNdef = mifareHandler.FullCardRead(ParseSectors(sectors), keyA, keyB);
                            var firstRecord = cardResultForNdef.CardData?.NdefMessage?.Records?.FirstOrDefault();
                            item.Add("NDEF_Type", firstRecord?.Type ?? "N/A");
                            item.Add("NDEF_Content", firstRecord?.Content ?? "N/A");
                        }
                    }
                    else
                    {
                        var cardResult = mifareHandler.FullCardRead(ParseSectors(sectors), keyA, keyB);
                        var settings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore, Formatting = Formatting.Indented };
                        item.Add("JsonData", JsonConvert.SerializeObject(cardResult, settings));
                    }
                    items.Add(item);
                }
            }
            catch (Exception ex)
            {
                Log?.Error($"An error occurred during card processing for list '{data.ListName}'.", ex);
            }
            return items;
        }

        protected override void CleanupOverride(CustomListData data)
        {
            if (_listNameToMonitorMap.TryGetValue(data.ListName, out var monitor))
            {
                monitor.Cancel();
                monitor.Dispose();
                _listNameToMonitorMap.Remove(data.ListName);
                Log?.Info($"Monitoring stopped for list '{data.ListName}'.");
            }
        }

        #region Helper Methods
        private string SelectReader(string[] allReaderNames, int? readerIndex)
        {
            if (readerIndex.HasValue)
            {
                if (readerIndex.Value >= 0 && readerIndex.Value < allReaderNames.Length)
                {
                    return allReaderNames[readerIndex.Value];
                }
                Log?.Warning($"Invalid reader index '{readerIndex.Value}'. Max index is {allReaderNames.Length - 1}.");
                return null;
            }
            return allReaderNames.FirstOrDefault();
        }

        private List<int> ParseSectors(string sectorString)
        {
            if (string.IsNullOrWhiteSpace(sectorString)) return null;
            try
            {
                var sectors = new HashSet<int>();
                var parts = sectorString.Split(',');
                foreach (var part in parts)
                {
                    if (part.Contains('-'))
                    {
                        var rangeParts = part.Split('-');
                        if (rangeParts.Length == 2 && int.TryParse(rangeParts[0], out int start) && int.TryParse(rangeParts[1], out int end) && start <= end)
                        {
                            for (int i = start; i <= end; i++) sectors.Add(i);
                        }
                    }
                    else
                    {
                        if (int.TryParse(part, out int sector)) sectors.Add(sector);
                    }
                }
                return sectors.OrderBy(s => s).ToList();
            }
            catch (Exception ex)
            {
                Log?.Warning($"Failed to parse sector string '{sectorString}'.", ex);
                return null;
            }
        }
        #endregion
    }
}