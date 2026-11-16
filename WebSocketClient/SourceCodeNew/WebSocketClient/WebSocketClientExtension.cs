using Peakboard.ExtensionKit;

namespace WebSocketClient;

public class WebSocketClientExtension : ExtensionBase
{
    public WebSocketClientExtension() : base() { }

    public WebSocketClientExtension(IExtensionHost host) : base(host) { }

    protected override ExtensionDefinition GetDefinitionOverride()
    {
        return new ExtensionDefinition
        {
            ID = "WebSocketClient",
            Name = "WebSocket Client",
            Description = "Connects to a WebSocket server (ws:// or wss://), shows incoming messages live "
                        + "as a table and sends messages from Lua or Building Blocks.",
            Version = "1.0",
            MinVersion = "1.0",
            Author = "Peakboard",
            Company = "Peakboard GmbH",
            Copyright = "Copyright © Peakboard GmbH",
        };
    }

    protected override CustomListCollection GetCustomListsOverride()
    {
        return new CustomListCollection
        {
            new WebSocketCustomList(),
        };
    }
}
