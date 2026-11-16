using Peakboard.ExtensionKit;

namespace WebSocketClient;

/// <summary>
/// The configuration of one WebSocket data source, read from the properties the user filled in.
/// </summary>
internal sealed record WebSocketSettings(
    Uri Url,
    IReadOnlyList<KeyValuePair<string, string>> Headers,
    IReadOnlyList<string> SubProtocols,
    bool IgnoreSslErrors,
    bool SingleRow,
    int MaxRows,
    bool ParseTopLevelJson,
    string SampleJson,
    bool IncludeSentMessages,
    string OnConnectPayload,
    bool AutoReconnect,
    int ReconnectIntervalMs,
    int KeepAliveSeconds)
{
    public const string BufferModeRingBuffer = "RingBuffer";
    public const string BufferModeSingleRow = "SingleRow";

    /// <summary>
    /// Reads and validates the properties. Throws <see cref="InvalidOperationException"/> with a
    /// message meant for the Designer user when a value is not usable.
    /// </summary>
    public static WebSocketSettings From(CustomListData data)
    {
        var url = Get(data, "WsUrl").Trim();
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != "ws" && uri.Scheme != "wss"))
        {
            throw new InvalidOperationException($"WsUrl must be a complete ws:// or wss:// address, got '{url}'.");
        }

        var bufferMode = Get(data, "BufferMode").Trim();
        if (bufferMode.Length == 0)
        {
            bufferMode = BufferModeRingBuffer;
        }

        if (!bufferMode.Equals(BufferModeRingBuffer, StringComparison.OrdinalIgnoreCase)
            && !bufferMode.Equals(BufferModeSingleRow, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"BufferMode must be '{BufferModeRingBuffer}' or '{BufferModeSingleRow}', got '{bufferMode}'.");
        }

        return new WebSocketSettings(
            uri,
            ParseHeaders(Get(data, "CustomHeaders")),
            Get(data, "SubProtocol").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            GetBool(data, "IgnoreSslErrors", false),
            bufferMode.Equals(BufferModeSingleRow, StringComparison.OrdinalIgnoreCase),
            GetInt(data, "MaxRows", 100, 1, 100000),
            GetBool(data, "ParseTopLevelJson", false),
            Get(data, "SampleJson").Trim(),
            GetBool(data, "IncludeSentMessages", false),
            Get(data, "OnConnectPayload"),
            GetBool(data, "AutoReconnect", true),
            GetInt(data, "ReconnectIntervalMs", 5000, 100, 3600000),
            GetInt(data, "KeepAliveSeconds", 30, 0, 3600));
    }

    /// <summary>
    /// True when the user gave a real sample. The default "{}" (and an empty field) means "derive
    /// the columns from the first live message instead".
    /// </summary>
    public bool HasSampleJson => SampleJson.Length > 0 && SampleJson.Replace(" ", "") != "{}";

    private static string Get(CustomListData data, string name)
    {
        return data.Properties.TryGetValue(name, StringComparison.OrdinalIgnoreCase, out var value) && value != null
            ? value
            : "";
    }

    private static bool GetBool(CustomListData data, string name, bool fallback)
    {
        var raw = Get(data, name).Trim();
        if (raw.Length == 0)
        {
            return fallback;
        }

        if (bool.TryParse(raw, out var value))
        {
            return value;
        }

        throw new InvalidOperationException($"{name} must be true or false, got '{raw}'.");
    }

    private static int GetInt(CustomListData data, string name, int fallback, int min, int max)
    {
        var raw = Get(data, name).Trim();
        if (raw.Length == 0)
        {
            return fallback;
        }

        if (!int.TryParse(raw, out var value) || value < min || value > max)
        {
            throw new InvalidOperationException($"{name} must be a whole number between {min} and {max}, got '{raw}'.");
        }

        return value;
    }

    /// <summary>
    /// Parses "Key1:Val1;Key2:Val2". Only the first colon separates key and value, so a value such
    /// as "Bearer a:b" or a URL survives.
    /// </summary>
    private static IReadOnlyList<KeyValuePair<string, string>> ParseHeaders(string raw)
    {
        var result = new List<KeyValuePair<string, string>>();
        foreach (var part in raw.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var colon = part.IndexOf(':');
            if (colon <= 0)
            {
                throw new InvalidOperationException($"CustomHeaders entry '{part}' is not in the form Key:Value.");
            }

            result.Add(new KeyValuePair<string, string>(part[..colon].Trim(), part[(colon + 1)..].Trim()));
        }

        return result;
    }
}
