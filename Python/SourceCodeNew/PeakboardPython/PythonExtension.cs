using Peakboard.ExtensionKit;

namespace PeakboardPython;

public class PythonExtension : ExtensionBase
{
    public PythonExtension(IExtensionHost host) : base(host)
    {
    }

    protected override ExtensionDefinition GetDefinitionOverride()
    {
        return new ExtensionDefinition
        {
            ID = "PeakboardPython",
            Name = "Python Extension",
            Description = "This is an Extension for running Python scripts that produce tabular data",
            Version = "1.0",
            Author = "Peakboard Team",
            Company = "Peakboard GmbH",
            Copyright = "Copyright © 2025",
        };
    }

    protected override CustomListCollection GetCustomListsOverride()
    {
        return new CustomListCollection
        {
            new PythonCustomList(),
        };
    }
}
