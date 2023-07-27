using Peakboard.ExtensionKit;
using System;

namespace Peakboard.Extensions.Trello
{
    /// <summary>
    /// Represents a Peakboard Extension for PostgreSQL databases.
    /// </summary>
    [ExtensionIcon("Peakboard.Extensions.Trello.logo.png")]
    public class TrelloExtension : ExtensionBase
    {
        /// <inheritdoc/>
        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "Peakboard.Extensions.Trello",
                Name = "Trello",
                Description = "This is an Extension for accessing Trello boards, lists and cards",
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
                new TrelloCardsCustomList(),
                new TrelloBoardsCustomList(),
            };
        }
    }
}
