using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Peakboard.ExtensionKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Description;
using System.Text;
using System.Threading.Tasks;

namespace PeakboardExtensionMicrosoftDynamics365
{
    public class CrmHelper
    {
        public static IOrganizationService TryConnection(string link, string username, string password)
        {
            IOrganizationService service = null;
            try
            {
                ClientCredentials clientCredentials = new ClientCredentials();
                clientCredentials.UserName.UserName = username;
                clientCredentials.UserName.Password = password;

                if (Uri.TryCreate(link, UriKind.RelativeOrAbsolute, out Uri url))
                {
                    service = new OrganizationServiceProxy(url, null, clientCredentials, null);
                }
                else
                {
                    return null;
                }

            }
            catch (Exception ex)
            {
                return null;
            }
            

            return service;
        }

        public static List<CrmName> GetTablesName(string link, string username, string password)
        {
            List<CrmName> tableList = new List<CrmName>();

            IOrganizationService service = TryConnection(link, username, password);

            if (service == null)
            {
                return null;
            }
            else
            {
                try
                {
                    RetrieveAllEntitiesRequest metaDataRequest = new RetrieveAllEntitiesRequest();
                    RetrieveAllEntitiesResponse metaDataResponse = new RetrieveAllEntitiesResponse();
                    metaDataRequest.EntityFilters = EntityFilters.Entity;
                    metaDataResponse = (RetrieveAllEntitiesResponse)service.Execute(metaDataRequest);

                    var entities = metaDataResponse.EntityMetadata;

                    foreach (var c in entities)
                    {
                        if (c.DisplayName.LocalizedLabels.Count() > 1)
                        {
                            CrmName crmName = new CrmName
                            {
                                displayName = c.DisplayName.UserLocalizedLabel.Label,
                                logicalName = c.LogicalName
                            };
                            tableList.Add(crmName);
                        }
                    }

                    
                }
                catch(Exception exception)
                {
                    return null;
                }
                var sortedList = tableList.OrderBy(x => x.displayName).ToList();
                return sortedList;
            }
        }

        public static List<CrmName> GetViewsName(string link, string username, string password)
        {
            List<CrmName> viewList = new List<CrmName>();

            IOrganizationService service = TryConnection(link, username, password);

            if (service == null)
            {
                return null;
            }
            else
            {
                

                try
                {
                    QueryExpression personalViews = new QueryExpression("savedquery");
                    personalViews.ColumnSet = new ColumnSet("name");

                    EntityCollection viewCollection = new EntityCollection();

                    viewCollection = service.RetrieveMultiple(personalViews);

                    foreach (var c in viewCollection.Entities)
                    {
                        CrmName crmName = new CrmName
                        {
                            displayName = c["name"].ToString(),
                            logicalName = c["name"].ToString()
                        };
                        viewList.Add(crmName);
                    }
                }
                catch
                {
                    return null;
                }

                

                var sortedList = viewList.OrderBy(x => x.displayName).ToList();
                return sortedList;
            }
        }

        public static List<CrmName> GetTableColumns(string link, string username, string password, string table)
        {
            List<CrmName> columns=new List<CrmName>();

            IOrganizationService service = TryConnection(link, username, password);

            if (service == null)
            {
                return null;
            }
            else
            {
                try
                {
                    RetrieveEntityRequest metaDataRequest = new RetrieveEntityRequest();
                    RetrieveEntityResponse metaDataResponse = new RetrieveEntityResponse();
                    metaDataRequest.EntityFilters = EntityFilters.Attributes;
                    metaDataRequest.LogicalName = table.ToLower();
                    metaDataResponse = (RetrieveEntityResponse)service.Execute(metaDataRequest);

                    var entities = metaDataResponse.EntityMetadata;

                    foreach (var c in entities.Attributes)
                    {
                        if (c.DisplayName.LocalizedLabels.Count() > 1)
                        {
                            CrmName crmName = new CrmName
                            {
                                displayName = c.DisplayName.UserLocalizedLabel.Label,
                                logicalName = c.LogicalName
                            };
                            columns.Add(crmName);
                        }
                    }
                }
                catch
                {
                    return null;
                }
                
            }

            var sortedList = columns.OrderBy(x => x.displayName).ToList();
            return sortedList;

        }

