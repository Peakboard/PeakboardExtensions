using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace PeakboardExtensionHL7.Hl7
{
    /// <summary>
    /// A parsed HL7 v2 message. Deliberately small: segments are kept as their raw
    /// text and split into fields on demand, because the lists mostly hand segments
    /// on unchanged and only a handful of fields are ever read.
    ///
    /// Field numbering follows HL7 convention, including the MSH quirk: MSH-1 is the
    /// field separator itself and MSH-2 the encoding characters, so MSH-9 is the
    /// message type exactly as in the standard.
    /// </summary>
    public sealed class Hl7Message
    {
        public char FieldSeparator { get; }
        public char ComponentSeparator { get; }
        public char RepetitionSeparator { get; }
        public char EscapeCharacter { get; }
        public char SubcomponentSeparator { get; }

        public IReadOnlyList<Hl7Segment> Segments { get; }
        public string RawText { get; }

        private Hl7Message(string raw, char field, char comp, char rep, char esc, char sub, List<Hl7Segment> segments)
        {
            RawText = raw;
            FieldSeparator = field;
            ComponentSeparator = comp;
            RepetitionSeparator = rep;
            EscapeCharacter = esc;
            SubcomponentSeparator = sub;
            Segments = segments;
        }

        /// <summary>
        /// Parses a message. Accepts CR, LF or CRLF as segment terminator - the
        /// standard says CR, but messages pasted from files or typed into a test
        /// tool routinely arrive with LF.
        /// </summary>
        public static Hl7Message Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new FormatException("The message is empty.");

            var lines = text.Replace("\r\n", "\r").Replace('\n', '\r')
                .Split('\r')
                .Select(l => l.Trim('\0', ' ', '\t', '\u000b', '\u001c'))
                .Where(l => l.Length > 0)
                .ToList();

            if (lines.Count == 0 || !lines[0].StartsWith("MSH", StringComparison.Ordinal) || lines[0].Length < 8)
                throw new FormatException("The message does not start with an MSH segment.");

            var msh = lines[0];
            char field = msh[3];
            // MSH-2 is normally ^~\& but may be shorter; fall back to the defaults.
            var encEnd = msh.IndexOf(field, 4);
            var enc = encEnd < 0 ? msh.Substring(4) : msh.Substring(4, encEnd - 4);
            char comp = enc.Length > 0 ? enc[0] : '^';
            char rep = enc.Length > 1 ? enc[1] : '~';
            char esc = enc.Length > 2 ? enc[2] : '\\';
            char sub = enc.Length > 3 ? enc[3] : '&';

            var segments = new List<Hl7Segment>(lines.Count);
            var msg = new Hl7Message(string.Join("\r", lines), field, comp, rep, esc, sub, segments);
            for (int i = 0; i < lines.Count; i++)
                segments.Add(new Hl7Segment(msg, lines[i], i + 1));
            return msg;
        }

        /// <summary>
        /// Parses a full message, or - when the text does not start with MSH - one or
        /// more loose segments using the default encoding characters |^~\&amp;. Lets
        /// GetValue work on the Segment column of a single row.
        /// </summary>
        public static Hl7Message ParseLenient(string text)
        {
            var trimmed = (text ?? "").TrimStart('\u000b', ' ', '\r', '\n', '\t');
            if (trimmed.StartsWith("MSH", StringComparison.Ordinal)) return Parse(trimmed);

            var lines = trimmed.Replace("\r\n", "\r").Replace('\n', '\r').Split('\r')
                .Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
            if (lines.Count == 0) throw new FormatException("The text is empty.");

            var segments = new List<Hl7Segment>(lines.Count);
            var msg = new Hl7Message(string.Join("\r", lines), '|', '^', '~', '\\', '&', segments);
            for (int i = 0; i < lines.Count; i++)
                segments.Add(new Hl7Segment(msg, lines[i], i + 1));
            return msg;
        }

        public Hl7Segment FirstSegment(string type) =>
            Segments.FirstOrDefault(s => s.Type.Equals(type, StringComparison.OrdinalIgnoreCase));

        public Hl7Segment Msh => Segments[0];
        public Hl7Segment Pid => FirstSegment("PID");

        public string MessageCode => Msh.GetValue(9, 1);
        public string TriggerEvent => Msh.GetValue(9, 2);
        public string MessageStructure => Msh.GetValue(9, 3);
        public string MessageType => string.IsNullOrEmpty(TriggerEvent) ? MessageCode : MessageCode + "^" + TriggerEvent;
        public string ControlId => Msh.GetValue(10);
        public string ProcessingId => Msh.GetValue(11);
        public string Version => Msh.GetValue(12);
        public string SendingApplication => Msh.GetValue(3, 1);
        public string SendingFacility => Msh.GetValue(4, 1);
        public string ReceivingApplication => Msh.GetValue(5, 1);
        public string ReceivingFacility => Msh.GetValue(6, 1);
        public string MessageDateTime => Msh.GetValue(7, 1);

        /// <summary>PID-3 component 1 of the first repetition; the primary patient identifier.</summary>
        public string PatientId => Pid?.GetValue(3, 1) ?? "";

        /// <summary>
        /// Every identifier the message carries for its patient: all repetitions of
        /// PID-3, plus the retained PID-2 and PID-4 that older senders still fill.
        /// </summary>
        public IEnumerable<string> AllPatientIds
        {
            get
            {
                var pid = Pid;
                if (pid == null) yield break;
                foreach (var f in new[] { 3, 2, 4 })
                {
                    var raw = pid.GetRawField(f);
                    if (string.IsNullOrEmpty(raw)) continue;
                    foreach (var repetition in raw.Split(RepetitionSeparator))
                    {
                        var id = Decode(repetition.Split(ComponentSeparator)[0]);
                        if (!string.IsNullOrEmpty(id)) yield return id;
                    }
                }
            }
        }

        /// <summary>PID-5 as "Family, Given Middle" - readable on a dashboard.</summary>
        public string PatientName
        {
            get
            {
                var pid = Pid;
                if (pid == null) return "";
                var family = pid.GetValue(5, 1);
                var given = string.Join(" ", new[] { pid.GetValue(5, 2), pid.GetValue(5, 3) }.Where(s => s.Length > 0));
                if (family.Length == 0) return given;
                return given.Length == 0 ? family : family + ", " + given;
            }
        }

        public string PatientBirthDate => Pid?.GetValue(7, 1) ?? "";
        public string PatientSex => Pid?.GetValue(8, 1) ?? "";

        /// <summary>
        /// Resolves a path such as "PID-5.1", "OBX[2]-5", "MSH-9.2" or "PID-3(2).1"
        /// (second repetition of PID-3). Returns "" when anything along the path is missing.
        /// </summary>
        public string GetValue(string path)
        {
            if (!Hl7Path.TryParse(path, out var p))
                throw new FormatException($"'{path}' is not a valid HL7 path. Expected e.g. PID-5.1 or OBX[2]-5.");
            // No segment named ("5.1"): the first segment, which is what a single
            // segment row passed on its own is.
            if (p.SegmentType == null)
                return Segments.Count > 0 ? Segments[0].GetValue(p) : "";

            var seg = Segments.Where(s => s.Type.Equals(p.SegmentType, StringComparison.OrdinalIgnoreCase))
                .Skip(p.SegmentOccurrence - 1).FirstOrDefault();
            return seg?.GetValue(p) ?? "";
        }

        /// <summary>Resolves HL7 escape sequences (\F\ \S\ \T\ \R\ \E\ \.br\ \Xhh\).</summary>
        public string Decode(string value)
        {
            if (string.IsNullOrEmpty(value) || value.IndexOf(EscapeCharacter) < 0) return value ?? "";

            var sb = new StringBuilder(value.Length);
            int i = 0;
            while (i < value.Length)
            {
                char c = value[i];
                if (c != EscapeCharacter) { sb.Append(c); i++; continue; }

                int end = value.IndexOf(EscapeCharacter, i + 1);
                if (end < 0) { sb.Append(value, i, value.Length - i); break; }

                var seq = value.Substring(i + 1, end - i - 1);
                switch (seq)
                {
                    case "F": sb.Append(FieldSeparator); break;
                    case "S": sb.Append(ComponentSeparator); break;
                    case "T": sb.Append(SubcomponentSeparator); break;
                    case "R": sb.Append(RepetitionSeparator); break;
                    case "E": sb.Append(EscapeCharacter); break;
                    case ".br": sb.Append('\n'); break;
                    default:
                        if (seq.Length > 1 && (seq[0] == 'X' || seq[0] == 'x') && seq.Length % 2 == 1)
                        {
                            try
                            {
                                for (int k = 1; k < seq.Length; k += 2)
                                    sb.Append((char)byte.Parse(seq.Substring(k, 2), NumberStyles.HexNumber));
                                break;
                            }
                            catch (FormatException) { }
                        }
                        // Unknown or formatting sequence (\H\, \N\, \.sp\ ...): drop it.
                        break;
                }
                i = end + 1;
            }
            return sb.ToString();
        }
    }

    public sealed class Hl7Segment
    {
        private readonly Hl7Message _message;
        private string[] _fields;

        public string Text { get; }
        public string Type { get; }

        /// <summary>1-based position in the message; MSH is 1.</summary>
        public int Index { get; }

        internal Hl7Segment(Hl7Message message, string text, int index)
        {
            _message = message;
            Text = text;
            Index = index;
            var sep = text.IndexOf(message.FieldSeparator);
            Type = (sep < 0 ? text : text.Substring(0, sep)).Trim().ToUpperInvariant();
        }

        public bool IsMsh => Type == "MSH";

        private string[] Fields
        {
            get
            {
                if (_fields != null) return _fields;
                var parts = Text.Split(_message.FieldSeparator);
                if (IsMsh)
                {
                    // parts[0]="MSH", parts[1]=MSH-2 ... so insert MSH-1 (the separator itself).
                    var list = new List<string>(parts.Length + 1) { parts[0], _message.FieldSeparator.ToString() };
                    list.AddRange(parts.Skip(1));
                    parts = list.ToArray();
                }
                _fields = parts;
                return _fields;
            }
        }

        /// <summary>Number of the last field present in the segment.</summary>
        public int FieldCount => Fields.Length - 1;

        /// <summary>The field as received: separators and escape sequences intact.</summary>
        public string GetRawField(int field) =>
            field >= 1 && field < Fields.Length ? Fields[field] : "";

        /// <summary>
        /// The field for display: escape sequences resolved, but only when the field is
        /// a single plain value. A field with components or repetitions is returned raw,
        /// because resolving \S\ to ^ inside it would make it ambiguous.
        /// </summary>
        public string GetDisplayField(int field)
        {
            var raw = GetRawField(field);
            if (IsMsh && field <= 2) return raw;
            if (raw.IndexOf(_message.ComponentSeparator) >= 0 || raw.IndexOf(_message.RepetitionSeparator) >= 0
                || raw.IndexOf(_message.SubcomponentSeparator) >= 0)
                return raw;
            return _message.Decode(raw);
        }

        /// <summary>First repetition, given component (1-based), escape sequences resolved.</summary>
        public string GetValue(int field, int component = 0) =>
            GetValue(new Hl7Path(Type, 1, field, 1, component, 0));

        public string GetValue(Hl7Path p)
        {
            var raw = GetRawField(p.Field);
            if (string.IsNullOrEmpty(raw)) return "";
            if (IsMsh && p.Field <= 2) return raw;

            var reps = raw.Split(_message.RepetitionSeparator);
            if (p.Repetition > reps.Length) return "";
            var value = reps[p.Repetition - 1];

            if (p.Component > 0)
            {
                var comps = value.Split(_message.ComponentSeparator);
                if (p.Component > comps.Length) return "";
                value = comps[p.Component - 1];

                if (p.Subcomponent > 0)
                {
                    var subs = value.Split(_message.SubcomponentSeparator);
                    if (p.Subcomponent > subs.Length) return "";
                    value = subs[p.Subcomponent - 1];
                }
            }
            else if (reps.Length == 1 && value.IndexOf(_message.ComponentSeparator) < 0
                     && value.IndexOf(_message.SubcomponentSeparator) < 0)
            {
                return _message.Decode(value);
            }
            else
            {
                // A whole composite field was asked for: hand it back as received.
                return value;
            }

            return _message.Decode(value);
        }
    }

    /// <summary>
    /// "PID-5.1", "OBX[2]-5", "PID-3(2).1", and for a single segment "5.1" or "5".
    /// </summary>
    public readonly struct Hl7Path
    {
        public string SegmentType { get; }
        public int SegmentOccurrence { get; }
        public int Field { get; }
        public int Repetition { get; }
        public int Component { get; }
        public int Subcomponent { get; }

        public Hl7Path(string segmentType, int occurrence, int field, int repetition, int component, int subcomponent)
        {
            SegmentType = segmentType;
            SegmentOccurrence = occurrence;
            Field = field;
            Repetition = repetition;
            Component = component;
            Subcomponent = subcomponent;
        }

        public static bool TryParse(string path, out Hl7Path result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(path)) return false;
            var s = path.Trim().ToUpperInvariant();

            string segType = null;
            int occurrence = 1;
            var dash = s.IndexOf('-');
            if (dash >= 0)
            {
                var seg = s.Substring(0, dash);
                s = s.Substring(dash + 1);
                var br = seg.IndexOf('[');
                if (br >= 0)
                {
                    if (!seg.EndsWith("]") || !int.TryParse(seg.Substring(br + 1, seg.Length - br - 2), out occurrence) || occurrence < 1)
                        return false;
                    seg = seg.Substring(0, br);
                }
                if (seg.Length != 3) return false;
                segType = seg;
            }

            var parts = s.Split('.');
            if (parts.Length > 3) return false;

            int repetition = 1;
            var fieldPart = parts[0];
            var paren = fieldPart.IndexOf('(');
            if (paren >= 0)
            {
                if (!fieldPart.EndsWith(")") || !int.TryParse(fieldPart.Substring(paren + 1, fieldPart.Length - paren - 2), out repetition) || repetition < 1)
                    return false;
                fieldPart = fieldPart.Substring(0, paren);
            }

            if (!int.TryParse(fieldPart, out var field) || field < 1) return false;
            int component = 0, subcomponent = 0;
            if (parts.Length > 1 && (!int.TryParse(parts[1], out component) || component < 1)) return false;
            if (parts.Length > 2 && (!int.TryParse(parts[2], out subcomponent) || subcomponent < 1)) return false;

            result = new Hl7Path(segType, occurrence, field, repetition, component, subcomponent);
            return true;
        }
    }
}
