using System.Net.WebSockets;
using System.Security.Authentication;
using System.Text;
using Newtonsoft.Json.Linq;
using Peakboard.ExtensionKit;

namespace WebSocketClient;

/// <summary>
/// One live WebSocket connection of one data source: connects, reconnects, receives, sends, and
/// keeps the table rows — locally and, through the push callback, in Peakboard.
/// </summary>
internal sealed class WebSocketSession : IDisposable
{
    public const string StateConnecting = "Connecting";
    public const string StateConnected = "Connected";
    public const string StateDisconnected = "Disconnected";
    public const string StateError = "Error";

    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(10);

    private readonly ILoggingService? _log;
    private readonly IRowSink _sink;
    private readonly object _rowsLock = new();
    private readonly List<CustomListObjectElement> _rows = new();
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    private IReadOnlyList<JsonField>? _fields;
    private ClientWebSocket? _socket;
    private CancellationTokenSource? _loopCts;
    private Task? _loop;
    private long _seq;
    private string _state = StateDisconnected;

    public WebSocketSession(WebSocketSettings settings, IRowSink sink, ILoggingService? log)
    {
        Settings = settings;
        _sink = sink;
        _log = log;

        if (settings.ParseTopLevelJson && settings.HasSampleJson)
        {
            _fields = RowBuilder.InferFields(settings.SampleJson);
        }
    }

    public WebSocketSettings Settings { get; }

    /// <summary>The JSON schema in dynamic mode — from SampleJson or the first JSON message.</summary>
    public IReadOnlyList<JsonField>? Fields => _fields;

    public string State => _state;

    public bool IsRunning => _loop is { IsCompleted: false };

    public IReadOnlyList<CustomListObjectElement> Snapshot()
    {
        lock (_rowsLock)
        {
            return _rows.ToList();
        }
    }

    /// <summary>Starts the connect/receive loop (again). Does nothing while it already runs.</summary>
    public void Start()
    {
        if (IsRunning)
        {
            return;
        }

        _loopCts = new CancellationTokenSource();
        var token = _loopCts.Token;
        _loop = Task.Run(() => RunAsync(token));
    }

    /// <summary>Closes the connection and ends the loop. AutoReconnect stays off until Start.</summary>
    public async Task StopAsync()
    {
        var cts = _loopCts;
        var loop = _loop;
        if (cts == null || loop == null)
        {
            return;
        }

        await CloseSocketAsync().ConfigureAwait(false);
        cts.Cancel();

        try
        {
            await loop.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        }
        catch (TimeoutException)
        {
            _log?.Warning("WebSocket: the receive loop did not end within 5 s after Disconnect.");
        }

        cts.Dispose();
        _loopCts = null;
        _loop = null;
    }

    /// <summary>Sends a text message. Returns false when there is no open connection.</summary>
    public async Task<bool> SendAsync(string text)
    {
        var socket = _socket;
        if (socket == null || socket.State != WebSocketState.Open)
        {
            _log?.Warning("WebSocket: message not sent, the connection is not open.");
            return false;
        }

        await _sendLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var bytes = Encoding.UTF8.GetBytes(text ?? "");
            await socket.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            _sendLock.Release();
        }

        if (Settings.IncludeSentMessages && !Settings.ParseTopLevelJson)
        {
            AddRow(RowBuilder.EnvelopeRow(NextSeq(), RowBuilder.DirectionSent, _state, text ?? ""));
        }

