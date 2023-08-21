using Peakboard.ExtensionKit;
using System;

namespace Peakboard.Extensions.TableauTokenGenerator
{
    [ExtensionIcon("Peakboard.Extensions.TableauTokenGenerator.logo.png")]
    public class TableauTokenGenerator : ExtensionBase
    {
        /// <inheritdoc/>
        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "Peakboard.Extensions.TableauTokenGenerator",
                Name = "TableauTokenGenerator",
                Description = "This is an Extension for accessing Tableau Tokens",
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
                new TableauTokenGeneratorCustomList()
            };
        }
    }
}
