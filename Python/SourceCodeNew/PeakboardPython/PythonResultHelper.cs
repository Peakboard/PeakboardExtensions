using Peakboard.ExtensionKit;

namespace PeakboardPython;

public class PythonResultHelper
{
    public List<(string Name, CustomListColumnTypes Type)> Columns { get; } = new();
    public List<Dictionary<string, object>> Rows { get; } = new();

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
