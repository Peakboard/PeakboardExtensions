using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PeakboardExtensionHL7.Hl7
{
    /// <summary>
    /// Decides which messages a list keeps, and which of their segments.
    ///
    /// Each criterion is a comma- or semicolon-separated list; empty means "no
    /// restriction". MSH and PID are always kept for an accepted message, whatever
    /// SegmentTypes says, so every stored row can be traced to its message and patient.
    /// </summary>
    public sealed class Hl7Filter
    {
        private static readonly char[] Separators = { ',', ';', '\r', '\n', '\t' };
        private static readonly string[] AlwaysKept = { "MSH", "PID" };

        private readonly List<(string Code, string Event)> _messageTypes;
        private readonly HashSet<string> _segmentTypes;
        private readonly List<Regex> _patientIds;

        public string MessageTypesText { get; }
        public string SegmentTypesText { get; }
        public string PatientIdsText { get; }

        public Hl7Filter(string messageTypes, string segmentTypes, string patientIds)
        {
            MessageTypesText = messageTypes?.Trim() ?? "";
            SegmentTypesText = segmentTypes?.Trim() ?? "";
            PatientIdsText = patientIds?.Trim() ?? "";

            // "ADT", "ADT^A01", "ADT_A01" and "ADT^*" are all accepted.
            _messageTypes = Split(MessageTypesText)
                .Select(t => t.ToUpperInvariant().Replace('_', '^').Split('^'))
                .Select(p => (p[0], p.Length > 1 && p[1] != "*" ? p[1] : null))
                .ToList();

            _segmentTypes = new HashSet<string>(Split(SegmentTypesText).Select(s => s.ToUpperInvariant()));

            // Exact, case-insensitive match; * and ? are wildcards ("4711*").
            _patientIds = Split(PatientIdsText)
                .Select(p => new Regex("^" + Regex.Escape(p).Replace(@"\*", ".*").Replace(@"\?", ".") + "$",
                    RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                .ToList();
        }

        public static Hl7Filter None { get; } = new Hl7Filter("", "", "");

        public Hl7Filter WithPatientIds(string patientIds) =>
            new Hl7Filter(MessageTypesText, SegmentTypesText, patientIds);

        public bool AcceptsMessage(Hl7Message message)
        {
            if (_messageTypes.Count > 0)
            {
                var code = message.MessageCode.ToUpperInvariant();
                var ev = message.TriggerEvent.ToUpperInvariant();
                if (!_messageTypes.Any(t => t.Code == code && (t.Event == null || t.Event == ev)))
                    return false;
            }

            if (_patientIds.Count > 0)
            {
                // A message without a PID cannot belong to the patients asked for.
                var ids = message.AllPatientIds.ToList();
                if (!ids.Any(id => _patientIds.Any(r => r.IsMatch(id))))
                    return false;
            }

            return true;
        }

        public bool KeepsSegment(Hl7Segment segment) =>
            _segmentTypes.Count == 0 || AlwaysKept.Contains(segment.Type) || _segmentTypes.Contains(segment.Type);

        /// <summary>
        /// The segments kept for a message, or an empty list when the message is rejected.
        ///
        /// With SegmentTypes set, a message containing none of them is rejected as a
        /// whole: asking for OBX means "results", and an ADT with no OBX would otherwise
        /// still leave its MSH and PID rows behind.
        /// </summary>
        public IReadOnlyList<Hl7Segment> Apply(Hl7Message message)
        {
            if (!AcceptsMessage(message)) return Array.Empty<Hl7Segment>();
            if (_segmentTypes.Count > 0 && !message.Segments.Any(s => _segmentTypes.Contains(s.Type)))
                return Array.Empty<Hl7Segment>();
            return message.Segments.Where(KeepsSegment).ToList();
        }

        private static IEnumerable<string> Split(string text) =>
            (text ?? "").Split(Separators, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s.Length > 0);
    }
}
