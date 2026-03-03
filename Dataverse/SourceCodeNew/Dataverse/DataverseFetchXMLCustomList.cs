using System;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using Peakboard.ExtensionKit;
using Newtonsoft.Json.Linq;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Sdk.Metadata;
using System.Xml;

namespace Dataverse
{
    [Serializable]
    [CustomListIcon("Dataverse.Dataverse.png")]

    class DataverseFetchXMLCustomList : CustomListBase
    {
        private HashSet<string> ExtractEntityNamesFromFetchXML(string fetchXml)
        {
            var entityNames = new HashSet<string>();
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(fetchXml);

                // Extract main entity names
                var entityNodes = doc.GetElementsByTagName("entity");
                foreach (XmlElement node in entityNodes)
                {
                    var entityName = node.GetAttribute("name");
                    if (!string.IsNullOrWhiteSpace(entityName))
                    {
                        entityNames.Add(entityName);
                    }
                }

                // Extract linked entity names
                var linkEntityNodes = doc.GetElementsByTagName("link-entity");
                foreach (XmlElement node in linkEntityNodes)
                {
                    var entityName = node.GetAttribute("name");
                    if (!string.IsNullOrWhiteSpace(entityName))
                    {
                        entityNames.Add(entityName);
                    }
                }
            }
            catch
            {
                // If parsing fails, return empty set
            }
            return entityNames;
        }