        public static CustomListColumnCollection GetEntityColumns(string link, string username, string password, string table, string displayName, string logicalName)
        {
            CustomListColumnCollection columnCollection = new CustomListColumnCollection();

            IOrganizationService service = TryConnection(link, username, password);

            if (service == null)
            {
                return null;
            }
            else
            {
                QueryExpression qe = new QueryExpression(table.ToLower());

                string[] newDisplayName = displayName.Split(',');
                string[] newLogicalName = logicalName.Replace(" ", String.Empty).ToLower().Split(',');
                qe.ColumnSet = new ColumnSet(newLogicalName);
                EntityCollection ec = new EntityCollection();
                try
                {
                    ec = service.RetrieveMultiple(qe);
                }
                catch
                {
                    return null;
                }
                
                foreach (var c in qe.ColumnSet.Columns)
                {
                    if (ec.Entities[0].Attributes.Contains(c))
                    {
                        if (ec.Entities[0].Attributes[c] is Money)
                        {
                            columnCollection.Add(new CustomListColumn(c, CustomListColumnTypes.Number));
                        }
                        else
                        {
                            columnCollection.Add(new CustomListColumn(c, CustomListColumnTypes.String));
                        }
                    }
                }
            }

            return columnCollection;
        }

        public static CustomListColumnCollection GetViewColumns(string link, string username, string password, string logicalNameView)
        {
            CustomListColumnCollection columnCollection = new CustomListColumnCollection();

            IOrganizationService service = TryConnection(link, username, password);

            if (service == null)
            {
                return null;
            }
            else
            {
                var query = new QueryExpression
                {
                    EntityName = "savedquery",
                    ColumnSet = new ColumnSet("fetchxml"),
                    Criteria = new FilterExpression
                    {
                        FilterOperator = LogicalOperator.And,
                        Conditions =
                        {
                            new ConditionExpression
                            {
                                AttributeName = "name",
                                Operator = ConditionOperator.Equal,
                                Values = { logicalNameView }//View Name - "Active Accounts"
                            },
                        }
                    }
                };



                try
                {
                    var result = service.RetrieveMultiple(query);

                    var fetchToQueryExpressionRequest = new FetchXmlToQueryExpressionRequest();
                    fetchToQueryExpressionRequest.FetchXml = result.Entities[0].Attributes["fetchxml"].ToString();
                    var fetchToQueryExpressionResponse = (FetchXmlToQueryExpressionResponse)service.Execute(fetchToQueryExpressionRequest);
                    QueryExpression qe = fetchToQueryExpressionResponse.Query;

                    EntityCollection ec = new EntityCollection();
                    ec = service.RetrieveMultiple(qe);

                    foreach (var c in qe.ColumnSet.Columns)
                    {
                        if (ec.Entities[0].Attributes.Contains(c))
                        {
                            if (ec.Entities[0].Attributes[c] is Money)
                            {
                                columnCollection.Add(new CustomListColumn(c, CustomListColumnTypes.Number));
                            }
                            else
                            {
                                columnCollection.Add(new CustomListColumn(c, CustomListColumnTypes.String));
                            }
                        }
                    }
                }
                catch
                {
                    return null;
                }
            }

            return columnCollection;
        }

