using System;
using System.Collections.Concurrent;
using Peakboard.ExtensionKit;
using PeakboardExtensionHL7.Hl7;

namespace PeakboardExtensionHL7.Extension
{
    /// <summary>
    /// One row, always: is the port open, who is connected, when did the last message
    /// arrive, and the last error. The message lists are empty until something
    /// arrives, which on its own does not tell "nothing sent" from "port blocked".
    ///
    /// It shares the listener of the message lists on the same port and never decides
    /// the encoding. On its own it still opens the port, which is enough to check that
    /// a sending system can connect.
    /// </summary>
    [CustomListIcon("PeakboardExtensionHL7.HL7.png")]
    public class StatusCustomList : CustomListBase
    {
        private sealed class ListState
        {
            public string ListName;
            public MllpListener.Lease Lease;
            public Action Handler;
            public readonly object Lock = new object();
        }

        private readonly ConcurrentDictionary<string, ListState> _states =
            new ConcurrentDictionary<string, ListState>(StringComparer.OrdinalIgnoreCase);

        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "HL7Status",
                Name = "HL7 - Status",
                Description = "Health of the HL7 listener on a port: listening, connections, message counts and the last error.",
                PropertyInputPossible = true,
                SupportsPushOnly = true,
                PropertyInputDefaults =
                {
                    Hl7PushListBase.PortProperty(),
                },
            };
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            return new CustomListColumnCollection
            {
                new CustomListColumn("Status", CustomListColumnTypes.String),
                new CustomListColumn("Listening", CustomListColumnTypes.Boolean),
                new CustomListColumn("Port", CustomListColumnTypes.Number),
                new CustomListColumn("Encoding", CustomListColumnTypes.String),
                new CustomListColumn("ActiveConnections", CustomListColumnTypes.Number),
                new CustomListColumn("MessagesReceived", CustomListColumnTypes.Number),
                new CustomListColumn("MessagesRejected", CustomListColumnTypes.Number),
                new CustomListColumn("LastMessageAt", CustomListColumnTypes.String),
                new CustomListColumn("LastMessageType", CustomListColumnTypes.String),
                new CustomListColumn("LastRemoteEndpoint", CustomListColumnTypes.String),
                new CustomListColumn("LastError", CustomListColumnTypes.String),
            };
        }

        protected override void CheckDataOverride(CustomListData data) => Hl7PushListBase.ReadPort(data);

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            var port = Hl7PushListBase.ReadPort(data);
            var listener = _states.TryGetValue(data.ListName ?? "", out var state)
                ? state.Lease?.Listener
                : MllpListener.Find(port);
            return new CustomListObjectElementCollection { BuildRow(listener, port) };
        }

        protected override void SetupOverride(CustomListData data)
        {
            var listName = data.ListName ?? "";
            CleanupState(listName);

            var state = new ListState { ListName = listName };
            _states[listName] = state;

            state.Lease = MllpListener.Acquire(Hl7PushListBase.ReadPort(data), null, Log);
            var listener = state.Lease.Listener;
            state.Handler = () =>
            {
                lock (state.Lock) Data?.Push(listName).Update(0, BuildRow(listener, listener.Port));
            };
            listener.StatusChanged += state.Handler;
            state.Handler();
        }

        protected override void CleanupOverride(CustomListData data) => CleanupState(data.ListName ?? "");

        private void CleanupState(string listName)
        {
            if (!_states.TryRemove(listName, out var state)) return;
            var lease = state.Lease;
            if (lease?.Listener != null) lease.Listener.StatusChanged -= state.Handler;
            lease?.Dispose();
        }

        private static CustomListObjectElement BuildRow(MllpListener listener, int port)
        {
            if (listener == null)
            {
                return Row("Not started - the listener opens in the Peakboard Runtime", false, port, "", 0, 0, 0, "", "", "", "");
            }

            var status = listener.IsListening
                ? $"Listening on port {listener.Port}"
                : $"Not listening on port {listener.Port}";

            return Row(status, listener.IsListening, listener.Port, listener.EncodingName,
                listener.ActiveConnections, listener.MessagesReceived, listener.MessagesRejected,
                listener.LastMessageAt.HasValue ? listener.LastMessageAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : "",
                listener.LastMessageType, listener.LastRemoteEndpoint, listener.LastError);
        }

        private static CustomListObjectElement Row(string status, bool listening, int port, string encoding,
            int connections, long received, long rejected, string lastAt, string lastType, string lastRemote, string lastError)
        {
            return new CustomListObjectElement
            {
                { "Status", status },
                { "Listening", listening },
                { "Port", (double)port },
                { "Encoding", encoding },
                { "ActiveConnections", (double)connections },
                { "MessagesReceived", (double)received },
                { "MessagesRejected", (double)rejected },
                { "LastMessageAt", lastAt },
                { "LastMessageType", lastType },
                { "LastRemoteEndpoint", lastRemote },
                { "LastError", lastError },
            };
        }
    }
}
