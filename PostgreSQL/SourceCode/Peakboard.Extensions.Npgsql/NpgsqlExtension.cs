using Peakboard.ExtensionKit;
using System;

namespace Peakboard.Extensions.Npgsql
{
    /// <summary>
    /// Represents a Peakboard Extension for PostgreSQL databases.
    /// </summary>
    [ExtensionIcon("Peakboard.Extensions.Npgsql.elephant64.png")]
    public class NpgsqlExtension : ExtensionBase
    {
        /// <inheritdoc/>
        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "Peakboard.Extensions.Npgsql",
                Name = "PostgreSQL",
                Description = "This is an Extension for accessing a PostgreSQL Database.",
                Version = "1.0",
                Author = "Peakboard Team",
                Company = "Peakboard GmbH",
                Copyright = "Copyright © 2022"
            };
        }

        /// <inheritdoc/>
        protected override CustomListCollection GetCustomListsOverride()
        {
            return new CustomListCollection
            {
                new NpgsqlCustomList()
            };
        }
    }
}
