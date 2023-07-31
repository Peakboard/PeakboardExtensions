using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using Peakboard.ExtensionKit;
using System;
using System.Collections.Generic;
using System.IdentityModel.Metadata;
using System.Linq;
using System.ServiceModel.Description;
using System.Text;
using System.Threading.Tasks;

namespace PeakboardExtensionMicrosoftDynamics365
{
    public class CrmHelper
    {
        public static CrmServiceClient TryConnection(string URL, string username, string password, string clientid, string clientsecret)
        {
            string _CrmConnectionString = string.Empty;
            
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                _CrmConnectionString = $@"Url = {URL};AuthType = OAuth;UserName = {username};Password = {password};AppId = 51f81489-12ee-4a9e-aaae-a2591f45987d;RedirectUri = app://58145B91-0C36-4500-8554-080854F2AC97;LoginPrompt=Auto";
            }
            else if (!string.IsNullOrEmpty(clientid) && !string.IsNullOrEmpty(clientsecret))
            {
                _CrmConnectionString = $@"AuthType=ClientSecret;url={URL};ClientId={clientid};ClientSecret={clientsecret}";
            }
            else
            {
                throw new Exception("Either user name / password or client id / client secret must be set. Otherwise it doesn't make sense!");
            }
            


            CrmServiceClient crmConnection = new CrmServiceClient(_CrmConnectionString);

            if (!string.IsNullOrWhiteSpace(crmConnection.LastCrmError))
                throw new Exception(crmConnection.LastCrmError);

            WhoAmIRequest request = new WhoAmIRequest();
            WhoAmIResponse response = (WhoAmIResponse)
            crmConnection.Execute(request);

            if (response.UserId == null)
            {
                return null;
            }

            return crmConnection;
        }

