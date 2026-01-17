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
                var entityNodes = doc.GetElementsByTagName("entity");
                foreach (XmlElement node in entityNodes)
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

            var fetchResults = serviceClient.RetrieveMultiple(new FetchExpression(data.Properties["FetchXML"]));
            
            if (fetchResults.Entities.Count == 0)
            {
                throw new InvalidOperationException("No records found in the specified query.");
            }   

            var columns = new CustomListColumnCollection();
            
            // Extract entity names from FetchXML
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
            
            // Extract columns from the first result
            if (fetchResults.Entities.Count > 0)
            {
                var firstEntity = fetchResults.Entities[0];
                foreach (var attributeName in firstEntity.Attributes.Keys)
                {
                    var attributeTypeCode = attributeTypeMap.ContainsKey(attributeName) 
                        ? attributeTypeMap[attributeName] 
                        : AttributeTypeCode.String;

                    if (attributeTypeCode == AttributeTypeCode.Boolean)
                    {
                        columns.Add(new CustomListColumn(attributeName, CustomListColumnTypes.Boolean));
                    }
                    else if (attributeTypeCode == AttributeTypeCode.Double || 
                             attributeTypeCode == AttributeTypeCode.Decimal || 
                             attributeTypeCode == AttributeTypeCode.Integer || 
                             attributeTypeCode == AttributeTypeCode.BigInt)
                    {
                        columns.Add(new CustomListColumn(attributeName, CustomListColumnTypes.Number));
                    }
                    else
                    {
                        columns.Add(new CustomListColumn(attributeName, CustomListColumnTypes.String));
                    }
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
            
            // Extract entity names from FetchXML
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
            
            foreach(var entity in fetchResults.Entities)
            {
                var item = new CustomListObjectElement();
                foreach (var attribute in entity.Attributes)
                {
                    var columnName = attribute.Key;
                    var value = attribute.Value;
                    var attributeTypeCode = attributeTypeMap.ContainsKey(columnName) ? attributeTypeMap[columnName] : AttributeTypeCode.String;
                    
                    if (value == null)
                    {
                        // Handle null values
                        if (attributeTypeCode == AttributeTypeCode.Boolean)
                        {
                            item.Add(columnName, false);
                        }
                        else if (attributeTypeCode == AttributeTypeCode.Double || 
                                 attributeTypeCode == AttributeTypeCode.Decimal || 
                                 attributeTypeCode == AttributeTypeCode.Integer || 
                                 attributeTypeCode == AttributeTypeCode.BigInt)
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
                            var attributeMetadata = attributeMetadataMap.ContainsKey(columnName) ? attributeMetadataMap[columnName] : null;
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
                        else if (attributeTypeCode == AttributeTypeCode.Boolean)
                        {
                            item.Add(columnName, value);
                        }
                        else if (attributeTypeCode == AttributeTypeCode.Double || 
                                 attributeTypeCode == AttributeTypeCode.Decimal || 
                                 attributeTypeCode == AttributeTypeCode.Integer || 
                                 attributeTypeCode == AttributeTypeCode.BigInt)
                        {
                            item.Add(columnName, Convert.ToDouble(value));
                        }
                        else
                        {
                            item.Add(columnName, value.ToString());
                        }
                    }
                }
                items.Add(item);
            }
            return items;
        }
    }
}
