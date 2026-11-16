using System.Collections.Concurrent;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Peakboard.ExtensionKit;

namespace WebSocketClient;

[Serializable]
internal class WebSocketCustomList : CustomListBase
{
    private const string FnSendText = "SendText";
    private const string FnSendKeyValueJson = "SendKeyValueJson";
    private const string FnClearBuffer = "ClearBuffer";
    private const string FnReconnect = "Reconnect";
    private const string FnDisconnect = "Disconnect";

    /// <summary>How long "Load data" in the Designer listens when no live connection exists yet.</summary>
    private static readonly TimeSpan ProbeDuration = TimeSpan.FromSeconds(3);

    /// <summary>How long the Designer waits for a first JSON message to derive columns from.</summary>
    private static readonly TimeSpan SchemaProbeTimeout = TimeSpan.FromSeconds(5);

    // One instance serves every data source of this type, so connections are kept per list name.
    [NonSerialized]
    private ConcurrentDictionary<string, WebSocketSession>? _sessions;

    private ConcurrentDictionary<string, WebSocketSession> Sessions =>
        _sessions ??= new ConcurrentDictionary<string, WebSocketSession>(StringComparer.OrdinalIgnoreCase);

    protected override CustomListDefinition GetDefinitionOverride()
    {
        return new CustomListDefinition
        {
            ID = "WebSocketClientList",
            Name = "WebSocket",
            Description = "Receives WebSocket messages live and sends messages over the same connection.",
            PropertyInputPossible = true,

            // The rows arrive through Data.Push while the connection is open; a periodic reload
            // would only re-read the same buffer. The Designer sets the data source's reload
            // state to Push for such lists.
            SupportsPushOnly = true,
            PropertyInputDefaults =
            {
                new CustomListPropertyDefinition { Name = "WsUrl", Value = "ws://127.0.0.1:1880/ws" },
                new CustomListPropertyDefinition { Name = "CustomHeaders", Value = "", TypeDefinition = new CustomListPropertyStringTypeDefinition { Masked = true } },
                new CustomListPropertyDefinition { Name = "SubProtocol", Value = "" },
                new CustomListPropertyDefinition { Name = "IgnoreSslErrors", Value = "False", TypeDefinition = new CustomListPropertyBooleanTypeDefinition() },
                new CustomListPropertyDefinition
                {
                    Name = "BufferMode",
                    Value = WebSocketSettings.BufferModeRingBuffer,
                    TypeDefinition = new CustomListPropertyStringTypeDefinition
                    {
                        SelectableValues = new[] { WebSocketSettings.BufferModeRingBuffer, WebSocketSettings.BufferModeSingleRow },
                    },
                },
                new CustomListPropertyDefinition { Name = "MaxRows", Value = "100", TypeDefinition = new CustomListPropertyNumberTypeDefinition { Integer = true, Minimum = 1, Maximum = 100000 } },
                new CustomListPropertyDefinition { Name = "ParseTopLevelJson", Value = "False", TypeDefinition = new CustomListPropertyBooleanTypeDefinition() },
                new CustomListPropertyDefinition { Name = "SampleJson", Value = "{}", TypeDefinition = new CustomListPropertyStringTypeDefinition { MultiLine = true } },
                new CustomListPropertyDefinition { Name = "IncludeSentMessages", Value = "False", TypeDefinition = new CustomListPropertyBooleanTypeDefinition() },
                new CustomListPropertyDefinition { Name = "OnConnectPayload", Value = "", TypeDefinition = new CustomListPropertyStringTypeDefinition { MultiLine = true } },
                new CustomListPropertyDefinition { Name = "AutoReconnect", Value = "True", TypeDefinition = new CustomListPropertyBooleanTypeDefinition() },
                new CustomListPropertyDefinition { Name = "ReconnectIntervalMs", Value = "5000", TypeDefinition = new CustomListPropertyNumberTypeDefinition { Integer = true, Minimum = 100, Maximum = 3600000 } },
                new CustomListPropertyDefinition { Name = "KeepAliveSeconds", Value = "30", TypeDefinition = new CustomListPropertyNumberTypeDefinition { Integer = true, Minimum = 0, Maximum = 3600 } },
            },
            Functions =
            {
                new CustomListFunctionDefinition
                {
                    Name = FnSendText,
                    Description = "Sends a string or JSON payload over the open WebSocket.",
                    InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                    {
                        new CustomListFunctionInputParameterDefinition { Name = "Message", Description = "The text to send.", Optional = false, Type = CustomListFunctionParameterTypes.String },
                    },
                    ReturnParameters = SuccessReturn(),
                },
                new CustomListFunctionDefinition
                {
                    Name = FnSendKeyValueJson,
                    Description = "Builds {\"<Key>\": \"<Value>\"} and sends it.",
                    InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                    {
                        new CustomListFunctionInputParameterDefinition { Name = "Key", Description = "The JSON key.", Optional = false, Type = CustomListFunctionParameterTypes.String },
                        new CustomListFunctionInputParameterDefinition { Name = "Value", Description = "The JSON value (sent as a string).", Optional = false, Type = CustomListFunctionParameterTypes.String },
                    },
                    ReturnParameters = SuccessReturn(),
                },
                new CustomListFunctionDefinition { Name = FnClearBuffer, Description = "Empties the table immediately." },
                new CustomListFunctionDefinition { Name = FnReconnect, Description = "Closes the current session and connects again right away." },
                new CustomListFunctionDefinition { Name = FnDisconnect, Description = "Closes the WebSocket and pauses AutoReconnect until Reconnect is called." },
            },
        };
    }

