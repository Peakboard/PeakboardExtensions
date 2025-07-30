using Peakboard.ExtensionKit;

namespace CalcDemo;

class CalcDemoCustomList : CustomListBase
{
    protected override CustomListDefinition GetDefinitionOverride()
    {
        return new CustomListDefinition
        {
            ID = $"CalcDemoList",
            Name = "Calculation demo",
            PropertyInputPossible = false,
            Functions =
            {
                new CustomListFunctionDefinition()
                {
                    Name = "calc",
                    InputParameters = new CustomListFunctionInputParameterDefinitionCollection
                    {
                        new CustomListFunctionInputParameterDefinition
                        {
                            Name = "var a",
                            Description = "var a desc",
                            Optional = false,
                            Type = CustomListFunctionParameterTypes.Number
                        },
                        new CustomListFunctionInputParameterDefinition
                        {
                            Name = "var b",
                            Description = "var b desc",
                            Optional = false,
                            Type = CustomListFunctionParameterTypes.Number
                        },
                        new CustomListFunctionInputParameterDefinition
                        {
                            Name = "op",
                            Description = "op desc",
                            Optional = false,
                            Type = CustomListFunctionParameterTypes.String
                        }
                    },
                    ReturnParameters = new CustomListFunctionReturnParameterDefinitionCollection
                    {
                        new CustomListFunctionReturnParameterDefinition
                        {
                            Name = "result",
                            Description = "result desc",
                            Type = CustomListFunctionParameterTypes.Number
                        }
                    }
                }
            }
        };
    }

    protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
    {
        return
        [
            new CustomListColumn("Dummy", CustomListColumnTypes.Boolean)
        ];
    }

    protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
    {
        var items = new CustomListObjectElementCollection();
        items.Add(new CustomListObjectElement { { "Dummy", true } });
        return items;
    }

    protected override CustomListExecuteReturnContext ExecuteFunctionOverride(CustomListData data, CustomListExecuteParameterContext context)
    {
        var varA = double.Parse(context.Values[0].StringValue);
        var varB = double.Parse(context.Values[1].StringValue);
        var op = context.Values[2].StringValue;
        var ret = new CustomListExecuteReturnContext();

        switch (op)
        {
            case "+":
                ret.Add(varA + varB);
                break;
            case "-":
                ret.Add(varA - varB);
                break;
            case "*":
                ret.Add(varA * varB);
                break;
            case "/":
                ret.Add(varA / varB);
                break;
        }

        return ret;
    }
}