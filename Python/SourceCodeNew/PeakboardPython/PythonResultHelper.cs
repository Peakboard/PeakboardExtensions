using System.Net.Http;
using Peakboard.ExtensionKit;

namespace PeakboardPython;

public class PythonResultHelper
{
    private static readonly HttpClient HttpClient = new();
    private readonly ILoggingService _log;

    public PythonResultHelper(ILoggingService log)
    {
        _log = log;
    }

    public List<(string Name, CustomListColumnTypes Type)> Columns { get; } = new();
    public List<Dictionary<string, object>> Rows { get; } = new();

    public string fetch(string url)
    {
        return HttpClient.GetStringAsync(url).Result;
    }

    public void log_info(string message)
    {
        _log?.Info(message);
    }

    public void log_error(string message)
    {
        _log?.Error(message);
    }

    public void add_column(string name, string type)
    {
        var columnType = type.ToLowerInvariant() switch
        {
            "string" => CustomListColumnTypes.String,
            "number" => CustomListColumnTypes.Number,
            "boolean" => CustomListColumnTypes.Boolean,
            _ => throw new ArgumentException($"Unknown column type '{type}'. Use 'string', 'number', or 'boolean'.")
        };

        Columns.Add((name, columnType));
    }

    public void add_row(IDictionary<string, object> values)
    {
        var row = new Dictionary<string, object>();

        foreach (var kvp in values)
        {
            row[kvp.Key] = kvp.Value;
        }

        Rows.Add(row);
    }
}
