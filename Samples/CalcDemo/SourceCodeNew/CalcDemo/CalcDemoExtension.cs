using Peakboard.ExtensionKit;

namespace CalcDemo;

public class CalcDemoExtension : ExtensionBase
{
    protected override ExtensionDefinition GetDefinitionOverride()
    {
        return new ExtensionDefinition
        {
            ID = "CalcDemo",
            Name = "Calculation demo",
            Description = "This is a sample implementation for input and output parameters",
            Version = "1.0",
            Author = "Thilo Brosinsky",
            Company = "Peakboard GmbH",
            Copyright = "Peakboard GmbH",
        };
    }

    protected override CustomListCollection GetCustomListsOverride()
    {
        return
        [
            new CalcDemoCustomList(),
        ];
    }
}