using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Peakboard.ExtensionKit;
using PeakboardExtensionHL7.Hl7;

namespace PeakboardExtensionHL7.Extension
{
    /// <summary>
    /// What the Segments and Messages lists share: the listener lease, the filter,
    /// the row buffer with its MaxRows cap, and the Lua functions.
    ///
    /// Push-only. In History mode new rows are appended at the end; once MaxRows is
    /// reached the oldest row is removed from the top. In Latest mode the rows of the
    /// newest message replace the previous ones. The rows are also held here, so a reload
    /// (GetItems) returns what the board is showing instead of blanking it.
    ///
    /// State is keyed by list name: Peakboard may serve several data sources of the
    /// same type from one instance of this class.
    /// </summary>
    public abstract class Hl7PushListBase : CustomListBase
    {
        public const string DefaultPort = "2575";
        private const int PreviewSeconds = 10;
        private const string ModeHistory = "History";
        private const string ModeLatest = "Latest";

        private sealed class ListState
        {
            public readonly object Lock = new object();
            public readonly List<CustomListObjectElement> Rows = new List<CustomListObjectElement>();
            public string ListName;
            public int MaxRows;
            public bool LatestOnly;
            public Hl7Filter Filter;
            public MllpListener.Lease Lease;
            public Action<ReceivedMessage> Handler;
        }

        private readonly ConcurrentDictionary<string, ListState> _states =
            new ConcurrentDictionary<string, ListState>(StringComparer.OrdinalIgnoreCase);

        protected abstract string ListId { get; }
        protected abstract string ListDisplayName { get; }
        protected abstract string ListDescription { get; }
        protected abstract int DefaultMaxRows { get; }

        /// <summary>The columns, in order. Rows must supply exactly these keys.</summary>
        protected abstract CustomListColumnCollection BuildColumns();