        private List<string> ExtractAttributeNamesFromFetchXML(string fetchXml)
        {
            var attributeNames = new List<string>();
            try
            {
                var doc = new XmlDocument();
                doc.LoadXml(fetchXml);

                // Extract attribute names from the main entity
                var entityNodes = doc.GetElementsByTagName("entity");
                foreach (XmlElement entityNode in entityNodes)
                {
                    foreach (XmlNode child in entityNode.ChildNodes)
                    {
                        if (child is XmlElement element && element.LocalName == "attribute")
                        {
                            var attrName = element.GetAttribute("name");
                            if (!string.IsNullOrWhiteSpace(attrName) && !attributeNames.Contains(attrName))
                            {
                                attributeNames.Add(attrName);
                            }
                        }
                    }
                }

                // Extract attribute names from linked entities (with alias prefix)
                var linkEntityNodes = doc.GetElementsByTagName("link-entity");
                foreach (XmlElement linkNode in linkEntityNodes)
                {
                    var alias = linkNode.GetAttribute("alias");
                    foreach (XmlNode child in linkNode.ChildNodes)
                    {
                        if (child is XmlElement element && element.LocalName == "attribute")
                        {
                            var attrName = element.GetAttribute("name");
                            if (!string.IsNullOrWhiteSpace(attrName))
                            {
                                var fullName = !string.IsNullOrWhiteSpace(alias) ? $"{alias}.{attrName}" : attrName;
                                if (!attributeNames.Contains(fullName))
                                {
                                    attributeNames.Add(fullName);
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // If parsing fails, return empty list
            }
            return attributeNames;
        }

        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "DataverseFetchXML",
                Name = "Dataverse FetchXML",
                Description = "Fetches data from Dataverse FetchXML query.",
                PropertyInputPossible = true,
                PropertyInputDefaults = {
                new CustomListPropertyDefinition() { Name = "DataverseURL", Value = "https://xxx.crm4.dynamics.com/" },
                new CustomListPropertyDefinition() { Name = "ClientId", Value = "" },
                new CustomListPropertyDefinition() { Name = "ClientSecret", TypeDefinition = TypeDefinition.String.With(masked: true) },
                new CustomListPropertyDefinition() { Name = "TenantId", Value=""  },
                new CustomListPropertyDefinition() { Name = "FetchXML", TypeDefinition = TypeDefinition.String.With(multiLine: true ), Value="<fetch top='5'>\r\n<entity name=\"account\">\r\n    <attribute name=\"name\" />\r\n    <attribute name=\"address1_city\" />\r\n    <attribute name=\"accountid\" />\r\n    <attribute name=\"createdon\" />\r\n    <attribute name=\"customertypecode\" />\r\n    <order attribute=\"createdon\" descending=\"true\" />\r\n<attribute name=\"address1_city\" />\r\n   <attribute name=\"accountid\" />\r\n    <attribute name=\"createdon\" />\r\n    <attribute name=\"customertypecode\" />\r\n    <order attribute=\"createdon\" descending=\"true\" />\r\n  </entity>\r\n</fetch>" }
                }
            };
        }

        protected override void CheckDataOverride(CustomListData data)
        {
            if (string.IsNullOrWhiteSpace(data.Properties["DataverseURL"]))
            {
                throw new InvalidOperationException("Invalid DataverseURL");
            }
            if (!data.Properties["DataverseURL"].EndsWith($"/"))
            {
                throw new InvalidOperationException("BaseURL must end with /");
            }
            if (string.IsNullOrWhiteSpace(data.Properties["ClientId"]))
            {
                throw new InvalidOperationException("Invalid ClientId");
            }
            if (string.IsNullOrWhiteSpace(data.Properties["ClientSecret"]))
            {
                throw new InvalidOperationException("Invalid ClientSecret");
            }
            if (string.IsNullOrWhiteSpace(data.Properties["TenantId"]))
            {
                throw new InvalidOperationException("Invalid TenantId");
            }
            if (string.IsNullOrWhiteSpace(data.Properties["FetchXML"]))
            {
                throw new InvalidOperationException("Invalid FetchXML");
            }
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            var serviceClient = DataverseExtension.GetConnection(data);

            var columns = new CustomListColumnCollection();

            // Extract attribute names and entity names from FetchXML
            var requestedAttributes = ExtractAttributeNamesFromFetchXML(data.Properties["FetchXML"]);
            var entityNames = ExtractEntityNamesFromFetchXML(data.Properties["FetchXML"]);

            // Build metadata map for all involved entities
            var attributeTypeMap = new Dictionary<string, AttributeTypeCode>();
            foreach (var entityName in entityNames)
            {
                try
                {
                    var retrieveEntityRequest = new RetrieveEntityRequest { LogicalName = entityName, EntityFilters = EntityFilters.Attributes };
                    var retrieveEntityResponse = (RetrieveEntityResponse)serviceClient.Execute(retrieveEntityRequest);
                    var entityMetadata = retrieveEntityResponse.EntityMetadata;

                    foreach (var attribute in entityMetadata.Attributes)
                    {
                        attributeTypeMap[attribute.LogicalName] = attribute.AttributeType ?? AttributeTypeCode.String;
                    }
                }
                catch
                {
                    // If metadata retrieval fails for an entity, continue
                }
            }

            // Create columns based on the FetchXML attribute list (not the first row's data)
            foreach (var attributeName in requestedAttributes)
            {
                // For linked entity attributes (alias.attributeName), look up the base attribute name
                var lookupKey = attributeName.Contains(".") ? attributeName.Split('.')[1] : attributeName;

                var attributeTypeCode = attributeTypeMap.ContainsKey(lookupKey)
                    ? attributeTypeMap[lookupKey]
                    : AttributeTypeCode.String;

                if (attributeTypeCode == AttributeTypeCode.Boolean)
                {
                    columns.Add(new CustomListColumn(attributeName, CustomListColumnTypes.Boolean));
                }
                else if (attributeTypeCode == AttributeTypeCode.Double ||
                         attributeTypeCode == AttributeTypeCode.Decimal ||
                         attributeTypeCode == AttributeTypeCode.Integer ||
                         attributeTypeCode == AttributeTypeCode.BigInt ||
                         attributeTypeCode == AttributeTypeCode.Money)
                {
                    columns.Add(new CustomListColumn(attributeName, CustomListColumnTypes.Number));
                }
                else
                {
                    columns.Add(new CustomListColumn(attributeName, CustomListColumnTypes.String));
                }
            }

            return columns;
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            var serviceClient = DataverseExtension.GetConnection(data);
            this.Log.Info($"Connected to Dataverse Organization: {serviceClient.ConnectedOrgFriendlyName}");

            var fetchResults = serviceClient.RetrieveMultiple(new FetchExpression(data.Properties["FetchXML"]));

            if (fetchResults.Entities.Count == 0)
            {
                throw new InvalidOperationException("No records found in the specified query.");
            }

            var items = new CustomListObjectElementCollection();

            // Extract attribute names and entity names from FetchXML
            var requestedAttributes = ExtractAttributeNamesFromFetchXML(data.Properties["FetchXML"]);
            var entityNames = ExtractEntityNamesFromFetchXML(data.Properties["FetchXML"]);

            // Build metadata maps for all involved entities
            var attributeTypeMap = new Dictionary<string, AttributeTypeCode>();
            var attributeMetadataMap = new Dictionary<string, AttributeMetadata>();

            foreach (var entityName in entityNames)
            {
                try
                {
                    var retrieveEntityRequest = new RetrieveEntityRequest { LogicalName = entityName, EntityFilters = EntityFilters.Attributes };
                    var retrieveEntityResponse = (RetrieveEntityResponse)serviceClient.Execute(retrieveEntityRequest);
                    var entityMetadata = retrieveEntityResponse.EntityMetadata;

                    foreach (var attribute in entityMetadata.Attributes)
                    {
                        attributeTypeMap[attribute.LogicalName] = attribute.AttributeType ?? AttributeTypeCode.String;
                        attributeMetadataMap[attribute.LogicalName] = attribute;
                    }
                }
                catch
                {
                    // If metadata retrieval fails for an entity, continue
                }
            }

            foreach (var entity in fetchResults.Entities)
            {
                var item = new CustomListObjectElement();

                // Iterate over the expected columns from FetchXML, not the entity's actual attributes
                foreach (var columnName in requestedAttributes)
                {
                    // For linked entity attributes (alias.attributeName), look up the base attribute name
                    var lookupKey = columnName.Contains(".") ? columnName.Split('.')[1] : columnName;
                    var attributeTypeCode = attributeTypeMap.ContainsKey(lookupKey) ? attributeTypeMap[lookupKey] : AttributeTypeCode.String;

                    // Check if the entity has this attribute
                    if (!entity.Attributes.ContainsKey(columnName))
                    {
                        // Attribute is missing (null in Dataverse) - add a default value
                        if (attributeTypeCode == AttributeTypeCode.Boolean)
                        {
                            item.Add(columnName, false);
                        }
                        else if (attributeTypeCode == AttributeTypeCode.Double ||
                                 attributeTypeCode == AttributeTypeCode.Decimal ||
                                 attributeTypeCode == AttributeTypeCode.Integer ||
                                 attributeTypeCode == AttributeTypeCode.BigInt ||
                                 attributeTypeCode == AttributeTypeCode.Money)
                        {
                            item.Add(columnName, 0);
                        }
                        else
                        {
                            item.Add(columnName, string.Empty);
                        }
                    }
                    else
                    {
                        var value = entity.Attributes[columnName];

                        // Extract value from AliasedValue (linked entity attributes)
                        if (value is AliasedValue aliasedValue)
                        {
                            value = aliasedValue.Value;
                        }

                        if (value == null)
                        {
                            if (attributeTypeCode == AttributeTypeCode.Boolean)
                            {
                                item.Add(columnName, false);
                            }
                            else if (attributeTypeCode == AttributeTypeCode.Double ||
                                     attributeTypeCode == AttributeTypeCode.Decimal ||
                                     attributeTypeCode == AttributeTypeCode.Integer ||
                                     attributeTypeCode == AttributeTypeCode.BigInt ||
                                     attributeTypeCode == AttributeTypeCode.Money)
                            {
                                item.Add(columnName, 0);
                            }
                            else
                            {
                                item.Add(columnName, string.Empty);
                            }
                        }
                        else
                        {
                            // Handle OptionSetValue by extracting the plain text label
                            if (value is OptionSetValue optionSetValue)
                            {
                                var attributeMetadata = attributeMetadataMap.ContainsKey(lookupKey) ? attributeMetadataMap[lookupKey] : null;
                                if (attributeMetadata is PicklistAttributeMetadata picklistMetadata)
                                {
                                    var option = picklistMetadata.OptionSet.Options
                                        .FirstOrDefault(o => o.Value == optionSetValue.Value);
                                    item.Add(columnName, option?.Label?.UserLocalizedLabel?.Label ?? optionSetValue.Value.ToString());
                                }
                                else
                                {
                                    item.Add(columnName, optionSetValue.Value.ToString());
                                }
                            }
                            // Handle EntityReference by extracting the GUID
                            else if (value is EntityReference entityRef)
                            {
                                item.Add(columnName, entityRef.Id.ToString());
                            }
                            // Handle Money by extracting the numeric value
                            else if (value is Money money)
                            {
                                item.Add(columnName, Convert.ToDouble(money.Value));
                            }
                            else if (attributeTypeCode == AttributeTypeCode.Boolean)
                            {
                                item.Add(columnName, value);
                            }
                            else if (attributeTypeCode == AttributeTypeCode.Double ||
                                     attributeTypeCode == AttributeTypeCode.Decimal ||
                                     attributeTypeCode == AttributeTypeCode.Integer ||
                                     attributeTypeCode == AttributeTypeCode.BigInt ||
                                     attributeTypeCode == AttributeTypeCode.Money)
                            {
                                item.Add(columnName, Convert.ToDouble(value));
                            }
                            else
                            {
                                item.Add(columnName, value.ToString());
                            }
                        }
                    }
                }
                items.Add(item);
            }
            return items;
        }
    }
}
