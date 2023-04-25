using System.Collections.Generic;

namespace GraphFunctionTestConsole
{
    public class RequestData
    {
        public static string Permissions = "user.read offline_access chat.read sites.read.all onlinemeetings.read";

        public static  string[] Requests =
        {
            /*"activites",*/
            "calendar", 
            "calendars",    
            "calendarGroups",
            "chats",
            "contactFolders",
            "contacts",
            "createdObjects",
            "drive",
            "drives",
            "events",
            "followedSites",
            "insights",
            "mailFolders",
            /*"manager",*/
            "memberOf",
            "messages",
            "onenote",
            "onlineMeetings",
            "outlook",
            "ownedDevices",
            "ownedObjects",
            "people",
            "planner",
            "todo/lists",
            "todo/lists/tasks/tasks"
        };

        public static Dictionary<string, string[]> SelectParams = new Dictionary<string, string[]>()
        {
            /*{
                "activities", new[]
                {
                    "activationUrl", "id", "createdDateTime", "userTimezone", "contentUrl", "appDisplayName",
                    "expirationDateTime", "lastModifiedDateTime"
                }
            },*/
            {
                "calendar",
                new[] { "id", "canEdit", "isDefaultCalendar", "name", "events", "isDefaultCalendar", "hexColor" }
            },
            {
                "calendars", new[] { "id", "canEdit", "canShare", "owner", "name", "defaultOnlineMeetingProvider" }
            },
            {
                "calendarGroups", new[] { "id", "changeKey", "classId", "name", "calendars" }
            },
            {
                "chats", new[] { "id", "chatType", "topic", "members", "lastUpdatedDateTime" }
            },
            {
                "contactFolders", new[] { "id", "displayName", "childFolders", "parentFolderId" }
            },
            {
                "contacts",
                new[]
                {
                    "id", "createdDateTime", "birthday", "displayName", "emailAddresses", "officeLocation", "surname"
                }
            },
            {
                "createdObjects", new[] { "id", "deletedDateTime" }
            },
            {
                "drive",
                new[]
                {
                    "id", "createdBy", "description", "name", "root", "owner", "webUrl", "driveType", "createdByUser"
                }
            },
            {
                "drives", new[] { "id", "createdBy", "name", "parentReference", "root" }
            },
            {
                "events",
                new[]
                {
                    "id", "createdDateTime", "attendees", "hasAttachments", "importance", "start", "subject", "end"
                }
            },
            {
                "followedSites",
                new[] { "id", "createdBy", "description", "name", "displayName", "permissions", "drive" }
            },
            {
                "insights", new[] { "id", "shared", "trending", "used" }
            },
            {
                "mailFolders",
                new[] { "id", "displayName", "isHidden", "totalItemCount", "childFolders", "messageRules" }
            },
            /*{
                "manager", new[] { "id", "deletedDateTime" }
            },*/
            {
                "memberOf",
                new[]
                {
                    "id", "deletedDateTime", "classification", "createdDateTime", "securityEnabled", "preferredLanguage"
                }
            },
            {
                "messages", new[]
                {
                    "id", "bccRecipients", "flag", "from", "hasAttachments", "importance", "sentDateTime",
                    "subject", "receivedDateTime"
                }
            },
            {
                "onenote", new[] { "id", "notebooks", "resources", "sections", "sectionGroups", "pages", "operations" }
            },
            {
                "onlineMeetings",
                new[]
                {
                    "id", "allowMeetingChat", "chatInfo", "externalId", "participants", "startDateTime", "subject",
                    "recordAutomatically"
                }
            },
            {
                "outlook", new[] { "id", "masterCategories" }
            },
            {
                "ownedDevices", new[] { "id", "accountEnabled", "deviceId", "displayName", "model", "sourceType" }
            },
            {
                "ownedObjects", new[] { "id", "appid", "identifierUris", "tags", "samlMetadataUrl" }
            },
            {
                "people",
                new[]
                {
                    "id", "birthday", "companyName", "department", "displayName", "givenName", "imAddress",
                    "isFavorite", "surname", "userPrincipalName", "websites"
                }
            },
            {
                "planner", new[] { "id", "plans", "tasks" }
            },
            {
                "todo/lists",
                new[] { "id", "displayName", "isOwner", "isShared", "wellknownListName", "extensions", "tasks" }
            },
            {
                "todo/lists/tasks/tasks",
                new[]
                {
                    "id", "dueDateTime", "createdDateTime", "completedDateTime", "categories", "title", "status",
                    "reminderDateTime"
                }
            }
        };

