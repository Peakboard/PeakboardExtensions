using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.Net.Http;
using System.Security.Principal;
using System.Threading.Tasks;
using Peakboard.ExtensionKit;

namespace PeakboardExtensionLDAP
{
    [Serializable]
    class LDAPCustomList : CustomListBase
    {
        CustomListColumnCollection columns = new CustomListColumnCollection();

        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = $"LDAPCustomList",
                Name = "LDAP",
                PropertyInputPossible = true,
                PropertyInputDefaults = {
                    new CustomListPropertyDefinition(){ Name = "Server", Value = ""},
                    new CustomListPropertyDefinition(){ Name = "User", Value = ""},
                    new CustomListPropertyDefinition(){ Name = "Password", Value = "", Masked=true},
                    new CustomListPropertyDefinition(){ Name = "Properties", Value = "displayName;mail;telephoneNumber;department;title;objectSid;",  MultiLine = true},
                }
            };
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            var propertiesRaw = data.Properties["Properties"];
            var properties = propertiesRaw.Split(';');
            
            columns.Clear();

            foreach (var property in properties)
            {
                columns.Add(new CustomListColumn(property, CustomListColumnTypes.String));
            }

            return columns;
        }


        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            var items = new CustomListObjectElementCollection();
            var row = new CustomListObjectElement();

            // Currently logged in user name (DOMAIN\username)
            string currentUser = WindowsIdentity.GetCurrent().Name;

            // Extract the SAM account name (username)
            string samAccountName = currentUser.Split('\\')[1];

            DirectorySearcher searcher;

            var user = data.Properties["User"];
            var password = data.Properties["Password"];

            // Search LDAP directory
            if (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(password))
            {
                searcher = new DirectorySearcher(new DirectoryEntry(data.Properties["Server"], user, password))
                {
                    Filter = $"(&(objectClass=user)(samAccountName={samAccountName}))"
                };
            }
            else
            {
                searcher = new DirectorySearcher(new DirectoryEntry(data.Properties["Server"]))
                {
                    Filter = $"(&(objectClass=user)(samAccountName={samAccountName}))"
                };
            }

            foreach (CustomListColumn column in columns)
            {
                searcher.PropertiesToLoad.Add(column.Name);
            }

            // Find the user
            SearchResult result = searcher.FindOne();

            if (result != null)
            {
                var propertiesRaw = data.Properties["Properties"];
                var properties = propertiesRaw.Split(';');

                // Output of all properties
                foreach (string propertyName in properties)
                {
                    if (result.Properties[propertyName].Count == 0 || result.Properties[propertyName][0] == null)
                    {
                        row.Add(propertyName, string.Empty);
                    }
                    else if (result.Properties[propertyName][0] is byte[] byteArray)
                    {
                        try
                        {
                            SecurityIdentifier propData = new SecurityIdentifier(byteArray, 0);
                            row.Add(propertyName, propData.Value);
                        }
                        catch (Exception ex)
                        {
                            row.Add(propertyName, ex.Message);
                        }
                    }
                    else if (result.Properties[propertyName][0] is string stringVal)
                    {
                        row.Add(propertyName, stringVal);
                    }
                    else
                    {
                        row.Add(propertyName, $"{result.Properties[propertyName][0].ToString()}");
                    }
                }
            }
            else
            {
                throw new Exception("User information could not be found.");
            }

            items.Add(row);

            return items;
        }
    }
}
