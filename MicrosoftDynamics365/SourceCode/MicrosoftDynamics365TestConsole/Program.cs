using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Messages;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;
using Microsoft.Xrm.Tooling.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Description;
using System.Text;
using System.Threading.Tasks;



namespace MicrosoftDynamics365TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {

			string userName = "";
			string password = "";
			string url = "";

            // string _CrmConnectionString = $@"Url = {url};AuthType = OAuth;UserName = {userName};Password = {password};AppId = 51f81489-12ee-4a9e-aaae-a2591f45987d;RedirectUri = app://58145B91-0C36-4500-8554-080854F2AC97;LoginPrompt=Auto";
            
            // Dismantle App
            string _CrmConnectionString = $@"AuthType=ClientSecret;url=https://peakboard.crm4.dynamics.com/;ClientId=XXX;ClientSecret=XXX";


            CrmServiceClient crmConn = new CrmServiceClient(_CrmConnectionString);

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
                                Values = { "All Active Projects" }
                            },
                             new ConditionExpression
                            {
                                AttributeName = "returnedtypecode",
                                Operator = ConditionOperator.Equal,
                                Values = { "pb_project" }
                            },
                        }
                }
            };


            var result = crmConn.RetrieveMultiple(query);

            var fetchToQueryExpressionRequest = new FetchXmlToQueryExpressionRequest();
            fetchToQueryExpressionRequest.FetchXml = result.Entities[0].Attributes["fetchxml"].ToString();
            var fetchToQueryExpressionResponse = (FetchXmlToQueryExpressionResponse)crmConn.Execute(fetchToQueryExpressionRequest);
            QueryExpression qe = fetchToQueryExpressionResponse.Query;
            EntityCollection ec = crmConn.RetrieveMultiple(qe);

            foreach (var c in qe.ColumnSet.Columns)
            {
                System.Windows.MessageBox.Show(c);
                if (ec.Entities[0].Attributes.Contains(c) && ec.Entities[0].Attributes?[c] is OptionSetValue)
                {
                    Console.WriteLine("Jetzt");
                    // columnCollection.Add(new CustomListColumn(c, CustomListColumnTypes.Number));
                }
                else
                {
                    // columnCollection.Add(new CustomListColumn(c, CustomListColumnTypes.String));
                }
            }




            Console.ReadLine();



            //RetrieveEntityRequest retrieveEntityRequest = new RetrieveEntityRequest
            //{
            //    EntityFilters = EntityFilters.All,
            //    LogicalName = "account"
            //};

            //RetrieveEntityResponse retrieveAccountEntityResponse = (RetrieveEntityResponse)crmConn.Execute(retrieveEntityRequest);
            //EntityMetadata AccountEntity = retrieveAccountEntityResponse.EntityMetadata;

            //Console.WriteLine("Account entity metadata:");
            //Console.WriteLine(AccountEntity.SchemaName);
            //Console.WriteLine(AccountEntity.DisplayName.UserLocalizedLabel.Label);
            //Console.WriteLine(AccountEntity.EntityColor);

            //Console.WriteLine("Account entity attributes:");
            //foreach (object attribute in AccountEntity.Attributes)
            //{
            //    AttributeMetadata a = (AttributeMetadata)attribute;
            //    Console.WriteLine(a.LogicalName);
            //}
            //Console.ReadLine();






            //IOrganizationService _service = null;
            //try
            //{
            //	ClientCredentials clientCredentials = new ClientCredentials();
            //	clientCredentials.UserName.UserName = "patrick.theobald@peakboard.com";
            //	clientCredentials.UserName.Password = "Hellraiser78!";


            //	// Copy and Paste Organization Service Endpoint Address URL
            //	//_service = new OrganizationServiceProxy(new Uri("https://peakboard-test.api.crm4.dynamics.com/XRMServices/2011/Organization.svc"),
            //	// null, clientCredentials, null);

            //	if (_service != null)
            //	{
            //		Guid userid = ((WhoAmIResponse)_service.Execute(new WhoAmIRequest())).UserId;
            //		if (userid != Guid.Empty)
            //		{
            //			Console.WriteLine("Connection Successful!...");
            //			RetrieveContact(_service, userid);
            //		}
            //	}
            //	else
            //	{
            //		Console.WriteLine("Failed to Established Connection!!!");
            //	}
            //}
            //catch (Exception ex)
            //{
            //	Console.WriteLine("Exception caught - " + ex.Message);
            //}
            //Console.ReadKey();
        }



        public static void RetrieveContact(IOrganizationService _service, Guid userid)
        {
            //string fetch = @"<fetch mapping='logical'>
            //                    <entity name='contact'>
            //                        <attribute name='contactid'/>
            //                        <attribute name='fullname'/>
            //                        <attribute name='emailaddress1'/>
            //                    </entity>
            //                </fetch>";

            //EntityCollection result = _service.RetrieveMultiple(new FetchExpression(fetch));
            //if(result.Entities.Count>0)
            //{
            //    foreach(var c in result.Entities)
            //    {
            //        Console.WriteLine(c.Attributes["fullname"]+"    "+ c.Attributes["emailaddress1"]);
            //    }
            //}
            //else
            //{
            //    Console.WriteLine("No results found!");
            //}

            //RetrieveEntityRequest metaDataRequest = new RetrieveEntityRequest();
            //RetrieveEntityResponse metaDataResponse = new RetrieveEntityResponse();
            //metaDataRequest.EntityFilters = EntityFilters.All;
            //metaDataRequest.LogicalName = "Account".ToLower();
            //metaDataResponse = (RetrieveEntityResponse)_service.Execute(metaDataRequest);

            //var entities = metaDataResponse.EntityMetadata;

            //int i = 0;
            //foreach (var c in entities.Attributes)
            //{

            //    if(c.DisplayName.LocalizedLabels.Count()>1)
            //    {
            //        i++;
            //        Console.WriteLine(c.LogicalName);
            //    }

            //}
            //Console.WriteLine(i);





            //RetrieveAllEntitiesRequest metaDataRequest = new RetrieveAllEntitiesRequest();
            //RetrieveAllEntitiesResponse metaDataResponse = new RetrieveAllEntitiesResponse();
            //metaDataRequest.EntityFilters = EntityFilters.Entity;
            //metaDataResponse = (RetrieveAllEntitiesResponse)_service.Execute(metaDataRequest);

            //var entities = metaDataResponse.EntityMetadata;

            //int i = 0;
            //foreach (var c in entities)
            //{
            //    if (c.DisplayName.LocalizedLabels.Count() ==2)
            //    {
            //        Console.WriteLine(c.DisplayName.UserLocalizedLabel.Label);
            //        i++;
            //    }
            //}
            //Console.WriteLine("Size : " + i);





            //string columns = "contact";
            //string[] newColumns = columns.Replace(" ", String.Empty).ToLower().Split(',');
            //QueryExpression qe = new QueryExpression("fullname");
            //qe.ColumnSet = new ColumnSet(newColumns);
            //EntityCollection ec = _service.RetrieveMultiple(qe);

            //int i = 0;
            //foreach (Entity c in ec.Entities)
            //{

            //    if (!ec.Entities[i].Attributes.Contains(columns))
            //    {
            //        Console.WriteLine("NULL");
            //    }
            //    else
            //    {
            //        Console.WriteLine(c.Attributes[columns]);
            //    }
            //    i++;

            //}


            //RetrieveEntityRequest metaDataRequest = new RetrieveEntityRequest();
            //RetrieveEntityResponse metaDataResponse = new RetrieveEntityResponse();
            //metaDataRequest.EntityFilters = EntityFilters.Attributes;
            //metaDataRequest.MetadataId 
            //metaDataResponse = (RetrieveEntityResponse)_service.Execute(metaDataRequest);

            //var entities = metaDataResponse.EntityMetadata;

            //foreach (var c in entities.Attributes)
            //{
            //    if (c.DisplayName.LocalizedLabels.Count() > 1)
            //    {
            //        Console.WriteLine(c.LogicalName);
            //    }
            //}
            //EntityCollection retrievedRecords = new EntityCollection();

            //retrievedRecords = _service.RetrieveMultiple(query);

            //foreach(var c in retrievedRecords.Entities)
            //{
            //    Console.WriteLine(c.KeyAttributes);

            //}

            //ViewColumn vc = new ViewColumn();
            //vc.EntityLogicalName = "All Accounts";


            //QueryExpression query = new QueryExpression();
            //query.EntityName = "account";
            //query.ColumnSet = new ColumnSet(allColumns: true);


            //RetrieveMultipleRequest metaDataRequest = new RetrieveMultipleRequest();
            //RetrieveMultipleResponse metaDataResponse = new RetrieveMultipleResponse();
            //metaDataRequest.Query = query;
            //metaDataResponse = (RetrieveMultipleResponse)_service.Execute(metaDataRequest);

            //foreach(var c in metaDataResponse.EntityCollection.Entities[0].Attributes)
            //{
            //    Console.WriteLine(c.Key);
            //}


            //    var query = new QueryExpression
            //    {
            //        EntityName = "savedquery",
            //        ColumnSet = new ColumnSet("savedqueryid", "name", "fetchxml"),
            //        Criteria = new FilterExpression
            //        {
            //            FilterOperator = LogicalOperator.And,
            //            Conditions =
            //        {
            //        new ConditionExpression
            //        {
            //        AttributeName = "name",
            //        Operator = ConditionOperator.Equal,
            //        Values = { "View Name" }//View Name - "Active Accounts"
            //        },
            //        new ConditionExpression
            //        {
            //        AttributeName = "returnedtypecode",
            //        Operator = ConditionOperator.Equal,
            //        Values = { "Entity Name" } //Account
            //        }
            //        }
            //        }
            //    };
            //    var result = _service.RetrieveMultiple(query);
            //    var view = result.Entities.FirstOrDefault();
            //    var fetchXml = view.GetAttributeValue("fetchxml");
            //Console.WriteLine(fetchXml);


            //var query = new QueryExpression
            //{
            //    EntityName = "savedquery",
            //    ColumnSet = new ColumnSet("fetchxml"),
            //    Criteria = new FilterExpression
            //    {
            //        FilterOperator = LogicalOperator.And,
            //        Conditions =
            //        {
            //            new ConditionExpression
            //            {
            //                AttributeName = "name",
            //                Operator = ConditionOperator.Equal,
            //                Values = { "Active Accounts" }//View Name - "Active Accounts"
            //            },
            //        }
            //    }
            //};

            //var result = _service.RetrieveMultiple(query);


            //Console.WriteLine(result.Entities[0].Attributes["fetchxml"]);

            //var fetchToQueryExpressionRequest = new FetchXmlToQueryExpressionRequest();
            //fetchToQueryExpressionRequest.FetchXml = result.Entities[0].Attributes["fetchxml"].ToString();
            //var fetchToQueryExpressionResponse = (FetchXmlToQueryExpressionResponse)_service.Execute(fetchToQueryExpressionRequest);
            //QueryExpression qe = fetchToQueryExpressionResponse.Query;

            //EntityCollection ec = _service.RetrieveMultiple(qe);


            //for (int i = 0; i < 10; i++)
            //{
            //    foreach (string column in ec.Entities[0].Attributes.Keys)
            //    {
            //        string newString = "";

            //        if (ec.Entities.Count == 0 || !ec.Entities[0].Attributes.Keys.Contains(column))
            //        {
            //            newString = "";
            //        }
            //        else
            //        {
            //            if (ec.Entities[i].Attributes[column] is OptionSetValue)
            //            {
            //                OptionSetValue o = new OptionSetValue();
            //                o = (OptionSetValue)ec.Entities[0].Attributes[column];
            //                newString = o.Value.ToString();
            //            }
            //            else
            //            {
            //                newString = ec.Entities[0].Attributes[column].ToString();
            //            }
            //        }
            //    Console.WriteLine(column);
            //    Console.WriteLine(newString);
            //    }
            //}



            //var query = new QueryExpression
            //{
            //    EntityName = "savedquery",
            //    ColumnSet = new ColumnSet("fetchxml"),
            //    Criteria = new FilterExpression
            //    {
            //        FilterOperator = LogicalOperator.And,
            //        Conditions =
            //        {
            //            new ConditionExpression
            //            {
            //                AttributeName = "name",
            //                Operator = ConditionOperator.Equal,
            //                Values = { "Active Accounts" }//View Name - "Active Accounts"
            //            },
            //        }
            //    }
            //};

            //var result = _service.RetrieveMultiple(query);

            //var fetchToQueryExpressionRequest = new FetchXmlToQueryExpressionRequest();
            //fetchToQueryExpressionRequest.FetchXml = result.Entities[0].Attributes["fetchxml"].ToString();
            //var fetchToQueryExpressionResponse = (FetchXmlToQueryExpressionResponse)_service.Execute(fetchToQueryExpressionRequest);
            //QueryExpression qe = fetchToQueryExpressionResponse.Query;
            //EntityCollection ec = _service.RetrieveMultiple(qe);

            //foreach (var c in ec.Entities[0].Attributes.Keys)
            //{
            //    Console.WriteLine(c);
            //}


            string maxRows = "25";
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
                            Values = { "All Accounts" }//View Name - "Active Accounts"
                        },
                    }
                }
            };

            var result = _service.RetrieveMultiple(query);

            var fetchToQueryExpressionRequest = new FetchXmlToQueryExpressionRequest();
            fetchToQueryExpressionRequest.FetchXml = result.Entities[0].Attributes["fetchxml"].ToString();
            var fetchToQueryExpressionResponse = (FetchXmlToQueryExpressionResponse)_service.Execute(fetchToQueryExpressionRequest);
            QueryExpression qe = fetchToQueryExpressionResponse.Query;


            Console.WriteLine(result.Entities[0].Attributes["fetchxml"].ToString());

            EntityCollection ec = _service.RetrieveMultiple(qe);

            if (int.Parse(maxRows) > ec.Entities.Count)
            {
                maxRows = ec.Entities.Count.ToString();
            }
            for (int i = 0; i < int.Parse(maxRows); i++)
            {
                Console.WriteLine(i);
                foreach (string column in qe.ColumnSet.Columns)
                {
                    string newString = "";

                    if (ec.Entities.Count == 0 || !ec.Entities[i].Attributes.Keys.Contains(column))
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
                        else if(ec.Entities[i].Attributes[column] is AliasedValue)
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
                        else
                        {
                            newString = ec.Entities[i].Attributes[column].ToString();
                        }
                    }

                    Console.WriteLine(column + " : " + newString);
                }
                Console.WriteLine();
            }







            //QueryExpression personalViews = new QueryExpression("savedquery");
            //personalViews.ColumnSet = new ColumnSet("name", "savedqueryid");

            //EntityCollection viewCollection = _service.RetrieveMultiple(personalViews);

            //foreach (var c in viewCollection.Entities)
            //{
            //    Console.WriteLine(c["name"]+"   "+c["savedqueryid"]);
            //}




            //if (!(c.Attributes["firstname"] is string))
            //{
            //    Console.WriteLine(" NULLLLLLLLLL " + c.Attributes["fullname"].ToString());
            //}
            //else
            //{
            //    Console.WriteLine(c.Attributes["firstname"].ToString() + "  " + c.Attributes["fullname"].ToString());
            //}
            //if (c.Attributes.Contains("customertypecode"))
            //{
            //    if (c.Attributes["customertypecode"] is OptionSetValue)
            //    {
            //        OptionSetValue o = new OptionSetValue();

            //    }
            //    Console.WriteLine(c.Attributes["customertypecode"].ToString() + "  " + c.Attributes["customertypecode"].ToString());
            //}
            //else
            //{
            //    Console.WriteLine(" NULLLLLLLLLL " + c.Attributes["customertypecode"].ToString());
            //}

            //foreach (var c in ec.Entities[0].Attributes.Keys)
            //{
            //    columns.Add(c);
            //}


            //}
            //foreach (var c in ec.Entities[0].Attributes.Keys)
            //{
            //    Console.WriteLine(c);
            //}





            //Console.Write(ec.Entities[0].Attributes.Values.);

            //for (int i=0;i<ec.Entities.Count;i++)
            //    {
            //        for (int j = 0; j < ec.Entities[i].Attributes.Count;j++)
            //        {
            //            Console.Write(ec.Entities[i].Attributes[j.ToString()+"   "]);
            //        }
            //    Console.WriteLine("");
            //    }
        }
        }
}