        /// <summary>The rows one accepted message produces; kept holds the filtered segments.</summary>
        protected abstract IEnumerable<CustomListObjectElement> BuildRows(ReceivedMessage received, IReadOnlyList<Hl7Segment> kept);

        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = ListId,
                Name = ListDisplayName,
                Description = ListDescription,
                PropertyInputPossible = true,
                SupportsPushOnly = true,
                PropertyInputDefaults =
                {
                    PortProperty(),
                    new CustomListPropertyDefinition
                    {
                        Name = "Encoding", Value = "UTF-8",
                        TypeDefinition = TypeDefinition.String.With(selectableValues: new[] { "UTF-8", "ISO-8859-1", "ASCII" }),
                    },
                    new CustomListPropertyDefinition { Name = "MessageTypes", Value = "" },
                    new CustomListPropertyDefinition { Name = "SegmentTypes", Value = "" },
                    new CustomListPropertyDefinition { Name = "PatientIds", Value = "" },
                    new CustomListPropertyDefinition
                    {
                        Name = "Mode", Value = ModeHistory,
                        TypeDefinition = TypeDefinition.String.With(selectableValues: new[] { ModeHistory, ModeLatest }),
                    },
                    new CustomListPropertyDefinition
                    {
                        Name = "MaxRows", Value = DefaultMaxRows.ToString(),
                        TypeDefinition = TypeDefinition.Number.With(integer: true, minimum: 1, maximum: 100000),
                    },
                },
                Functions =
                {
                    new CustomListFunctionDefinition
                    {
                        Name = "SetPatientIds",
                        Description = "Replaces the PatientIds filter at runtime, e.g. from a patient picker. " +
                                      "Applies to messages received from now on; call Clear first to drop rows of other patients.",
                        InputParameters =
                        {
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "patientIds",
                                Type = CustomListFunctionParameterTypes.String,
                                Description = "Comma-separated patient IDs, * and ? as wildcards. Empty for all patients.",
                            },
                        },
                    },
                    new CustomListFunctionDefinition
                    {
                        Name = "ProcessMessage",
                        Description = "Runs an HL7 message through this list's filter as if it had been received. " +
                                      "For testing a board without a sending system.",
                        InputParameters =
                        {
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "message",
                                Type = CustomListFunctionParameterTypes.String,
                                Description = "The HL7 message; segments separated by CR or LF.",
                            },
                        },
                        ReturnParameters =
                        {
                            new CustomListFunctionReturnParameterDefinition
                            {
                                Name = "result",
                                Type = CustomListFunctionParameterTypes.String,
                                Description = "\"OK\" with the number of rows added, \"FILTERED\" if the filter rejected it, or the parse error.",
                            },
                        },
                    },
                    new CustomListFunctionDefinition
                    {
                        Name = "GetValue",
                        Description = "Reads one value from a message or segment, e.g. GetValue(row.Segment, '5.1') or GetValue(row.MSH, 'MSH-9.2').",
                        InputParameters =
                        {
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "text",
                                Type = CustomListFunctionParameterTypes.String,
                                Description = "A whole message, or a single segment such as the Segment column.",
                            },
                            new CustomListFunctionInputParameterDefinition
                            {
                                Name = "path",
                                Type = CustomListFunctionParameterTypes.String,
                                Description = "Field path: 'PID-5.1', 'OBX[2]-5', 'PID-3(2).1', or '5.1' for a single segment.",
                            },
                        },
                        ReturnParameters =
                        {
                            new CustomListFunctionReturnParameterDefinition
                            {
                                Name = "value",
                                Type = CustomListFunctionParameterTypes.String,
                                Description = "The value with escape sequences resolved, or an empty string if absent.",
                            },
                        },
                    },
                },
            };
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data) => BuildColumns();

        protected override void CheckDataOverride(CustomListData data)
        {
            ReadPort(data);
            ReadMaxRows(data);
            ReadLatestOnly(data);
            ReadKeepMshPid(data);
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            var items = new CustomListObjectElementCollection();
            if (_states.TryGetValue(data.ListName ?? "", out var state))
            {
                lock (state.Lock)
                    foreach (var row in state.Rows) items.Add(row);
            }
            else
            {
                // The Runtime always calls Setup before loading; the Designer preview never does.
                // So no state means preview: listen on the port for a moment and show what arrives.
                foreach (var row in ListenForPreview(data)) items.Add(row);
            }
            return items;
        }

        /// <summary>
        /// Opens the port for PreviewSeconds and returns the rows of the messages that
        /// arrived and passed the filter. The sender gets its ACK as usual.
        /// </summary>
        private List<CustomListObjectElement> ListenForPreview(CustomListData data)
        {
            var filter = ReadFilter(data);
            var maxRows = ReadMaxRows(data);
            var latestOnly = ReadLatestOnly(data);
            var rows = new List<CustomListObjectElement>();
            var rowsLock = new object();
            Action<ReceivedMessage> handler = received =>
            {
                var kept = filter.Apply(received.Message);
                if (kept.Count == 0) return;
                lock (rowsLock) StoreRows(rows, BuildRows(received, kept).ToList(), latestOnly, maxRows, null);
            };

            using (var lease = MllpListener.Acquire(ReadPort(data), ReadProperty(data, "Encoding", "UTF-8"), Log))
            {
                if (!lease.Listener.IsListening)
                    throw new InvalidOperationException(lease.Listener.LastError);

                lease.Listener.MessageReceived += handler;
                try { Thread.Sleep(TimeSpan.FromSeconds(PreviewSeconds)); }
                finally { lease.Listener.MessageReceived -= handler; }
            }

            lock (rowsLock) return rows.ToList();
        }

        protected override void SetupOverride(CustomListData data)
        {
            var listName = data.ListName ?? "";
            CleanupState(listName);

            var state = new ListState
            {
                ListName = listName,
                MaxRows = ReadMaxRows(data),
                LatestOnly = ReadLatestOnly(data),
                Filter = ReadFilter(data),
            };
            state.Handler = received => OnMessage(state, received);

            _states[listName] = state;

            state.Lease = MllpListener.Acquire(ReadPort(data), ReadProperty(data, "Encoding", "UTF-8"), Log);
            state.Lease.Listener.MessageReceived += state.Handler;

            // Thrown, so the board shows a data source error instead of a list that
            // stays empty. The usual cause is the port being taken, often by Designer
            // and Runtime on one PC.
            if (!state.Lease.Listener.IsListening)
            {
                var error = state.Lease.Listener.LastError;
                CleanupState(listName);
                throw new InvalidOperationException(error);
            }

            Log?.Info($"[HL7] {ListDisplayName} '{listName}' on port {state.Lease.Listener.Port}: " +
                      $"MessageTypes='{state.Filter.MessageTypesText}' SegmentTypes='{state.Filter.SegmentTypesText}' " +
                      $"PatientIds='{state.Filter.PatientIdsText}' KeepMshPid={state.Filter.KeepMshPid} " +
                      $"Mode={(state.LatestOnly ? ModeLatest : ModeHistory)} MaxRows={state.MaxRows}");
        }

        protected override void CleanupOverride(CustomListData data) => CleanupState(data.ListName ?? "");

        private void CleanupState(string listName)
        {
            if (!_states.TryRemove(listName, out var state)) return;
            var lease = state.Lease;
            if (lease?.Listener != null) lease.Listener.MessageReceived -= state.Handler;
            lease?.Dispose();
        }

        protected override CustomListExecuteReturnContext ExecuteFunctionOverride(CustomListData data, CustomListExecuteParameterContext context)
        {
            var ret = new CustomListExecuteReturnContext();
            var name = context.FunctionName ?? "";
            _states.TryGetValue(data.ListName ?? "", out var state);

            if (name.Equals("SetPatientIds", StringComparison.OrdinalIgnoreCase))
            {
                var ids = context.Values.Count > 0 ? context.Values[0].StringValue : "";
                if (state != null)
                {
                    lock (state.Lock) state.Filter = state.Filter.WithPatientIds(ids);
                    Log?.Info($"[HL7] '{state.ListName}' PatientIds set to '{ids}'.");
                }
            }
            else if (name.Equals("ProcessMessage", StringComparison.OrdinalIgnoreCase))
            {
                var text = context.Values.Count > 0 ? context.Values[0].StringValue : "";
                ret.Add(ProcessManually(state, data, text));
            }
            else if (name.Equals("GetValue", StringComparison.OrdinalIgnoreCase))
            {
                var text = context.Values.Count > 0 ? context.Values[0].StringValue : "";
                var path = context.Values.Count > 1 ? context.Values[1].StringValue : "";
                string value;
                try { value = Hl7Message.ParseLenient(text).GetValue(path); }
                catch (FormatException ex)
                {
                    Log?.Warning($"[HL7] GetValue('{path}'): {ex.Message}");
                    value = "";
                }
                ret.Add(value);
            }

            return ret;
        }

        private string ProcessManually(ListState state, CustomListData data, string text)
        {
            Hl7Message message;
            try { message = Hl7Message.Parse(text); }
            catch (FormatException ex) { return ex.Message; }

            if (state == null)
                return "The list is not running. ProcessMessage works in the Peakboard Runtime or a running Designer preview.";

            var received = new ReceivedMessage { Message = message, ReceivedAt = DateTime.Now, RemoteEndpoint = "ProcessMessage" };
            var added = OnMessage(state, received);
            return added > 0 ? $"OK {added}" : "FILTERED";
        }

        /// <summary>Filters, builds and pushes the rows for one message. Returns the number of rows added.</summary>
        private int OnMessage(ListState state, ReceivedMessage received)
        {
            lock (state.Lock)
            {
                var kept = state.Filter.Apply(received.Message);
                if (kept.Count == 0) return 0;

                var rows = BuildRows(received, kept).ToList();
                StoreRows(state.Rows, rows, state.LatestOnly, state.MaxRows, Data?.Push(state.ListName));
                return rows.Count;
            }
        }

        /// <summary>
        /// Puts the rows of one message into target and mirrors each change to push, if given.
        /// Latest mode overwrites the existing rows in place, so a single-row list keeps its row.
        /// </summary>
        private static void StoreRows(List<CustomListObjectElement> target, IReadOnlyList<CustomListObjectElement> rows,
            bool latestOnly, int maxRows, CustomListDataServicePushObject push)
        {
            if (latestOnly)
            {
                for (var i = 0; i < rows.Count; i++)
                {
                    if (i < target.Count)
                    {
                        target[i] = rows[i];
                        push?.Update(i, rows[i]);
                    }
                    else
                    {
                        target.Add(rows[i]);
                        push?.Add(rows[i]);
                    }
                }
                while (target.Count > rows.Count)
                {
                    target.RemoveAt(target.Count - 1);
                    push?.Remove(target.Count);
                }
                return;
            }

            foreach (var row in rows)
            {
                target.Add(row);
                push?.Add(row);
            }
            while (target.Count > maxRows)
            {
                target.RemoveAt(0);
                push?.Remove(0);
            }
        }

        protected static string ReadProperty(CustomListData data, string name, string fallback)
        {
            return data.Properties.TryGetValue(name, StringComparison.OrdinalIgnoreCase, out var value) && value != null
                ? value.Trim()
                : fallback;
        }

        internal static CustomListPropertyDefinition PortProperty() => new CustomListPropertyDefinition
        {
            Name = "Port", Value = DefaultPort,
            TypeDefinition = TypeDefinition.Number.With(integer: true, minimum: 1, maximum: 65535),
        };

        internal static int ReadPort(CustomListData data)
        {
            var text = ReadProperty(data, "Port", DefaultPort);
            if (!TryParseInt(text, out var port) || port < 1 || port > 65535)
                throw new InvalidOperationException($"Port must be a number between 1 and 65535, not '{text}'.");
            return port;
        }

        private int ReadMaxRows(CustomListData data)
        {
            var text = ReadProperty(data, "MaxRows", DefaultMaxRows.ToString());
            if (!TryParseInt(text, out var max) || max < 1)
                throw new InvalidOperationException($"MaxRows must be a positive number, not '{text}'.");
            return max;
        }

        private static bool ReadLatestOnly(CustomListData data)
        {
            var text = ReadProperty(data, "Mode", ModeHistory);
            if (text.Equals(ModeLatest, StringComparison.OrdinalIgnoreCase)) return true;
            if (text.Length == 0 || text.Equals(ModeHistory, StringComparison.OrdinalIgnoreCase)) return false;
            throw new InvalidOperationException($"Mode must be '{ModeHistory}' or '{ModeLatest}', not '{text}'.");
        }

        /// <summary>Accepts "2575" as well as "2575.0", which a number-typed property may hand over.</summary>
        private static bool TryParseInt(string text, out int value)
        {
            value = 0;
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var d)
                && !double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out d))
                return false;
            if (d != Math.Floor(d) || d < int.MinValue || d > int.MaxValue) return false;
            value = (int)d;
            return true;
        }

        /// <summary>Only the Segments list offers KeepMshPid; without the property MSH and PID are kept.</summary>
        private static bool ReadKeepMshPid(CustomListData data)
        {
            var text = ReadProperty(data, "KeepMshPid", "true");
            if (text.Length == 0 || text.Equals("true", StringComparison.OrdinalIgnoreCase) || text == "1") return true;
            if (text.Equals("false", StringComparison.OrdinalIgnoreCase) || text == "0") return false;
            throw new InvalidOperationException($"KeepMshPid must be true or false, not '{text}'.");
        }

        private static Hl7Filter ReadFilter(CustomListData data) =>
            new Hl7Filter(
                ReadProperty(data, "MessageTypes", ""),
                ReadProperty(data, "SegmentTypes", ""),
                ReadProperty(data, "PatientIds", ""),
                ReadKeepMshPid(data));

        protected static string FormatTimestamp(DateTime value) => value.ToString("yyyy-MM-dd HH:mm:ss");
    }
}
