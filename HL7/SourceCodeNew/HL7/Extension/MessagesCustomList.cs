using System.Collections.Generic;
using System.Linq;
using Peakboard.ExtensionKit;
using PeakboardExtensionHL7.Hl7;

namespace PeakboardExtensionHL7.Extension
{
    /// <summary>
    /// One row per accepted message: the MSH and PID lines as received, the header
    /// and patient fields already picked out of them, and the kept segments as text.
    /// </summary>
    [CustomListIcon("PeakboardExtensionHL7.HL7.png")]
    public class MessagesCustomList : Hl7PushListBase
    {
        protected override string ListId => "HL7Messages";
        protected override string ListDisplayName => "HL7 - Messages";
        protected override string ListDescription =>
            "Receives HL7 v2 messages over MLLP. One row per message with its MSH and PID lines and the segments kept by SegmentTypes.";
        protected override int DefaultMaxRows => 200;

        protected override CustomListColumnCollection BuildColumns()
        {
            return new CustomListColumnCollection
            {
                new CustomListColumn("ReceivedAt", CustomListColumnTypes.String),
                new CustomListColumn("MessageControlId", CustomListColumnTypes.String),
                new CustomListColumn("MessageType", CustomListColumnTypes.String),
                new CustomListColumn("MessageCode", CustomListColumnTypes.String),
                new CustomListColumn("TriggerEvent", CustomListColumnTypes.String),
                new CustomListColumn("Version", CustomListColumnTypes.String),
                new CustomListColumn("MessageDateTime", CustomListColumnTypes.String),
                new CustomListColumn("SendingApplication", CustomListColumnTypes.String),
                new CustomListColumn("SendingFacility", CustomListColumnTypes.String),
                new CustomListColumn("ReceivingApplication", CustomListColumnTypes.String),
                new CustomListColumn("ReceivingFacility", CustomListColumnTypes.String),
                new CustomListColumn("PatientId", CustomListColumnTypes.String),
                new CustomListColumn("PatientName", CustomListColumnTypes.String),
                new CustomListColumn("PatientBirthDate", CustomListColumnTypes.String),
                new CustomListColumn("PatientSex", CustomListColumnTypes.String),
                new CustomListColumn("SegmentCount", CustomListColumnTypes.Number),
                new CustomListColumn("SegmentTypes", CustomListColumnTypes.String),
                new CustomListColumn("MSH", CustomListColumnTypes.String),
                new CustomListColumn("PID", CustomListColumnTypes.String),
                new CustomListColumn("Segments", CustomListColumnTypes.String),
                new CustomListColumn("RemoteEndpoint", CustomListColumnTypes.String),
            };
        }

        protected override IEnumerable<CustomListObjectElement> BuildRows(ReceivedMessage received, IReadOnlyList<Hl7Segment> kept)
        {
            var m = received.Message;
            yield return new CustomListObjectElement
            {
                { "ReceivedAt", FormatTimestamp(received.ReceivedAt) },
                { "MessageControlId", m.ControlId },
                { "MessageType", m.MessageType },
                { "MessageCode", m.MessageCode },
                { "TriggerEvent", m.TriggerEvent },
                { "Version", m.Version },
                { "MessageDateTime", m.MessageDateTime },
                { "SendingApplication", m.SendingApplication },
                { "SendingFacility", m.SendingFacility },
                { "ReceivingApplication", m.ReceivingApplication },
                { "ReceivingFacility", m.ReceivingFacility },
                { "PatientId", m.PatientId },
                { "PatientName", m.PatientName },
                { "PatientBirthDate", m.PatientBirthDate },
                { "PatientSex", m.PatientSex },
                { "SegmentCount", (double)kept.Count },
                { "SegmentTypes", string.Join(",", kept.Select(s => s.Type)) },
                { "MSH", m.Msh.Text },
                { "PID", m.Pid?.Text ?? "" },
                // LF rather than the HL7 CR: it renders as a line break in a text box,
                // and GetValue accepts either.
                { "Segments", string.Join("\n", kept.Select(s => s.Text)) },
                { "RemoteEndpoint", received.RemoteEndpoint ?? "" },
            };
        }
    }
}