        return true;
    }

    /// <summary>Empties the table, locally and in Peakboard.</summary>
    public void ClearBuffer()
    {
        lock (_rowsLock)
        {
            for (var i = _rows.Count - 1; i >= 0; i--)
            {
                _sink.Remove(0);
            }

            _rows.Clear();
        }
    }

    public void Dispose()
    {
        try
        {
            StopAsync().Wait(TimeSpan.FromSeconds(6));
        }
        catch (Exception ex)
        {
            _log?.Warning($"WebSocket: error while closing: {ex.Message}");
        }

        _sendLock.Dispose();
    }

    private async Task RunAsync(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            SetState(StateConnecting, $"Connecting to {Settings.Url}");
            var failed = false;

            try
            {
                using var socket = CreateSocket();
                _socket = socket;
                await ConnectWithTimeoutAsync(socket, token).ConfigureAwait(false);
                SetState(StateConnected, $"Connected to {Settings.Url}"
                    + (string.IsNullOrEmpty(socket.SubProtocol) ? "" : $" (sub-protocol {socket.SubProtocol})"));

                if (!string.IsNullOrEmpty(Settings.OnConnectPayload))
                {
                    await SendAsync(Settings.OnConnectPayload).ConfigureAwait(false);
                }

                await ReceiveLoopAsync(socket, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // Disconnect or cleanup — not an error.
            }
            catch (Exception ex)
            {
                failed = true;
                _log?.Error($"WebSocket {Settings.Url}: {Describe(ex)}");
                SetState(StateError, Describe(ex));
            }
            finally
            {
                _socket = null;
            }

            if (!failed || token.IsCancellationRequested)
            {
                SetState(StateDisconnected, "Connection closed");
            }

            if (token.IsCancellationRequested || !Settings.AutoReconnect)
            {
                return;
            }

            try
            {
                await Task.Delay(Settings.ReconnectIntervalMs, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    /// <summary>
    /// Connects, but gives up after <see cref="ConnectTimeout"/>: a host that silently drops packets
    /// would otherwise hold a single attempt for the OS TCP timeout (~21 s on Windows).
    /// </summary>
    private async Task ConnectWithTimeoutAsync(ClientWebSocket socket, CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(ConnectTimeout);
        try
        {
            await socket.ConnectAsync(Settings.Url, timeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!token.IsCancellationRequested)
        {
            throw new TimeoutException($"No answer from {Settings.Url} within {ConnectTimeout.TotalSeconds:0} s.");
        }
    }

    private ClientWebSocket CreateSocket()
    {
        var socket = new ClientWebSocket();
        foreach (var header in Settings.Headers)
        {
            socket.Options.SetRequestHeader(header.Key, header.Value);
        }

        foreach (var protocol in Settings.SubProtocols)
        {
            socket.Options.AddSubProtocol(protocol);
        }

        socket.Options.KeepAliveInterval = TimeSpan.FromSeconds(Settings.KeepAliveSeconds);

        if (Settings.IgnoreSslErrors)
        {
            socket.Options.RemoteCertificateValidationCallback = (_, _, _, _) => true;
        }

        return socket;
    }

    private async Task ReceiveLoopAsync(ClientWebSocket socket, CancellationToken token)
    {
        var buffer = new byte[16 * 1024];
        using var message = new MemoryStream();

        while (socket.State == WebSocketState.Open && !token.IsCancellationRequested)
        {
            var result = await socket.ReceiveAsync(buffer, token).ConfigureAwait(false);
            if (result.MessageType == WebSocketMessageType.Close)
            {
                var reason = result.CloseStatus is null
                    ? "Server closed the connection"
                    : $"Server closed the connection: {result.CloseStatus} {result.CloseStatusDescription}".TrimEnd();
                _log?.Info($"WebSocket {Settings.Url}: {reason}");
                if (socket.State == WebSocketState.CloseReceived)
                {
                    await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None).ConfigureAwait(false);
                }

                return;
            }

            message.Write(buffer, 0, result.Count);
            if (!result.EndOfMessage)
            {
                continue;
            }

            // Binary frames are read as UTF-8 too: many servers send JSON as binary.
            var text = Encoding.UTF8.GetString(message.GetBuffer(), 0, (int)message.Length);
            message.SetLength(0);
            OnReceived(text);
        }
    }

    private void OnReceived(string text)
    {
        if (!Settings.ParseTopLevelJson)
        {
            AddRow(RowBuilder.EnvelopeRow(NextSeq(), RowBuilder.DirectionReceived, _state, text));
            return;
        }

        var objects = RowBuilder.ParseObjects(text);
        if (objects.Count == 0)
        {
            _log?.Warning($"WebSocket: message skipped, it is no JSON object/array of objects: {Shorten(text)}");
            return;
        }

        _fields ??= RowBuilder.InferFields(text);
        foreach (var obj in objects)
        {
            AddRow(RowBuilder.DynamicRow(NextSeq(), _state, obj, _fields!));
        }
    }

    /// <summary>
    /// Records a connection state change. In the envelope mode it becomes a SYS row (in SingleRow
    /// only the ConnectionState of the current row changes, so the last payload stays visible); in
    /// the dynamic mode it updates the ConnectionState of row 1 in SingleRow and is only logged in
    /// RingBuffer.
    /// </summary>
    private void SetState(string state, string detail)
    {
        _state = state;
        _log?.Info($"WebSocket {Settings.Url}: {state} — {detail}");

        if (Settings.SingleRow)
        {
            lock (_rowsLock)
            {
                if (_rows.Count > 0)
                {
                    var updated = RowBuilder.WithState(_rows[0], state);
                    _rows[0] = updated;
                    _sink.Update(0, updated);
                    return;
                }
            }
        }

        if (!Settings.ParseTopLevelJson)
        {
            AddRow(RowBuilder.EnvelopeRow(NextSeq(), RowBuilder.DirectionSystem, state, detail));
        }
    }

    private void AddRow(CustomListObjectElement row)
    {
        lock (_rowsLock)
        {
            if (Settings.SingleRow)
            {
                if (_rows.Count == 0)
                {
                    _rows.Add(row);
                }
                else
                {
                    _rows[0] = row;
                }

                _sink.Update(0, row);
                return;
            }

            _rows.Add(row);
            _sink.Add(row);

            while (_rows.Count > Settings.MaxRows)
            {
                _rows.RemoveAt(0);
                _sink.Remove(0);
            }
        }
    }

    private long NextSeq() => Interlocked.Increment(ref _seq);

    private async Task CloseSocketAsync()
    {
        var socket = _socket;
        if (socket == null || socket.State != WebSocketState.Open)
        {
            return;
        }

        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Closed by Peakboard", timeout.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _log?.Warning($"WebSocket: closing handshake failed: {ex.Message}");
        }
    }

    /// <summary>
    /// The message a user can act on: the outer message plus the innermost cause. .NET wraps a
    /// failed TLS handshake as "The SSL connection could not be established, see inner
    /// exception." — the certificate reason is only in the innermost exception, so it is pulled
    /// up, together with the hint to IgnoreSslErrors for a self-signed certificate.
    /// </summary>
    private string Describe(Exception ex)
    {
        var root = ex.GetBaseException();
        var text = root == ex ? ex.Message : $"{ex.Message} ({root.Message})";

        if (root is AuthenticationException && !Settings.IgnoreSslErrors)
        {
            text += ". For a self-signed certificate set IgnoreSslErrors to True.";
        }

        return text;
    }

    private static string Shorten(string text) => text.Length <= 200 ? text : text[..200] + "…";
}

/// <summary>Where a session's row changes go — Peakboard's push channel, or nowhere for a probe.</summary>
internal interface IRowSink
{
    void Add(CustomListObjectElement row);

    void Update(int index, CustomListObjectElement row);

    void Remove(int index);
}
