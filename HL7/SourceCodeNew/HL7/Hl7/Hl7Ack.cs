using System;
using System.Threading;

namespace PeakboardExtensionHL7.Hl7
{
    /// <summary>Builds original-mode acknowledgements (MSH + MSA).</summary>
    public static class Hl7Ack
    {
        private static long _counter;

        /// <summary>
        /// AA for a received message. Sending and receiving application/facility are
        /// swapped, and the version and processing ID are echoed, which is what
        /// interface engines match an ACK against.
        /// </summary>
        public static string BuildAccept(Hl7Message message)
        {
            var f = message.FieldSeparator;
            var enc = message.Msh.GetRawField(2);
            var msh = string.Join(f.ToString(),
                "MSH",
                enc,
                message.Msh.GetRawField(5),
                message.Msh.GetRawField(6),
                message.Msh.GetRawField(3),
                message.Msh.GetRawField(4),
                Timestamp(),
                "",
                $"ACK{message.ComponentSeparator}{message.TriggerEvent}{message.ComponentSeparator}ACK",
                NewControlId(),
                OrDefault(message.Msh.GetRawField(11), "P"),
                OrDefault(message.Msh.GetRawField(12), "2.5"));
            var msa = string.Join(f.ToString(), "MSA", "AA", message.ControlId);
            return msh + "\r" + msa + "\r";
        }

        /// <summary>AR for something that could not be parsed as HL7 at all.</summary>
        public static string BuildReject(string reason)
        {
            var text = (reason ?? "").Replace("|", " ").Replace("^", " ").Replace("~", " ").Replace("\\", " ").Replace("&", " ");
            if (text.Length > 80) text = text.Substring(0, 80);
            return $"MSH|^~\\&|PEAKBOARD||||{Timestamp()}||ACK|{NewControlId()}|P|2.5\rMSA|AR||{text}\r";
        }

        private static string Timestamp() => DateTime.Now.ToString("yyyyMMddHHmmss");

        private static string NewControlId() =>
            $"PB{DateTime.Now:yyyyMMddHHmmss}{Interlocked.Increment(ref _counter) % 10000:D4}";

        private static string OrDefault(string value, string fallback) =>
            string.IsNullOrEmpty(value) ? fallback : value;
    }
}
