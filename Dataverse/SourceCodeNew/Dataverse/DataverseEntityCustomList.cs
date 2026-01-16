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

namespace Dataverse
{
    [Serializable]
    [CustomListIcon("Dataverse.Dataverse.png")]

    class DataverseEntityCustomList : CustomListBase
    {

protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "DataverseEntities",
                Name = "Dataverse Entities",
                Description = "Fetches data from Dataverse Entities",
                PropertyInputPossible = true,
                PropertyInputDefaults = {
                new CustomListPropertyDefinition() { Name = "DataverseURL", Value = "https://xxx.crm4.dynamics.com/" },
                new CustomListPropertyDefinition() { Name = "ClientId", Value = "" },
                new CustomListPropertyDefinition() { Name = "ClientSecret", TypeDefinition = TypeDefinition.String.With(masked: true) },
                new CustomListPropertyDefinition() { Name = "TenantId", Value=""  },
                new CustomListPropertyDefinition() { Name = "Entity", Value="account"  },
                new CustomListPropertyDefinition() { Name = "Attributes", Value="accountid,name,emailaddress1,telephone1"  },
                new CustomListPropertyDefinition() { Name = "MaxRows", TypeDefinition = TypeDefinition.Number.With(minimum: 1, maximum: 100000), Value = "10"}
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
            if (string.IsNullOrWhiteSpace(data.Properties["Entity"]))
            {
                throw new InvalidOperationException("Invalid Entity");
            }
        }

        public static ServiceClient GetConnection(CustomListData data)
        {
            var dataverseUrl = data.Properties["DataverseURL"];
            var clientId = data.Properties["ClientId"];
            var clientSecret = data.Properties["ClientSecret"];
            var tenantId = data.Properties["TenantId"];

            var connectionString = $@"
                AuthType=ClientSecret;
                Url={dataverseUrl};
                ClientId={clientId};
                ClientSecret={clientSecret};
                Authority=https://login.microsoftonline.com/{tenantId};
                RequireNewInstance=true";

                var serviceClient = new ServiceClient(connectionString);

                if (serviceClient.IsReady)
                {
                    return serviceClient;
                }
                else
                {
                    throw new InvalidOperationException("Could not connect to Dataverse: " );  

                }
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            var serviceClient = GetConnection(data);
            // this.logger.Info($"Connected to Dataverse Organization: {serviceClient.ConnectedOrgFriendlyName}");

            var query = new QueryExpression(data.Properties["Entity"]);
            query.ColumnSet = new ColumnSet(data.Properties["Attributes"].Split(','));
            query.PageInfo = new PagingInfo { PageNumber = 1, Count = 1 };
            var entities = serviceClient.RetrieveMultiple(query).Entities;
            
            if (entities.Count == 0)
            {
                throw new InvalidOperationException("No records found in the specified entity with the given attributes.");
            }   

            var columns = new CustomListColumnCollection();
            
            // Retrieve metadata for the entity to get column type information
            var retrieveEntityRequest = new RetrieveEntityRequest { LogicalName = data.Properties["Entity"], EntityFilters = EntityFilters.Attributes };
            var retrieveEntityResponse = (RetrieveEntityResponse)serviceClient.Execute(retrieveEntityRequest);
            var entityMetadata = retrieveEntityResponse.EntityMetadata;
            
            // Create a mapping of attribute names to their types
            var attributeTypeMap = entityMetadata.Attributes
                .ToDictionary(a => a.LogicalName, a => a.AttributeType ?? AttributeTypeCode.String);
            
            // Loop through all attributes in the entity
            foreach (var columnName in data.Properties["Attributes"].Split(','))
            {
                var attributeTypeCode = attributeTypeMap.ContainsKey(columnName) 
                    ? attributeTypeMap[columnName] 
                    : AttributeTypeCode.String;

                if (attributeTypeCode == AttributeTypeCode.Boolean)
                {
                    columns.Add(new CustomListColumn(columnName, CustomListColumnTypes.Boolean));
                }
                else if (attributeTypeCode == AttributeTypeCode.Double || 
                         attributeTypeCode == AttributeTypeCode.Decimal || 
                         attributeTypeCode == AttributeTypeCode.Integer || 
                         attributeTypeCode == AttributeTypeCode.BigInt)
                {
                    columns.Add(new CustomListColumn(columnName, CustomListColumnTypes.Number));
                }
                else
                {
                    columns.Add(new CustomListColumn(columnName, CustomListColumnTypes.String));
                }
            }

            return columns;
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            var serviceClient = GetConnection(data);
            this.Log.Info($"Connected to Dataverse Organization: {serviceClient.ConnectedOrgFriendlyName}");

            var query = new QueryExpression(data.Properties["Entity"]);
            query.ColumnSet = new ColumnSet(data.Properties["Attributes"].Split(','));
            query.PageInfo = new PagingInfo { PageNumber = 1, Count = data.Properties["TenantId"] != null ? int.Parse(data.Properties["MaxRows"]) : 10  };
            var entities = serviceClient.RetrieveMultiple(query).Entities;
            
            if (entities.Count == 0)
            {
                throw new InvalidOperationException("No records found in the specified entity with the given attributes.");
            }   

            var items = new CustomListObjectElementCollection();
            
            // Retrieve metadata for the entity to get column type information
            var retrieveEntityRequest = new RetrieveEntityRequest { LogicalName = data.Properties["Entity"], EntityFilters = EntityFilters.Attributes };
            var retrieveEntityResponse = (RetrieveEntityResponse)serviceClient.Execute(retrieveEntityRequest);
            var entityMetadata = retrieveEntityResponse.EntityMetadata;
            
            // Create a mapping of attribute names to their types
            var attributeTypeMap = entityMetadata.Attributes
                .ToDictionary(a => a.LogicalName, a => a.AttributeType ?? AttributeTypeCode.String);

            // Create a mapping of attribute names to their metadata for option set lookups
            var attributeMetadataMap = entityMetadata.Attributes
                .ToDictionary(a => a.LogicalName, a => a);

            
            foreach(var entity in entities)
            {
                var item = new CustomListObjectElement();
                foreach (var columnName in data.Properties["Attributes"].Split(','))
                {
                    var attributeTypeCode = attributeTypeMap.ContainsKey(columnName) ? attributeTypeMap[columnName] : AttributeTypeCode.String;
                    if (!entity.Attributes.ContainsKey(columnName))
                    {
                        // Handle missing attributes
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
                        var value = entity.Attributes[columnName];
                        
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

                /*
                foreach (var attribute in entity.Attributes)
                {
                    var columnName = attribute.Key;
                    var value = attribute.Value;
                    var attributeTypeCode = attributeTypeMap.ContainsKey(columnName) 
                        ? attributeTypeMap[columnName] 
                        : AttributeTypeCode.String;

                    if (attributeTypeCode == AttributeTypeCode.Boolean)
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
                */
            }
            return items;
        }
    }
}
