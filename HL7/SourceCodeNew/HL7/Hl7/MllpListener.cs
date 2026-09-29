using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Peakboard.ExtensionKit;

namespace PeakboardExtensionHL7.Hl7
{
    public sealed class ReceivedMessage
    {
        public Hl7Message Message { get; init; }
        public DateTime ReceivedAt { get; init; }
        public string RemoteEndpoint { get; init; }
    }

    /// <summary>
    /// An MLLP (Minimal Lower Layer Protocol) listener: the TCP framing every HL7 v2
    /// interface engine speaks. A frame is 0x0B, the message, then 0x1C 0x0D.
    ///
    /// One listener per port, shared by every list configured for that port, so a
    /// board can show the Segments and Messages lists of one feed side by side.
    /// The listener starts with the first list and stops with the last.
    ///
    /// Every well-formed message is acknowledged with AA, including those no list
    /// keeps: filtering is a display decision, and a NAK would make the sender queue
    /// and retry messages the board simply is not interested in.
    /// </summary>
    public sealed class MllpListener
    {
        private const byte StartBlock = 0x0B;
        private const byte EndBlock = 0x1C;
        private const byte CarriageReturn = 0x0D;
        private const int MaxMessageBytes = 16 * 1024 * 1024;

        private static readonly object RegistryLock = new object();
        private static readonly Dictionary<int, MllpListener> Registry = new Dictionary<int, MllpListener>();

        private readonly Encoding _encoding;
        private readonly ILoggingService _log;
        private TcpListener _listener;
        private CancellationTokenSource _cts;
        private int _refCount;

        public int Port { get; }
        public string EncodingName { get; }
        public bool IsListening { get; private set; }
        public string LastError { get; private set; } = "";

        public event Action<ReceivedMessage> MessageReceived;

        private MllpListener(int port, string encodingName, ILoggingService log)
        {
            Port = port;
            EncodingName = encodingName ?? "UTF-8";
            _encoding = ResolveEncoding(EncodingName);
            _log = log;
        }

        /// <summary>
        /// Gets the listener for a port, starting it if this is the first user.
        /// Dispose the returned lease to release it. The first list on a port decides
        /// its encoding.
        /// </summary>
        public static Lease Acquire(int port, string encodingName, ILoggingService log)
        {
            MllpListener listener;
            lock (RegistryLock)
            {
                if (!Registry.TryGetValue(port, out listener))
                {
                    listener = new MllpListener(port, encodingName, log);
                    Registry[port] = listener;
                }
                else if (!string.Equals(listener.EncodingName, encodingName ?? "UTF-8", StringComparison.OrdinalIgnoreCase))
                {
                    log?.Warning($"[HL7] Port {port} is already open with encoding {listener.EncodingName}; " +
                                 $"the {encodingName} setting of this list is ignored.");
                }
                listener._refCount++;
                if (listener._refCount == 1) listener.Start();
            }
            return new Lease(listener);
        }

        private void Release()
        {
            lock (RegistryLock)
            {
                if (--_refCount > 0) return;
                Registry.Remove(Port);
                Stop();
            }
        }

        private void Start()
        {
            _cts = new CancellationTokenSource();
            try
            {
                _listener = new TcpListener(IPAddress.Any, Port);
                _listener.Start();
                IsListening = true;
                LastError = "";
                _log?.Info($"[HL7] Listening for MLLP on port {Port} ({EncodingName}).");
                _ = Task.Run(() => AcceptLoop(_cts.Token));
            }
            catch (Exception ex)
            {
                // Not thrown here: every list sharing the port reports it from
                // IsListening and LastError, and releases its lease.
                IsListening = false;
                LastError = $"Cannot listen on port {Port}: {ex.Message}";
                _log?.Error($"[HL7] {LastError}");
            }
        }

        private void Stop()
        {
            try { _cts?.Cancel(); } catch { }
            try { _listener?.Stop(); } catch { }
            _listener = null;
            IsListening = false;
            _log?.Info($"[HL7] Stopped listening on port {Port}.");
        }