        public static List<CrmName> GetTableNames(string URL, string username, string password, string clientid, string clientsecret)
        {
            List<CrmName> tableList = new List<CrmName>();

            CrmServiceClient service = TryConnection(URL, username, password, clientid, clientsecret);

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
                        CrmName crmName = new CrmName
                        {
                            displayName = c.LogicalName,
                            logicalName = c.LogicalName
                        };
                        tableList.Add(crmName);
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

        public static List<CrmName> GetViewNames(string URL, string username, string password, string clientid, string clientsecret)
        {
            List<CrmName> viewList = new List<CrmName>();

            CrmServiceClient service = TryConnection(URL, username, password, clientid, clientsecret);

            if (service == null)
            {
                return null;
            }
            else
            {
                try
                {
                    QueryExpression personalViews = new QueryExpression("savedquery");
                    personalViews.ColumnSet = new ColumnSet(true);

                    EntityCollection viewCollection = new EntityCollection();

                    viewCollection = service.RetrieveMultiple(personalViews);

                    foreach (var c in viewCollection.Entities)
                    {
                        CrmName crmName = new CrmName
                        {
                            displayName = c["returnedtypecode"].ToString() + " || " + c["name"].ToString(),
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

        public static List<CrmName> GetTableColumns(string link, string username, string password, string clientid, string clientsecret, string table)
        {
            List<CrmName> columns=new List<CrmName>();

            CrmServiceClient service = TryConnection(link, username, password, clientid, clientsecret);

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
                        if (c.DisplayName.LocalizedLabels.Count() > 0)
                        {
                            CrmName crmName = new CrmName
                            {
                                displayName = c.DisplayName.UserLocalizedLabel?.Label ?? c.LogicalName,
                                logicalName = c.LogicalName,
                                AttributeType = c.AttributeType.ToString()
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

        public static CustomListColumnCollection GetEntityColumns(string URL, string username, string password, string clientid, string clientsecret, string table)
        {
            CustomListColumnCollection columnCollection = new CustomListColumnCollection();

            foreach (var e in CrmHelper.GetTableColumns(URL, username, password, clientid, clientsecret, table))
            {
                if (e.AttributeType.Equals("Money") || e.AttributeType.Equals("Integer") || e.AttributeType.Equals("BigInt"))
                    columnCollection.Add(new CustomListColumn(e.logicalName, CustomListColumnTypes.Number));
                else
                    columnCollection.Add(new CustomListColumn(e.logicalName, CustomListColumnTypes.String));
            }

            return columnCollection;
        }

        public static CustomListColumnCollection GetViewColumns(string link, string username, string password, string clientid, string clientsecret, string ViewName)
        {
            string EntityName = string.Empty;

            if (ViewName.Contains(" || "))
            {
                EntityName = ViewName.Split(new string[] { " || " }, StringSplitOptions.None)[0];
                ViewName = ViewName.Split(new string[] { " || " }, StringSplitOptions.None)[1];
            }
            CustomListColumnCollection columnCollection = new CustomListColumnCollection();
            CrmServiceClient service = TryConnection(link, username, password, clientid, clientsecret);

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
                                Values = { ViewName }
                            },
                            new ConditionExpression
                            {
                                AttributeName = "returnedtypecode",
                                Operator = ConditionOperator.Equal,
                                Values = { EntityName }
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

                    EntityCollection ec = service.RetrieveMultiple(qe);

                    foreach (var c in qe.ColumnSet.Columns)
                    {
                        if (ec.Entities[0].Attributes.Contains(c) && ec.Entities[0].Attributes?[c] is Money)
                        {
                            columnCollection.Add(new CustomListColumn(c, CustomListColumnTypes.Number));
                        }
                        else
                        {
                            columnCollection.Add(new CustomListColumn(c, CustomListColumnTypes.String));
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

        public static CustomListObjectElementCollection GetViewData(string link, string username, string password, string clientid, string cliensecret, string maxRows, string ViewName)
        {
            string EntityName = string.Empty;

            if (ViewName.Contains(" || "))
            {
                EntityName = ViewName.Split(new string[] { " || " }, StringSplitOptions.None)[0];
                ViewName = ViewName.Split(new string[] { " || " }, StringSplitOptions.None)[1];
            }

            CustomListObjectElementCollection itemsCollection = new CustomListObjectElementCollection();
            CrmServiceClient service = TryConnection(link, username, password, clientid, cliensecret);

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
                            Values = { ViewName }
                        },
                        new ConditionExpression
                            {
                                AttributeName = "returnedtypecode",
                                Operator = ConditionOperator.Equal,
                                Values = { EntityName }
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
                                    if (ec.Entities[i].FormattedValues.Contains(column))
                                        item.Add(column, ec.Entities[i].FormattedValues[column]);
                                    else
                                        item.Add(column, string.Empty);
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

        public static CustomListObjectElementCollection GetDataFromEntity(string URL, string username, string password, string clientid, string clientsecret, string maxRows, string table, string displayName, string logicalName)
        {
            CustomListObjectElementCollection itemsCollection = new CustomListObjectElementCollection();
            CrmServiceClient service = TryConnection(URL, username, password, clientid, clientsecret);

            if (service == null)
            {
                return null;
            }

            List<CrmName> MyColumns = CrmHelper.GetTableColumns(URL, username, password, clientid, clientsecret, table);
            
            System.Collections.Hashtable MyTypes = new System.Collections.Hashtable();
            foreach (var e in CrmHelper.GetTableColumns(URL, username, password, clientid, clientsecret, table))
            {
                MyTypes.Add(e.logicalName, e.AttributeType);
            }

            QueryExpression qe = new QueryExpression(table.ToLower());
            qe.ColumnSet = new ColumnSet(true);
            EntityCollection ec = new EntityCollection();

            int iMaxRows = 0;
            if (int.TryParse(maxRows, out iMaxRows))
            {
                qe.TopCount = iMaxRows;
            }
                
            ec= service.RetrieveMultiple(qe);

            if (!string.IsNullOrWhiteSpace(service.LastCrmError))
                throw new Exception(service.LastCrmError);
            

            foreach (Entity entity in ec.Entities)
            {
                CustomListObjectElement item = new CustomListObjectElement();
                foreach (CrmName column in MyColumns)
                {
                    string newString = "";

                    if (entity.Attributes.Contains(column.logicalName))
                    { 
					    if (entity[column.logicalName] is OptionSetValue)
					    {
                            if (entity.FormattedValues.Contains(column.logicalName))
                            {
                                newString = entity.FormattedValues[column.logicalName];
                            }
                            
                            // ID:
                            //OptionSetValue o = (OptionSetValue)entity[column.logicalName];
						    //newString = o.Value.ToString();

					    }
					    else if (entity[column.logicalName] is AliasedValue)
					    {
						    AliasedValue o = new AliasedValue();
						    o = (AliasedValue)entity[column.logicalName];
						    newString = o.Value.ToString();
					    }
					    else if (entity[column.logicalName] is EntityReference)
					    {
						    EntityReference o = new EntityReference();
						    o = (EntityReference)entity[column.logicalName];
						    newString = o.Id.ToString();
					    }
					    else if (entity[column.logicalName] is Money)
					    {
						    Money o = new Money();
						    o = (Money)entity[column.logicalName];
						    newString = o.Value.ToString();
					    }
					    else
					    {
						    newString = entity[column.logicalName].ToString();
					    }
                    }
                    else
                    {
                        newString = "";
                    }

                    if (column.AttributeType.Equals("Money") || column.AttributeType.Equals("Integer") || column.AttributeType.Equals("BigInt"))
                    {
                        double d;
                        if (!double.TryParse(newString, out d))
                            newString = "0";
                    }

                    item.Add(column.logicalName, newString);
                }
                itemsCollection.Add(item);
            }
            return itemsCollection;
        }
    }
}
