using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Peakboard.ExtensionKit;
using System.Data;

namespace PeakboardExtensionYarooms
{
    [Serializable]
    class YaroomsCustomList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = $"YaroomsCustomList",
                Name = "Yarooms List",
                Description = "Returns data from Yarooms",
                PropertyInputPossible = true,
                PropertyInputDefaults = {
                    new CustomListPropertyDefinition() { Name = "Subdomain", Value = "" },
                    new CustomListPropertyDefinition() { Name = "Email", Value = "" },
                    new CustomListPropertyDefinition() { Name = "Password", Masked = true, Value="" },
                },
            };
        }

        protected override void CheckDataOverride(CustomListData data)
        {
            CheckProperties(data, out string Subdomain, out string Email, out string Password);
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            return new CustomListColumnCollection
            {
                new CustomListColumn("Id", CustomListColumnTypes.String),
                new CustomListColumn("Date", CustomListColumnTypes.String),
                new CustomListColumn("StartTime", CustomListColumnTypes.String),
                new CustomListColumn("EndTime", CustomListColumnTypes.String),
                new CustomListColumn("Location", CustomListColumnTypes.String),
                new CustomListColumn("Room", CustomListColumnTypes.String),
                new CustomListColumn("Name", CustomListColumnTypes.String),
                new CustomListColumn("Description", CustomListColumnTypes.String),
            };
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            CheckProperties(data, out string Subdomain, out string Email, out string Password);
            List<YaroomsMeeting> mymeetings = YaroomsHelper.GetAllMeetings(Subdomain, Email, Password);

            var items = new CustomListObjectElementCollection();

            foreach(var meeting in mymeetings)
            {
                items.Add(new CustomListObjectElement { { "Id", meeting.id },
                    { "Date", meeting.date },
                    { "StartTime", meeting.start },
                    { "EndTime", meeting.end },
                    { "Location", meeting.location },
                    { "Room", meeting.room },
                    { "Name", meeting.name },
                    { "Description", meeting.description },
                });
            }
            
            this.Log?.Info(string.Format("Ingres extension fetched {0} rows.", items.Count));

            return items;
        }


        private void CheckProperties(CustomListData data, out string Subdomain, out string Email, out string Password)
        {
            data.Properties.TryGetValue("Subdomain", StringComparison.OrdinalIgnoreCase, out Subdomain);
            data.Properties.TryGetValue("Email", StringComparison.OrdinalIgnoreCase, out Email);
            data.Properties.TryGetValue("Password", StringComparison.OrdinalIgnoreCase, out Password);

            if (string.IsNullOrWhiteSpace(Subdomain) || string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                throw new InvalidOperationException("Invalid properties. Please check carefully!");
            }
        }
    }
}