    protected override void CheckDataOverride(CustomListData data)
    {
        var settings = WebSocketSettings.From(data);
        if (settings.ParseTopLevelJson && settings.HasSampleJson && RowBuilder.InferFields(settings.SampleJson) == null)
        {
            throw new InvalidOperationException("SampleJson must be a JSON object (or an array of objects), e.g. {\"machine\": \"CNC-01\", \"temp\": 42.5}.");
        }
    }

    protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
    {
        try
        {
            var settings = WebSocketSettings.From(data);
            if (!settings.ParseTopLevelJson)
            {
                return RowBuilder.EnvelopeColumns();
            }

            var fields = ResolveFields(data.ListName, settings);
            if (fields == null)
            {
                Log?.Warning("WebSocket: no SampleJson and no JSON message within 5 s — only SeqId, Timestamp and ConnectionState are available.");
            }

            return RowBuilder.DynamicColumns(fields ?? Array.Empty<JsonField>());
        }
        catch (Exception ex)
        {
            Log?.Error($"WebSocket GetColumns: {ex.Message}");
            return RowBuilder.EnvelopeColumns();
        }
    }

    protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
    {
        try
        {
            if (data.ListName != null && Sessions.TryGetValue(data.ListName, out var session))
            {
                return ToCollection(session.Snapshot());
            }

            return ToCollection(Probe(WebSocketSettings.From(data)));
        }
        catch (Exception ex)
        {
            Log?.Error($"WebSocket GetItems: {ex.Message}");
            return new CustomListObjectElementCollection();
        }
    }

    protected override void SetupOverride(CustomListData data)
    {
        try
        {
            var listName = data.ListName ?? "";
            var settings = WebSocketSettings.From(data);
            if (Sessions.TryRemove(listName, out var previous))
            {
                previous.Dispose();
            }

            var session = new WebSocketSession(settings, new PushSink(this, listName), Log);
            Sessions[listName] = session;
            session.Start();
        }
        catch (Exception ex)
        {
            Log?.Error($"WebSocket Setup: {ex.Message}");
        }
    }

    protected override void CleanupOverride(CustomListData data)
    {
        try
        {
            if (data?.ListName != null && Sessions.TryRemove(data.ListName, out var session))
            {
                session.Dispose();
            }
        }
        catch (Exception ex)
        {
            Log?.Error($"WebSocket Cleanup: {ex.Message}");
        }
    }

    protected override CustomListExecuteReturnContext ExecuteFunctionOverride(CustomListData data, CustomListExecuteParameterContext context)
    {
        var result = new CustomListExecuteReturnContext();
        try
        {
            var name = context.FunctionName ?? "";
            var session = data.ListName != null && Sessions.TryGetValue(data.ListName, out var s) ? s : null;

            if (name.Equals(FnSendText, StringComparison.OrdinalIgnoreCase))
            {
                result.Add(session != null && session.SendAsync(Arg(context, 0)).Result);
            }
            else if (name.Equals(FnSendKeyValueJson, StringComparison.OrdinalIgnoreCase))
            {
                var json = new JObject { [Arg(context, 0)] = Arg(context, 1) }.ToString(Formatting.None);
                result.Add(session != null && session.SendAsync(json).Result);
            }
            else if (name.Equals(FnClearBuffer, StringComparison.OrdinalIgnoreCase))
            {
                session?.ClearBuffer();
            }
            else if (name.Equals(FnReconnect, StringComparison.OrdinalIgnoreCase))
            {
                Reconnect(data, session);
            }
            else if (name.Equals(FnDisconnect, StringComparison.OrdinalIgnoreCase))
            {
                session?.StopAsync().Wait();
            }
            else
            {
                Log?.Warning($"WebSocket: unknown function '{name}'.");
            }
        }
        catch (Exception ex)
        {
            Log?.Error($"WebSocket {context.FunctionName}: {(ex is AggregateException ae ? ae.GetBaseException().Message : ex.Message)}");
            if (IsSend(context.FunctionName))
            {
                result = new CustomListExecuteReturnContext();
                result.Add(false);
            }
        }

        return result;
    }