        public static CustomListObjectElementCollection GetDataFromView(string link, string username, string password, string maxRows, string logicalNameView)
        {
            CustomListObjectElementCollection itemsCollection = new CustomListObjectElementCollection();

            IOrganizationService service = TryConnection(link, username, password);

            if (service == null)
            {
                return null;
            }
            else
            {
                var query = new QueryExpression
                {
                    EntityName = "savedquery",
                    ColumnSet = new ColumnSet("fetchxml"),
                    Criteria = new FilterExpression
                    {
                        FilterOperator = LogicalOperator.And,
                        Conditions =
                    {
                        new ConditionExpression
                        {
                            AttributeName = "name",
                            Operator = ConditionOperator.Equal,
                            Values = { logicalNameView }//View Name - "Active Accounts"
                        },
                    }
                    }
                };

               
                try
                {
                    var result = service.RetrieveMultiple(query);

                    var fetchToQueryExpressionRequest = new FetchXmlToQueryExpressionRequest();
                    fetchToQueryExpressionRequest.FetchXml = result.Entities[0].Attributes["fetchxml"].ToString();
                    var fetchToQueryExpressionResponse = (FetchXmlToQueryExpressionResponse)service.Execute(fetchToQueryExpressionRequest);
                    QueryExpression qe = fetchToQueryExpressionResponse.Query;

                    EntityCollection ec = new EntityCollection();

                    ec = service.RetrieveMultiple(qe);

                    if (int.Parse(maxRows) > ec.Entities.Count)
                    {
                        maxRows = ec.Entities.Count.ToString();
                    }
                    for (int i = 0; i < int.Parse(maxRows); i++)
                    {
                        CustomListObjectElement item = new CustomListObjectElement();
                        foreach (string column in qe.ColumnSet.Columns)
                        {

                            if (ec.Entities.Count == 0 || !ec.Entities[i].Attributes.Keys.Contains(column))
                            {
                                item.Add(column, "");
                            }
                            else
                            {
                                if (ec.Entities[i].Attributes[column] is OptionSetValue)
                                {
                                    OptionSetValue o = new OptionSetValue();
                                    o = (OptionSetValue)ec.Entities[i].Attributes[column];
                                    item.Add(column, o.Value.ToString());
                                }
                                else if (ec.Entities[i].Attributes[column] is AliasedValue)
                                {
                                    AliasedValue o = new AliasedValue();
                                    o = (AliasedValue)ec.Entities[i].Attributes[column];
                                    item.Add(column, o.Value.ToString());
                                }
                                else if (ec.Entities[i].Attributes[column] is EntityReference)
                                {
                                    EntityReference o = new EntityReference();
                                    o = (EntityReference)ec.Entities[i].Attributes[column];
                                    item.Add(column, o.Name.ToString());

                                }
                                else if (ec.Entities[i].Attributes[column] is Money)
                                {
                                    Money o = new Money();
                                    o = (Money)ec.Entities[i].Attributes[column];
                                    item.Add(column, o.Value);
                                }
                                else
                                {
                                    item.Add(column, ec.Entities[i].Attributes[column].ToString());
                                }
                            }


                        }
                        itemsCollection.Add(item);
                    }
                }
                catch
                {
                    return null;
                }

                
            }



            return itemsCollection;
        }

        public static CustomListObjectElementCollection GetDataFromEntity(string link, string username, string password, string maxRows, string table, string displayName, string logicalName)
        {
            CustomListObjectElementCollection itemsCollection = new CustomListObjectElementCollection();

            IOrganizationService service = TryConnection(link, username, password);

            if (service == null)
            {
                return null;
            }
            else
            {

                QueryExpression qe = new QueryExpression(table.ToLower());

                //string[] newDisplayName = displayName.Split(',');
                string[] newLogicalName = logicalName.Replace(" ", String.Empty).ToLower().Split(',');
                qe.ColumnSet = new ColumnSet(newLogicalName);
                EntityCollection ec = new EntityCollection();

                try
                {
                    ec= service.RetrieveMultiple(qe);
                }
                catch
                {
                    return null;
                }
                
                

                if (int.Parse(maxRows) > ec.Entities.Count)
                {
                    maxRows = ec.Entities.Count.ToString();
                }
                for (int i = 0; i < int.Parse(maxRows); i++)
                {
                    CustomListObjectElement item = new CustomListObjectElement();
                    foreach (string column in newLogicalName)
                    {
                        string newString = "";

                        if (ec.Entities.Count == 0 || !ec.Entities[i].Attributes.Contains(column))
                        {
                            newString = "";
                        }
                        else
                        {
                            if (ec.Entities[i].Attributes[column] is OptionSetValue)
                            {
                                OptionSetValue o = new OptionSetValue();
                                o = (OptionSetValue)ec.Entities[i].Attributes[column];
                                newString = o.Value.ToString();
                            }
                            else if (ec.Entities[i].Attributes[column] is AliasedValue)
                            {
                                AliasedValue o = new AliasedValue();
                                o = (AliasedValue)ec.Entities[i].Attributes[column];
                                newString = o.Value.ToString();
                            }
                            else if (ec.Entities[i].Attributes[column] is EntityReference)
                            {
                                EntityReference o = new EntityReference();
                                o = (EntityReference)ec.Entities[i].Attributes[column];
                                newString = o.Name.ToString();
                            }
                            else if (ec.Entities[i].Attributes[column] is Money)
                            {
                                Money o = new Money();
                                o = (Money)ec.Entities[i].Attributes[column];
                                newString = o.Value.ToString();
                            }
                            else
                            {
                                newString = ec.Entities[i].Attributes[column].ToString();
                            }
                        }

                        item.Add(column, newString);
                    }
                    itemsCollection.Add(item);
                }
            }



        return itemsCollection;
        }
    }
}