        private async Task AcceptLoop(CancellationToken ct)
        {
            var listener = _listener;
            while (!ct.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await listener.AcceptTcpClientAsync(ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { break; }
                catch (ObjectDisposedException) { break; }
                catch (SocketException ex)
                {
                    if (ct.IsCancellationRequested) break;
                    SetError($"Accept failed: {ex.Message}");
                    continue;
                }

                _ = Task.Run(() => HandleClient(client, ct));
            }
        }

        private async Task HandleClient(TcpClient client, CancellationToken ct)
        {
            var remote = client.Client.RemoteEndPoint?.ToString() ?? "";
            _log?.Verbose($"[HL7] Connection from {remote}.");

            try
            {
                using (client)
                using (var stream = client.GetStream())
                {
                    var frame = new MemoryStream();
                    var buffer = new byte[8192];
                    bool inFrame = false;

                    while (!ct.IsCancellationRequested)
                    {
                        int read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), ct).ConfigureAwait(false);
                        if (read == 0) break;

                        for (int i = 0; i < read; i++)
                        {
                            byte b = buffer[i];
                            if (b == StartBlock)
                            {
                                // A new start block discards any unterminated frame.
                                frame.SetLength(0);
                                inFrame = true;
                            }
                            else if (b == EndBlock && inFrame)
                            {
                                inFrame = false;
                                var text = _encoding.GetString(frame.GetBuffer(), 0, (int)frame.Length);
                                frame.SetLength(0);
                                var ack = Process(text, remote);
                                if (ack != null)
                                {
                                    var bytes = Frame(ack);
                                    await stream.WriteAsync(bytes, ct).ConfigureAwait(false);
                                }
                            }
                            else if (inFrame)
                            {
                                if (frame.Length >= MaxMessageBytes)
                                {
                                    SetError($"Message from {remote} exceeded {MaxMessageBytes / (1024 * 1024)} MB and was dropped.");
                                    frame.SetLength(0);
                                    inFrame = false;
                                    continue;
                                }
                                frame.WriteByte(b);
                            }
                            // Bytes outside a frame (the trailing 0x0D, keep-alive noise) are ignored.
                        }
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (IOException) { }
            catch (Exception ex)
            {
                SetError($"Connection {remote} failed: {ex.Message}");
            }
            finally
            {
                _log?.Verbose($"[HL7] Connection from {remote} closed.");
            }
        }

        /// <summary>Parses, dispatches and returns the ACK to send (null for nothing).</summary>
        private string Process(string text, string remote)
        {
            Hl7Message message;
            try
            {
                message = Hl7Message.Parse(text);
            }
            catch (FormatException ex)
            {
                SetError($"Unparseable message from {remote}: {ex.Message}");
                return Hl7Ack.BuildReject(ex.Message);
            }

            var received = new ReceivedMessage { Message = message, ReceivedAt = DateTime.Now, RemoteEndpoint = remote };
            var handlers = MessageReceived;
            if (handlers != null)
            {
                foreach (Action<ReceivedMessage> h in handlers.GetInvocationList())
                {
                    try { h(received); }
                    catch (Exception ex) { _log?.Error($"[HL7] A list failed to process {message.ControlId}: {ex}"); }
                }
            }

            // Never acknowledge an acknowledgement: an ACK storm between two
            // receivers is the classic misconfiguration.
            if (message.MessageCode.Equals("ACK", StringComparison.OrdinalIgnoreCase)) return null;
            return Hl7Ack.BuildAccept(message);
        }

        private byte[] Frame(string text)
        {
            var body = _encoding.GetBytes(text);
            var bytes = new byte[body.Length + 3];
            bytes[0] = StartBlock;
            Buffer.BlockCopy(body, 0, bytes, 1, body.Length);
            bytes[^2] = EndBlock;
            bytes[^1] = CarriageReturn;
            return bytes;
        }

        private void SetError(string error)
        {
            LastError = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {error}";
            _log?.Error($"[HL7] {error}");
        }

        public static Encoding ResolveEncoding(string name)
        {
            switch ((name ?? "").Trim().ToUpperInvariant())
            {
                case "ISO-8859-1":
                case "LATIN1":
                    return Encoding.Latin1;
                case "ASCII":
                case "US-ASCII":
                    return Encoding.ASCII;
                default:
                    return new UTF8Encoding(false);
            }
        }

        public sealed class Lease : IDisposable
        {
            private MllpListener _listener;
            public MllpListener Listener => _listener;
            internal Lease(MllpListener listener) { _listener = listener; }

            public void Dispose()
            {
                var l = Interlocked.Exchange(ref _listener, null);
                l?.Release();
            }
        }
    }
}
