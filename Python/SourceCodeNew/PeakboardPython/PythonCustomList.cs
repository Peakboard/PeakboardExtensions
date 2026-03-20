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
                    Value = "import json\n\nresult.log_info(\"Fetching products from dummyjson API...\")\n\ntry:\n    response = result.fetch(\"https://dummyjson.com/products?limit=10\")\n    data = json.loads(response)\nexcept Exception as e:\n    result.log_error(\"Failed to fetch products: \" + str(e))\n    raise\n\nresult.add_column(\"Title\", \"string\")\nresult.add_column(\"Price\", \"number\")\nresult.add_column(\"Rating\", \"number\")\nresult.add_column(\"Brand\", \"string\")\nresult.add_column(\"Category\", \"string\")\n\nfor product in data[\"products\"]:\n    result.add_row({\n        \"Title\": product[\"title\"],\n        \"Price\": product[\"price\"],\n        \"Rating\": product[\"rating\"],\n        \"Brand\": str(product.get(\"brand\", \"\")),\n        \"Category\": product[\"category\"]\n    })\n\nresult.log_info(\"Successfully loaded \" + str(len(data[\"products\"])) + \" products\")",
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

        var helper = new PythonResultHelper(Log);
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
