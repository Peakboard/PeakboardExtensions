using System;
using System.Collections.Generic;
using System.Linq;
using Peakboard.ExtensionKit;
using PeakboardExtensionHL7.Hl7;

namespace PeakboardExtensionHL7.Extension
{
    /// <summary>
    /// The rows a list shows, with every change mirrored to the board through push.
    ///
    /// History: rows are appended, the oldest drop off the top beyond MaxRows.
    /// Latest: one slot per key (the patient), holding the rows of that key's newest
    /// message. Slots keep the order in which their patients first arrived. Push can
    /// only append at the end, so when a slot's row count changes, the rows from that
    /// slot on are overwritten in place and the end of the list grows or shrinks.
    ///
    /// Each row remembers its message, so a new filter can drop the rows it no
    /// longer accepts (Refilter).
    /// </summary>
    internal sealed class RowStore
    {
        private sealed class Entry
        {
            public CustomListObjectElement Row;
            public Hl7Message Message;
        }

        private sealed class Slot
        {
            public string Key;
            public int Count;
            public long Sequence;
        }

        private readonly List<Entry> _entries = new List<Entry>();
        private readonly List<Slot> _slots = new List<Slot>();
        private long _sequence;

        public IEnumerable<CustomListObjectElement> Rows => _entries.Select(e => e.Row);

        public void Append(Hl7Message message, IReadOnlyList<CustomListObjectElement> rows, int maxRows, CustomListDataServicePushObject push)
        {
            foreach (var row in rows)
            {
                _entries.Add(new Entry { Row = row, Message = message });
                push?.Add(row);
            }
            while (_entries.Count > maxRows)
            {
                _entries.RemoveAt(0);
                push?.Remove(0);
            }
        }

        public void ReplaceLatest(string key, Hl7Message message, IReadOnlyList<CustomListObjectElement> rows, int maxRows,
            CustomListDataServicePushObject push)
        {
            var entries = rows.Select(row => new Entry { Row = row, Message = message }).ToList();
            var slot = _slots.FirstOrDefault(s => SameKey(s.Key, key));
            if (slot == null)
            {
                slot = new Slot { Key = key, Count = entries.Count };
                _slots.Add(slot);
                foreach (var entry in entries)
                {
                    _entries.Add(entry);
                    push?.Add(entry.Row);
                }
            }
            else
            {
                var offset = OffsetOf(slot);
                var updated = _entries.Take(offset).Concat(entries).Concat(_entries.Skip(offset + slot.Count)).ToList();
                slot.Count = entries.Count;
                Rewrite(offset, updated, push);
            }
            slot.Sequence = ++_sequence;

            // A wildcard in PatientIds can match any number of patients: beyond MaxRows,
            // drop the patients that have gone longest without a message.
            while (_entries.Count > maxRows && _slots.Count > 1)
                RemoveSlot(_slots.Where(s => s != slot).OrderBy(s => s.Sequence).First(), push);
        }

        /// <summary>
        /// Drops the rows whose message the filter no longer accepts, e.g. after the
        /// PatientIds changed. In Latest mode the slots are keyed again by the new filter;
        /// when two end up with the same key, the one with the newer message stays.
        /// </summary>
        public void Refilter(Hl7Filter filter, bool latestOnly, CustomListDataServicePushObject push)
        {
            if (!latestOnly)
            {
                Rewrite(0, _entries.Where(e => filter.AcceptsMessage(e.Message)).ToList(), push);
                return;
            }

            var candidates = new List<(Slot Slot, List<Entry> Entries)>();
            var offset = 0;
            foreach (var slot in _slots)
            {
                var entries = _entries.GetRange(offset, slot.Count);
                offset += slot.Count;
                if (!filter.AcceptsMessage(entries[0].Message)) continue;
                slot.Key = filter.MatchedPatientId(entries[0].Message);
                candidates.Add((slot, entries));
            }

            var kept = candidates
                .Where(c => !candidates.Any(o => SameKey(o.Slot.Key, c.Slot.Key) && o.Slot.Sequence > c.Slot.Sequence))
                .ToList();
            _slots.Clear();
            _slots.AddRange(kept.Select(c => c.Slot));
            Rewrite(0, kept.SelectMany(c => c.Entries).ToList(), push);
        }

        private static bool SameKey(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

        private int OffsetOf(Slot slot) => _slots.TakeWhile(s => s != slot).Sum(s => s.Count);

        private void RemoveSlot(Slot slot, CustomListDataServicePushObject push)
        {
            var offset = OffsetOf(slot);
            var updated = _entries.Take(offset).Concat(_entries.Skip(offset + slot.Count)).ToList();
            _slots.Remove(slot);
            Rewrite(offset, updated, push);
        }

        /// <summary>
        /// Makes the entries equal to updated, which matches them before index from on.
        /// Rows that did not change are not pushed again.
        /// </summary>
        private void Rewrite(int from, List<Entry> updated, CustomListDataServicePushObject push)
        {
            for (var i = from; i < updated.Count; i++)
            {
                if (i >= _entries.Count)
                {
                    _entries.Add(updated[i]);
                    push?.Add(updated[i].Row);
                }
                else if (!ReferenceEquals(_entries[i], updated[i]))
                {
                    _entries[i] = updated[i];
                    push?.Update(i, updated[i].Row);
                }
            }
            while (_entries.Count > updated.Count)
            {
                _entries.RemoveAt(_entries.Count - 1);
                push?.Remove(_entries.Count);
            }
        }
    }
}
