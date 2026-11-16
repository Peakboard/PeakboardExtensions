using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Peakboard.ExtensionKit;

namespace WebSocketClient;

/// <summary>
/// Knows the columns of both table modes and turns a message into rows that match them.
/// </summary>
internal static class RowBuilder
{
    public const string SeqId = "SeqId";
    public const string Timestamp = "Timestamp";
    public const string Direction = "Direction";
    public const string ConnectionState = "ConnectionState";
    public const string Payload = "Payload";

    public const string DirectionReceived = "RX";
    public const string DirectionSent = "TX";
    public const string DirectionSystem = "SYS";

    private static readonly HashSet<string> FixedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        SeqId, Timestamp, Direction, ConnectionState, Payload,
    };

    /// <summary>Columns of the raw envelope mode (ParseTopLevelJson = false).</summary>
    public static CustomListColumnCollection EnvelopeColumns()
    {
        return new CustomListColumnCollection
        {
            new CustomListColumn(SeqId, CustomListColumnTypes.Number),
            new CustomListColumn(Timestamp, CustomListColumnTypes.String),
            new CustomListColumn(Direction, CustomListColumnTypes.String),
            new CustomListColumn(ConnectionState, CustomListColumnTypes.String),
            new CustomListColumn(Payload, CustomListColumnTypes.String),
        };
    }

    /// <summary>Columns of the dynamic mode: the three fixed ones plus one per JSON key.</summary>
    public static CustomListColumnCollection DynamicColumns(IReadOnlyList<JsonField> fields)
    {
        var columns = new CustomListColumnCollection
        {
            new CustomListColumn(SeqId, CustomListColumnTypes.Number),
            new CustomListColumn(Timestamp, CustomListColumnTypes.String),
            new CustomListColumn(ConnectionState, CustomListColumnTypes.String),
        };

        foreach (var field in fields)
        {
            columns.Add(new CustomListColumn(field.Name, field.Type));
        }

        return columns;
    }

    /// <summary>
    /// Derives the JSON schema from a message: its top-level keys and their types. A JSON array
    /// contributes the keys of its first object. Returns null when the text is no JSON object.
    /// Keys that collide with a fixed column are skipped.
    /// </summary>
    public static IReadOnlyList<JsonField>? InferFields(string text)
    {
        var obj = ParseObjects(text).FirstOrDefault();
        if (obj == null)
        {
            return null;
        }

        return obj.Properties()
            .Where(p => !FixedNames.Contains(p.Name))
            .Select(p => new JsonField(p.Name, TypeOf(p.Value)))
            .ToList();
    }

    /// <summary>
    /// Splits a message into the JSON objects it carries: one for an object, one per object
    /// element for an array, none for anything else.
    /// </summary>
    public static IReadOnlyList<JObject> ParseObjects(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<JObject>();
        }

        JToken token;
        try
        {
            token = JToken.Parse(text);
        }
        catch (JsonException)
        {
            return Array.Empty<JObject>();
        }

        return token switch
        {
            JObject obj => new[] { obj },
            JArray array => array.OfType<JObject>().ToList(),
            _ => Array.Empty<JObject>(),
        };
    }

    public static CustomListObjectElement EnvelopeRow(long seq, string direction, string state, string payload)
    {
        return new CustomListObjectElement
        {
            { SeqId, (double)seq },
            { Timestamp, Now() },
            { Direction, direction },
            { ConnectionState, state },
            { Payload, payload ?? "" },
        };
    }

    public static CustomListObjectElement DynamicRow(long seq, string state, JObject obj, IReadOnlyList<JsonField> fields)
    {
        var row = new CustomListObjectElement
        {
            { SeqId, (double)seq },
            { Timestamp, Now() },
            { ConnectionState, state },
        };

        foreach (var field in fields)
        {
            row.Add(field.Name, Convert(obj.GetValue(field.Name, StringComparison.Ordinal), field.Type));
        }

        return row;
    }

    /// <summary>A copy of a row with another ConnectionState — the rest stays as it was.</summary>
    public static CustomListObjectElement WithState(CustomListObjectElement row, string state)
    {
        var copy = new CustomListObjectElement();
        foreach (var pair in row)
        {
            copy.Add(pair.Key, pair.Key == ConnectionState ? state : pair.Value);
        }

        return copy;
    }

    private static string Now() => DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);

    private static CustomListColumnTypes TypeOf(JToken token)
    {
        return token.Type switch
        {
            JTokenType.Integer or JTokenType.Float => CustomListColumnTypes.Number,
            JTokenType.Boolean => CustomListColumnTypes.Boolean,
            _ => CustomListColumnTypes.String,
        };
    }

    /// <summary>
    /// Converts a JSON value to the declared column type. Never returns a JToken or null — a cell
    /// only takes string, double or bool.
    /// </summary>
    private static object Convert(JToken? token, CustomListColumnTypes type)
    {
        if (token == null || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined)
        {
            return Default(type);
        }

        switch (type)
        {
            case CustomListColumnTypes.Number:
                if (token.Type is JTokenType.Integer or JTokenType.Float)
                {
                    return token.Value<double>();
                }

                return double.TryParse(token.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
                    ? number
                    : 0d;

            case CustomListColumnTypes.Boolean:
                if (token.Type == JTokenType.Boolean)
                {
                    return token.Value<bool>();
                }

                return bool.TryParse(token.ToString(), out var flag) && flag;

            default:
                return token.Type is JTokenType.Object or JTokenType.Array
                    ? token.ToString(Formatting.None)
                    : token.ToString();
        }
    }

    private static object Default(CustomListColumnTypes type)
    {
        return type switch
        {
            CustomListColumnTypes.Number => 0d,
            CustomListColumnTypes.Boolean => false,
            _ => "",
        };
    }
}

/// <summary>A top-level JSON key and the column type it maps to.</summary>
internal sealed record JsonField(string Name, CustomListColumnTypes Type);
