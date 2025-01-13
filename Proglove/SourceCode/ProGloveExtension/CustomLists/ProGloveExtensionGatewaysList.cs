using Newtonsoft.Json;
using Peakboard.ExtensionKit;
using ProGlove;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProGloveExtension.CustomLists
{
    [Serializable]
    public class ProGloveExtensionGatewaysList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "CustomListGateways",
                Name = "GatewaysList",
                Description = "Add Gateways",
                PropertyInputPossible = true,
                PropertyInputDefaults =
                {
                    new CustomListPropertyDefinition(){Name = "ClientId"},
                    new CustomListPropertyDefinition(){Name = "BasedUrl"},
                    new CustomListPropertyDefinition(){Name = "Username"},
                    new CustomListPropertyDefinition(){Name = "Password"}
                }
            };
        }
        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            return new CustomListColumnCollection
            {
                new CustomListColumn("Id", CustomListColumnTypes.String),
                new CustomListColumn("Name", CustomListColumnTypes.String),
                new CustomListColumn("Type", CustomListColumnTypes.String),
                new CustomListColumn("Usecase", CustomListColumnTypes.String),
                new CustomListColumn("Depth", CustomListColumnTypes.Number),
                new CustomListColumn("Deleted", CustomListColumnTypes.Number),
                new CustomListColumn("ParentId", CustomListColumnTypes.String),
                new CustomListColumn("EntityType", CustomListColumnTypes.String),
                new CustomListColumn("ActualConfig", CustomListColumnTypes.String),
                new CustomListColumn("ApIpv4Address", CustomListColumnTypes.String),
                new CustomListColumn("Station", CustomListColumnTypes.String),
                new CustomListColumn("Path", CustomListColumnTypes.String),
                new CustomListColumn("City", CustomListColumnTypes.String),
                new CustomListColumn("Country", CustomListColumnTypes.String),
                new CustomListColumn("District", CustomListColumnTypes.String),
                new CustomListColumn("Latitude", CustomListColumnTypes.String),
                new CustomListColumn("Longitude", CustomListColumnTypes.String),
                new CustomListColumn("PostalCode", CustomListColumnTypes.String),
                new CustomListColumn("Premise", CustomListColumnTypes.String),
                new CustomListColumn("Region", CustomListColumnTypes.String),
                new CustomListColumn("Street", CustomListColumnTypes.String),
                new CustomListColumn("Policy", CustomListColumnTypes.String)
            };

        }
        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
                var items = new CustomListObjectElementCollection();
                ProGloveClient proGloveClient = new ProGloveClient(data.Properties["BasedUrl"], data.Properties["ClientId"]);
                var ath = proGloveClient.GetAuthenticationResponseAsync(data.Properties["Username"], data.Properties["Password"]).Result;
                if (ath == null)
                {
                    Log.Error("Incorrect authorization data");
                    return new CustomListObjectElementCollection();
                }
                string token = ath.AuthenticationResult.IdToken;
                var gateways = proGloveClient.GetGatewaysOrganisationAsync(ath.AuthenticationResult.IdToken).Result;
                if (gateways == null)
                {
                    Log.Error("Failed to fetch gateways organisation data");
                    return new CustomListObjectElementCollection();
                }
               
                CustomListObjectElement objectElement = null;

                foreach (var organisation in gateways.Items)
                {
                    try
                    {
                        if (organisation.Node != null)
                        {
                            objectElement = new CustomListObjectElement();
                            objectElement.Add("Id", $"{organisation.Node.Id}");
                            objectElement.Add("Name", $"{organisation.Node.Name}");
                            objectElement.Add("Type", $"{organisation.Node.Type}");
                            objectElement.Add("Usecase", $"{organisation.Node.UseCase}");
                            objectElement.Add("Depth", $"{organisation.Node.Depth}");
                            objectElement.Add("Deleted", $"{organisation.Node.Deleted}");
                            objectElement.Add("ParentId", $"{organisation.Node.ParentId}");
                            objectElement.Add("EntityType", $"{organisation.Node.EntityType}");
                            objectElement.Add("ActualConfig", $"{organisation.Node.ActualConfig}");
                            objectElement.Add("ApIpv4Address", $"{organisation.Node.ApIpv4Address}");
                            objectElement.Add("Station", $"{organisation.Node.Station}");
                            objectElement.Add("Path", JsonConvert.SerializeObject(organisation.Node.Path));
                            if (organisation.Node.Address != null)
                            {
                                objectElement.Add("City", $"{organisation.Node.Address.City}");
                                objectElement.Add("Country", $"{organisation.Node.Address.Country}");
                                objectElement.Add("District", $"{organisation.Node.Address.District}");
                                objectElement.Add("Latitude", $"{organisation.Node.Address.Latitude}");
                                objectElement.Add("Longitude", $"{organisation.Node.Address.Longitude}");
                                objectElement.Add("PostalCode", $"{organisation.Node.Address.PostalCode}");
                                objectElement.Add("Premise", $"{organisation.Node.Address.Premise}");
                                objectElement.Add("Region", $"{organisation.Node.Address.Region}");
                                objectElement.Add("Street", $"{organisation.Node.Address.Street}");
                            }
                            else
                            {
                                objectElement.Add("City", null);
                                objectElement.Add("Country", null);
                                objectElement.Add("District", null);
                                objectElement.Add("Latitude", null);
                                objectElement.Add("Longitude", null);
                                objectElement.Add("PostalCode", null);
                                objectElement.Add("Premise", null);
                                objectElement.Add("Region", null);
                                objectElement.Add("Street", null);
                             }
                            if (organisation.Node.Policy != null)
                            {
                                objectElement.Add("Policy", JsonConvert.SerializeObject(organisation.Node.Policy));
                            }
                            else
                            {
                                objectElement.Add("Policy",null);
                            }

                        }   
                    items.Add(objectElement);
                }
                    catch (Exception ex)
                    {
                       Log.Error(ex.Message);
                    }
                   
                }
                Log.Info($"Items count {items.Count}");
            return items;
        }
    }
}
