using Peakboard.ExtensionKit;
using System;

namespace PostgreSQL;

/// <summary>
/// Represents a Peakboard Extension for PostgreSQL databases.
/// </summary>
[ExtensionIcon("PostgreSQL.Elephant64.png")]
public class PostgreSQLExtension : ExtensionBase
{
    /// <inheritdoc/>
    protected override ExtensionDefinition GetDefinitionOverride()
    {
        return new ExtensionDefinition
        {
            ID = "PostgreSQL",
            Name = "PostgreSQL",
            Description = "This is an Extension for accessing a PostgreSQL Database.",
            Version = "1.0",
            Author = "Peakboard Team",
            Company = "Peakboard GmbH",
            Copyright = "Copyright © 2025"
        };
    }

    /// <inheritdoc/>
    protected override CustomListCollection GetCustomListsOverride()
    {
        return
        [
            new PostgreSQLCustomList()
        ];
    }
}