﻿using Peakboard.ExtensionKit;

namespace MQTTServer;

public class MQTTServerExtension : ExtensionBase
{
    public MQTTServerExtension() : base() { }

    public MQTTServerExtension(IExtensionHost host) : base(host) { }

    protected override ExtensionDefinition GetDefinitionOverride()
    {
        return new ExtensionDefinition
        {
            ID = "MQTTServer",
            Name = "MQTTServer",
            Description = "This extension provides information about MQTTServer.",
            Version = "1.0",
        };
    }

    protected override CustomListCollection GetCustomListsOverride()
    {
        return
        [
            new MQTTServerCustomList()
        ];
    }
}