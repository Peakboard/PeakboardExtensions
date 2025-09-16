using System;
using Peakboard.ExtensionKit;


namespace PeakboardExtensionLDAP
{
    public class LDAPExtension : ExtensionBase
    {
        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "LDAP",
                Name = "LDAP Extension",
                Description = "LDAP user data",
                Version = "1.0",
                Author = "Peakboard",
                Company = "Peakboard",
                Copyright = "Copyright © Peakboard GmbH",
            };
        }

        protected override CustomListCollection GetCustomListsOverride()
        {
            return new CustomListCollection
            {
                new LDAPCustomList(),
            };
        }
    }
}
