using System.Collections.Generic;
using Peakboard.ExtensionKit;
using PeakboardExtensionHL7.Hl7;

namespace PeakboardExtensionHL7.Extension
{
    /// <summary>
    /// One row per kept segment. Every row repeats the message and patient keys, so a
    /// table filtered to OBX still shows whose result it is. The MSH and PID of every
    /// accepted message are rows of their own too, unless KeepMshPid is off: then
    /// SegmentTypes = PV1 gives exactly one row per ADT event.
    ///
    /// Field1..Field20 are the segment's fields by HL7 number, so OBX-5 is Field5 and
    /// MSH-9 is Field9. Twenty covers the fields dashboards use (OBX-14, PV1-19).
    /// </summary>
    [CustomListIcon("PeakboardExtensionHL7.HL7.png")]
    public class SegmentsCustomList : Hl7PushListBase
    {
        public const int FieldColumns = 20;

        protected override string ListId => "HL7Segments";
        protected override string ListDisplayName => "HL7 - Segments";
        protected override string ListDescription =>
            "Receives HL7 v2 messages over MLLP. One row per segment, filtered by SegmentTypes; MSH and PID are kept unless KeepMshPid is off.";
        protected override int DefaultMaxRows => 1000;

        protected override CustomListDefinition GetDefinitionOverride()
        {
            var definition = base.GetDefinitionOverride();
            definition.PropertyInputDefaults.Add(new CustomListPropertyDefinition
            {
                Name = "KeepMshPid", Value = "true",
                TypeDefinition = TypeDefinition.Boolean,
            });
            return definition;
        }

        protected override CustomListColumnCollection BuildColumns()
        {
            var columns = new CustomListColumnCollection
            {
                new CustomListColumn("ReceivedAt", CustomListColumnTypes.String),
                new CustomListColumn("MessageControlId", CustomListColumnTypes.String),
                new CustomListColumn("MessageType", CustomListColumnTypes.String),
                new CustomListColumn("MessageCode", CustomListColumnTypes.String),
                new CustomListColumn("TriggerEvent", CustomListColumnTypes.String),
                new CustomListColumn("SendingApplication", CustomListColumnTypes.String),
                new CustomListColumn("SendingFacility", CustomListColumnTypes.String),
                new CustomListColumn("PatientId", CustomListColumnTypes.String),
                new CustomListColumn("PatientName", CustomListColumnTypes.String),
                new CustomListColumn("SegmentType", CustomListColumnTypes.String),
                new CustomListColumn("SegmentIndex", CustomListColumnTypes.Number),
            };
            for (int i = 1; i <= FieldColumns; i++)
                columns.Add(new CustomListColumn("Field" + i, CustomListColumnTypes.String));
            columns.Add(new CustomListColumn("Segment", CustomListColumnTypes.String));
            return columns;
        }

        protected override IEnumerable<CustomListObjectElement> BuildRows(ReceivedMessage received, IReadOnlyList<Hl7Segment> kept)
        {
            var m = received.Message;
            var receivedAt = FormatTimestamp(received.ReceivedAt);
            var controlId = m.ControlId;
            var messageType = m.MessageType;
            var code = m.MessageCode;
            var trigger = m.TriggerEvent;
            var sendingApp = m.SendingApplication;
            var sendingFacility = m.SendingFacility;
            var patientId = m.PatientId;
            var patientName = m.PatientName;

            foreach (var segment in kept)
            {
                var row = new CustomListObjectElement
                {
                    { "ReceivedAt", receivedAt },
                    { "MessageControlId", controlId },
                    { "MessageType", messageType },
                    { "MessageCode", code },
                    { "TriggerEvent", trigger },
                    { "SendingApplication", sendingApp },
                    { "SendingFacility", sendingFacility },
                    { "PatientId", patientId },
                    { "PatientName", patientName },
                    { "SegmentType", segment.Type },
                    { "SegmentIndex", (double)segment.Index },
                };
                for (int i = 1; i <= FieldColumns; i++)
                    row.Add("Field" + i, segment.GetDisplayField(i));
                row.Add("Segment", segment.Text);
                yield return row;
            }
        }
    }
}