        public static Dictionary<string, string[]> OrderByParams = new Dictionary<string, string[]>()
        {
            /*{
                "activities", new[]
                {
                    "id", "activationUrl", "appActivityId", "createdDateTime", "createdDateTime", "status",
                    "userTimezone"
                }
            },*/
            {
                "calendar",
                null
            },
            {
                "calendars", new[] { "canEdit", "canShare", "isRemovable", "name", "owner/name" }
            },
            {
                "calendarGroups", new[] { "name", "classId" }
            },
            {
                "chats", new[] { "chatType", "topic", "webUrl" }
            },
            {
                "contactFolders", new[] { "displayName", "parentFolderId" }
            },
            {
                "contacts",
                new[]
                {
                    "changeKey", "createdDateTime", "department", "givenName",
                    "surname"
                }
            },
            {
                "createdObjects", new[] { "deletedDateTime" }
            },
            {
                "drive",
                null
            },
            {
                "drives",
                new[] { "createdBy/user/displayName", "createdDateTime", "description", "name", "owner/user/displayName", "sharePointIds/webId" }
            },
            {
                "events",
                new[]
                {
                    "changeKey", "createdDateTime", "end/DateTime", "location/displayName",
                    "start/DateTime"
                }
            },
            {
                "followedSites",
                new[] { "createdBy", "createdDateTime", "lastModifiedBy", "name", "root" }
            },
            {
                "insights", null
            },
            {
                "mailFolders",
                new[] { "childFolderCount", "displayName", "isHidden", "totalItemCount" }
            },
            /*{
                "manager", null
            },*/
            {
                "memberOf",
                new[]
                {
                    "deletedDateTime"
                }
            },
            {
                "messages", new[]
                {
                    "createdDateTime", "flag", "from", "subject", "receivedDateTime"
                }
            },
            {
                "onenote", null
            },
            {
                "onlineMeetings",
                new[]
                {
                    "allowAttendeeToEnableCamera", "allowAttendeeToEnableMic", "chatInfo", "endDateTime",
                    "startDateTime", "subject"
                }
            },
            {
                "outlook", null
            },
            {
                "ownedDevices", new[] { "deletedDateTime" }
            },
            {
                "ownedObjects", new[] { "deletedDateTime" }
            },
            {
                "people",
                new[]
                {
                    "department", "surname", "displayName", "userPrincipalName"
                }
            },
            {
                "planner", null
            },
            {
                "todo/lists",
                new[] { "displayName", "isOwner", "isShared", "wellknownListName" }
            },
            {
                "todo/lists/tasks/tasks",
                new[]
                {
                    "bodyLastModifiedDateTime", "completedDateTime", "createdDateTime",
                    "startDateTime", "title"
                }
            }
        };

        public static Dictionary<string, string> FilterParams = new Dictionary<string, string>()
        {
            { "calendars", "canShare eq true" },
            { "calendarGroups", "startswith(name, 'M')" },
            { "chats", "topic eq 'Weekly'" },
            { "contacts", "startswith(displayname, 'B')" },
            { "drives", "driveType eq 'business'" },
            { "events", "start/dateTime lt '2023-04-04T09:00:00Z'" },
            { "mailFolders", "totalItemCount gt 1"},
            { "messages", "from/emailAddress/address eq 'notification@slack.com'" },
            { "people", "personType/class eq 'Person'" },
            { "todo/lists", "startswith(displayName, 'F')" },
            { "todo/lists/tasks/tasks", "status eq 'notStarted'" }
        };
    }
}