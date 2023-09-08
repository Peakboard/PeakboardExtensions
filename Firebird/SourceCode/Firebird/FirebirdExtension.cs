using Peakboard.ExtensionKit;
using System;

namespace Firebird
{
    /// <summary>
    /// Represents a Peakboard Extension for Firebird databases.
    /// </summary>
    [ExtensionIcon("Firebird.Firebird.png")]
    public class FirebirdExtension : ExtensionBase
    {
        /// <inheritdoc/>
        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "Firebird",
                Name = "Firebird",
                Description = "This is an Extension for accessing a Firebird Database.",
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
                new FirebirdCustomList()
            };
        }
    }
}