    private void Reconnect(CustomListData data, WebSocketSession? session)
    {
        if (session != null)
        {
            session.StopAsync().Wait();
            session.Start();
            return;
        }

        // No live session yet (e.g. called before Setup): create one.
        SetupOverride(data);
    }

    /// <summary>
    /// The JSON columns for the dynamic mode: SampleJson first, then the schema a running session
    /// has seen, then a short live connection waiting for the first JSON message.
    /// </summary>
    private IReadOnlyList<JsonField>? ResolveFields(string? listName, WebSocketSettings settings)
    {
        if (settings.HasSampleJson)
        {
            return RowBuilder.InferFields(settings.SampleJson);
        }

        if (listName != null && Sessions.TryGetValue(listName, out var running) && running.Fields != null)
        {
            return running.Fields;
        }

        using var probe = new WebSocketSession(settings with { AutoReconnect = false }, NullSink.Instance, Log);
        probe.Start();
        var deadline = DateTime.UtcNow + SchemaProbeTimeout;
        while (probe.Fields == null && probe.IsRunning && DateTime.UtcNow < deadline)
        {
            Thread.Sleep(100);
        }

        return probe.Fields;
    }

    /// <summary>Connects briefly and returns what arrived — the Designer's "Load data" preview.</summary>
    private IReadOnlyList<CustomListObjectElement> Probe(WebSocketSettings settings)
    {
        using var probe = new WebSocketSession(settings with { AutoReconnect = false }, NullSink.Instance, Log);
        probe.Start();
        var deadline = DateTime.UtcNow + ProbeDuration;
        while (probe.IsRunning && DateTime.UtcNow < deadline)
        {
            Thread.Sleep(100);
        }

        return probe.Snapshot();
    }

    private static bool IsSend(string? name) =>
        string.Equals(name, FnSendText, StringComparison.OrdinalIgnoreCase)
        || string.Equals(name, FnSendKeyValueJson, StringComparison.OrdinalIgnoreCase);

    private static string Arg(CustomListExecuteParameterContext context, int index) =>
        context.Values.Count > index ? context.Values[index].StringValue ?? "" : "";

    private static CustomListFunctionReturnParameterDefinitionCollection SuccessReturn() =>
        new()
        {
            new CustomListFunctionReturnParameterDefinition { Name = "Success", Description = "true when the message was handed to an open connection.", Type = CustomListFunctionParameterTypes.Boolean },
        };

    private static CustomListObjectElementCollection ToCollection(IEnumerable<CustomListObjectElement> rows)
    {
        var collection = new CustomListObjectElementCollection();
        foreach (var row in rows)
        {
            collection.Add(row);
        }

        return collection;
    }

    /// <summary>Forwards row changes to Peakboard's push channel of one list.</summary>
    private sealed class PushSink : IRowSink
    {
        private readonly WebSocketCustomList _owner;
        private readonly string _listName;

        public PushSink(WebSocketCustomList owner, string listName)
        {
            _owner = owner;
            _listName = listName;
        }

        public void Add(CustomListObjectElement row) => Safe(() => _owner.Data?.Push(_listName).Add(row));

        public void Update(int index, CustomListObjectElement row) => Safe(() => _owner.Data?.Push(_listName).Update(index, row));

        public void Remove(int index) => Safe(() => _owner.Data?.Push(_listName).Remove(index));

        private void Safe(Action push)
        {
            try
            {
                push();
            }
            catch (Exception ex)
            {
                _owner.Log?.Warning($"WebSocket: push to '{_listName}' failed: {ex.Message}");
            }
        }
    }

    /// <summary>Discards row changes — used by the short Designer probes.</summary>
    private sealed class NullSink : IRowSink
    {
        public static readonly NullSink Instance = new();

        public void Add(CustomListObjectElement row) { }

        public void Update(int index, CustomListObjectElement row) { }

        public void Remove(int index) { }
    }
}
