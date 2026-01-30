using IronPython.Hosting;
using Microsoft.Scripting.Hosting;
using Peakboard.ExtensionKit;

namespace PeakboardPython;

public class PythonCustomList : CustomListBase
{
    protected override CustomListDefinition GetDefinitionOverride()
    {
        return new CustomListDefinition
        {
            ID = "PythonCustomList",
            Name = "Python Script",
            Description = "Runs a Python script that produces tabular data",
            PropertyInputPossible = true,
            PropertyInputDefaults =
            {
                new CustomListPropertyDefinition
                {
                    Name = "Script",
                    Value = "result.add_column(\"Column1\", \"string\")\nresult.add_row({\"Column1\": \"Hello\"})",
                    TypeDefinition = TypeDefinition.String.With(multiLine: true)
                },
            },
        };
    }

    protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
    {
        var helper = ExecuteScript(data);

        var columns = new CustomListColumnCollection();
        foreach (var col in helper.Columns)
        {
            columns.Add(new CustomListColumn(col.Name, col.Type));
        }

        return columns;
    }

    protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
    {
        var helper = ExecuteScript(data);

        var items = new CustomListObjectElementCollection();
        foreach (var row in helper.Rows)
        {
            var item = new CustomListObjectElement();
            foreach (var col in helper.Columns)
            {
                if (row.TryGetValue(col.Name, out var value) && value != null)
                {
                    item.Add(col.Name, ConvertValue(value, col.Type));
                }
                else
                {
                    item.Add(col.Name, GetDefaultValue(col.Type));
                }
            }
            items.Add(item);
        }

        return items;
    }

    private PythonResultHelper ExecuteScript(CustomListData data)
    {
        data.Properties.TryGetValue("Script", StringComparison.OrdinalIgnoreCase, out var script);

        if (string.IsNullOrWhiteSpace(script))
        {
            throw new InvalidOperationException("The Python script is empty. Please provide a script.");
        }

        var engine = Python.CreateEngine();
        var scope = engine.CreateScope();

        var helper = new PythonResultHelper();
        scope.SetVariable("result", helper);

        try
        {
            engine.Execute(script, scope);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Python script error: {ex.Message}", ex);
        }

        return helper;
    }

    private static object ConvertValue(object value, CustomListColumnTypes type)
    {
        return type switch
        {
            CustomListColumnTypes.Number => Convert.ToDouble(value),
            CustomListColumnTypes.Boolean => Convert.ToBoolean(value),
            CustomListColumnTypes.String => value.ToString() ?? string.Empty,
            _ => value.ToString() ?? string.Empty
        };
    }

    private static object GetDefaultValue(CustomListColumnTypes type)
    {
        return type switch
        {
            CustomListColumnTypes.Number => 0d,
            CustomListColumnTypes.Boolean => false,
            CustomListColumnTypes.String => string.Empty,
            _ => string.Empty
        };
    }
}
