using Peakboard.ExtensionKit;
using System;

namespace Peakboard.Extensions.Npgsql
{
    /// <summary>
    /// Represents a Peakboard Extension for PervasiveSQL databases.
    /// </summary>
    public class NpgsqlExtension : ExtensionBase
    {
        /// <inheritdoc/>
        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "Peakboard.Extensions.Npgsql",
                Name = "PervasiveSQL",
                Description = "This is an Extension for accessing a PervasiveSQL Database.",
                Version = "1.0",
                Author = "Peakboard Team",
                Company = "Peakboard GmbH",
                Copyright = "Copyright © 2023"
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
