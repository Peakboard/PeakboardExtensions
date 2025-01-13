using Newtonsoft.Json;
using Peakboard.ExtensionKit;
using ProGlove;
using ProGlove.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading.Tasks;

namespace ProGloveExtension.CustomLists
{
    [Serializable]
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
                    new CustomListPropertyDefinition(){Name = "ClientId"},
                    new CustomListPropertyDefinition(){Name = "BasedUrl"},
                    new CustomListPropertyDefinition(){Name = "Username"},
                    new CustomListPropertyDefinition(){Name = "Password"}
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
            var customList = new CustomListObjectElementCollection();
            CustomListObjectElement objectElement = null;
            ProGloveClient proGloveClient = new ProGloveClient(data.Properties["BasedUrl"], data.Properties["ClientId"]);
            var ath = proGloveClient.GetAuthenticationResponseAsync(data.Properties["Username"], data.Properties["Password"]).Result;
            if (ath == null)
            {
                Log.Error("Incorrect authorization data");
                return new CustomListObjectElementCollection();
            }
            string token = ath.AuthenticationResult.IdToken;
            var gateways =  proGloveClient.GetGatewaysOrganisationAsync(token).Result;
            List<string> organisationIds = new List<string>();
            if (gateways != null)
            {
                foreach (var item in gateways.Items)
                {
                    if (item.Node!=null)
                    {
                        if (!string.IsNullOrEmpty(item.Node.Id))
                        {
                            organisationIds.Add(item.Node.Id);
                        }
                    }
                }
            }
            Log.Info($"ids count = {organisationIds.Count}");
            foreach (var item in organisationIds)
            {
                var photoReports = proGloveClient.GetReportsAsync(token, item).Result;
                if (photoReports != null)
                {
                    Log.Info($"reports count = {photoReports.Items.Count}");

                    foreach (var report in photoReports.Items)
                    {
                        objectElement = new CustomListObjectElement();
                        objectElement.Add("Id", $"{report.Id ?? "null"}");
                        objectElement.Add("Title", $"{report.Title ?? "null"}");
                        objectElement.Add("ReportType", $"{report.ReportType ?? "null"}");
                        objectElement.Add("Status", $"{report.Status ?? "null"}");
                        long timestamp = report.TimeCreated;
                        DateTimeOffset dateTime = DateTimeOffset.FromUnixTimeMilliseconds(timestamp);
                        objectElement.Add("TimeCreated", $"{dateTime.ToString("dd-MM-yyyy HH:mm:ss")}");
                        objectElement.Add("DeviceSerial", $"{report.DeviceSerial ?? "null"}");
                        objectElement.Add("PhotosCount", $"{report.PhotosCount}");
                        objectElement.Add("Path", $"{report.Path ?? "null"}");
                        if (report.Thumbnail != null)
                        {
                            objectElement.Add("ThumbnailUrl", $"{report.Thumbnail.Url ?? "null"}");
                            objectElement.Add("ThumbnailAttachmentSortKey", $"{report.Thumbnail.AttachmentSortKey}");
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
                            objectElement.Add("Description", $"{photoReports.Metadata.Description ?? "null"}");
                            objectElement.Add("Size", $"{photoReports.Metadata.Size}");
                            objectElement.Add("Filters", JsonConvert.SerializeObject(photoReports.Metadata.Filters ?? new List<Filter>()));
                            objectElement.Add("Search", JsonConvert.SerializeObject(photoReports.Metadata.Search ?? new List<object>()));
                            objectElement.Add("Sort", JsonConvert.SerializeObject(photoReports.Metadata.Sort ?? new List<Sort>()));
                        }
                        else
                        {
                            objectElement.Add("Description", "null");
                            objectElement.Add("Size", "null");
                            objectElement.Add("Filters", "null");
                            objectElement.Add("Search", "null");
                            objectElement.Add("Sort", "null");
                        }
                        customList.Add(objectElement);
                        Log.Info($"CustomList count = {customList.Count}");
                    }
                }
            }
            return customList;
        }
    }
}
