using Newtonsoft.Json;
using Peakboard.ExtensionKit;
using ProGlove;
using ProGlove.Models;
using System;
using System.Collections.Generic;

namespace ProGloveExtension.CustomLists
{
    [Serializable]
    [CustomListIcon("ProGloveExtension.pb_datasource_proglove.png")]
    public class ProGloveExtensionReportsList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "CustomListReports",
                Name = "ReportsCustomList",
                Description = "Add Gateways",
                PropertyInputPossible = true,
                PropertyInputDefaults =
                {
                    new CustomListPropertyDefinition(){Name = "CustomerID"},
                    new CustomListPropertyDefinition(){Name = "BasedUrl"},
                    new CustomListPropertyDefinition(){Name = "Email"},
                    new CustomListPropertyDefinition(){Name = "Password",Masked = true}
                }
            };
        }
        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            var columnsColl = new CustomListColumnCollection
            {
                new CustomListColumn("Id", CustomListColumnTypes.String),
                new CustomListColumn("Title", CustomListColumnTypes.String),
                new CustomListColumn("ReportType", CustomListColumnTypes.String),
                new CustomListColumn("Status", CustomListColumnTypes.String),
                new CustomListColumn("TimeCreated", CustomListColumnTypes.Number),
                new CustomListColumn("DeviceSerial", CustomListColumnTypes.String),
                new CustomListColumn("PhotosCount", CustomListColumnTypes.Number),
                new CustomListColumn("Path", CustomListColumnTypes.String),
                new CustomListColumn("ThumbnailUrl", CustomListColumnTypes.String),
                new CustomListColumn("ThumbnailAttachmentSortKey", CustomListColumnTypes.Number),
                new CustomListColumn("Next", CustomListColumnTypes.String),
                new CustomListColumn("Previous", CustomListColumnTypes.String),
                new CustomListColumn("Description", CustomListColumnTypes.String),
                new CustomListColumn("Size", CustomListColumnTypes.Number),
                new CustomListColumn("Filters", CustomListColumnTypes.String),
                new CustomListColumn("Search", CustomListColumnTypes.String),
                new CustomListColumn("Sort", CustomListColumnTypes.String)
            };

            return columnsColl;
        }
        protected override  CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            try
            {
                using (ProGloveClient proGloveClient =
                       new ProGloveClient(data.Properties["BasedUrl"], data.Properties["CustomerID"]))
                {
                    var customList = new CustomListObjectElementCollection();
                    CustomListObjectElement objectElement = null;

                    var ath = proGloveClient
                        .GetAuthenticationResponseAsync(data.Properties["Email"], data.Properties["Password"])
                        .Result;
                    if (ath == null)
                    {
                        Log.Error("Incorrect authorization data");
                        return new CustomListObjectElementCollection();
                    }

                    string token = ath.AuthenticationResult.IdToken;
                    var gateways = proGloveClient.GetGatewaysOrganisationAsync(token).Result;
                    List<string> organisationIds = new List<string>();
                    List<string> links = new List<string>();
                    List<Reports> reports = new List<Reports>();
                    if (gateways != null)
                    {
                        foreach (var item in gateways.Items)
                        {
                            if (item.Node != null)
                            {
                                if (!string.IsNullOrEmpty(item.Node.Id))
                                {
                                    var existId = organisationIds.Contains(item.Node.Id);
                                    if (!existId)
                                    {
                                        organisationIds.Add(item.Node.Id);
                                    }

                                }
                            }
                        }
                    }

                    Log.Info($"ids count = {organisationIds.Count}");
                    List<string> addedReportId = new List<string>();
                    foreach (var item in organisationIds)
                    {
                        var photoReports = proGloveClient.GetReportsAsync(token, item).Result;
                        if (photoReports != null)
                        {
                            Log.Info($"reports count = {photoReports.Items.Count}");

                            foreach (var report in photoReports.Items)
                            {
                                if (!addedReportId.Contains(report.Id))
                                {

                                    objectElement = new CustomListObjectElement();
                                    objectElement.Add("Id", $"{report.Id ?? "null"}");
                                    objectElement.Add("Title", $"{report.Title ?? "null"}");
                                    objectElement.Add("ReportType", $"{report.ReportType ?? "null"}");
                                    objectElement.Add("Status", $"{report.Status ?? "null"}");
                                    objectElement.Add("TimeCreated", report.TimeCreated);
                                    objectElement.Add("DeviceSerial", $"{report.DeviceSerial ?? "null"}");
                                    objectElement.Add("PhotosCount", report.PhotosCount);
                                    objectElement.Add("Path", $"{report.Path ?? "null"}");
                                    if (report.Thumbnail != null)
                                    {
                                        objectElement.Add("ThumbnailUrl", $"{report.Thumbnail.Url ?? "null"}");
                                        objectElement.Add("ThumbnailAttachmentSortKey",
                                            report.Thumbnail?.AttachmentSortKey);
                                    }
                                    else
                                    {
                                        objectElement.Add("ThumbnailUrl", "null");
                                        objectElement.Add("ThumbnailAttachmentSortKey", "null");
                                    }

                                    objectElement.Add("Next", $"{photoReports.Links?.Next ?? "null"}");
                                    objectElement.Add("Previous", $"{photoReports.Links?.Previous ?? "null"}");
                                    if (photoReports.Metadata != null)
                                    {
                                        objectElement.Add("Description",
                                            $"{photoReports.Metadata.Description ?? "null"}");
                                        objectElement.Add("Size", photoReports.Metadata.Size);
                                        objectElement.Add("Filters",
                                            JsonConvert.SerializeObject(photoReports.Metadata.Filters ??
                                                                        new List<Filter>()));
                                        objectElement.Add("Search",
                                            JsonConvert.SerializeObject(photoReports.Metadata.Search ??
                                                                        new List<object>()));
                                        objectElement.Add("Sort",
                                            JsonConvert.SerializeObject(photoReports.Metadata.Sort ??
                                                                        new List<Sort>()));
                                    }
                                    else
                                    {
                                        objectElement.Add("Description", "null");
                                        objectElement.Add("Size", null);
                                        objectElement.Add("Filters", "null");
                                        objectElement.Add("Search", "null");
                                        objectElement.Add("Sort", "null");
                                    }

                                    customList.Add(objectElement);
                                    addedReportId.Add(report.Id);
                                    Log.Info($"CustomList count = {customList.Count}");
                                }

                            }
                        }
                    }

                    return customList;
                }
            }
           
            catch (Exception e)
            {
                Log.Error(e.ToString());
                throw;
            }
        }
    }
}
