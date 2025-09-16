using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PeakboardExtensionYarooms
{
    public class YaroomsHelper
    {
        private static string myCachedToken = "";
        public static string GetToken(string subdomain, string email, string password)
        {
            if (!string.IsNullOrWhiteSpace(myCachedToken))
                return myCachedToken;

            Dictionary<string, string> values = new Dictionary<string, string>
            {
                { "subdomain", subdomain },
                { "email", email },
                { "password", password }
            };

            HttpContent content = new FormUrlEncodedContent(values);
            HttpClient client1 = new HttpClient();
            HttpResponseMessage response = client1.PostAsync("https://api.yarooms.com/auth", content).Result;
            string responseBody = response.Content.ReadAsStringAsync().Result;

            JObject dynobj = JObject.Parse(responseBody);
            string token;

            if (dynobj.ContainsKey("error"))
            {
                throw new InvalidOperationException("Yarooms returned: " + dynobj["error"]);
            }
            else
            {
                token = dynobj["data"]["token"]?.ToString();
            }

            return token;
        }

        public static List<YaroomsMeeting>  GetAllMeetings(string subdomain, string email, string password)
        {
            string token = YaroomsHelper.GetToken(subdomain, email, password);

            YaroomsHelper.GetMeetingRooms(token);
            Dictionary<string, string> locations = YaroomsHelper.GetLocations(token);
            List<YaroomsMeeting> meetings = new List<YaroomsMeeting>();

            foreach (var location in locations)
                YaroomsHelper.AddMeetingsFromLocationToList(token, location.Key, meetings);

            return meetings;
        }

        public static Dictionary<string, string> MyCachedMeetingRooms = new Dictionary<string, string>();
        public static Dictionary<string, string> GetMeetingRooms(string token)
        {
            if (MyCachedMeetingRooms.Count > 0)
                return MyCachedMeetingRooms;

            HttpClient client1 = new HttpClient();
            client1.DefaultRequestHeaders.Add("X-Token", token);
            HttpResponseMessage response = client1.GetAsync("https://api.yarooms.com/rooms").Result;
            string responseBody = response.Content.ReadAsStringAsync().Result;

            JObject dynobj = JObject.Parse(responseBody);

            if (dynobj.ContainsKey("error"))
            {
                throw new InvalidOperationException("Yarooms returned: " + dynobj["error"]);
            }
            else
            {

                foreach (var room in dynobj["data"]["list"])
                {
                    string roomid = room["id"]?.ToString();
                    string roomname = room["name"]?.ToString();
                    MyCachedMeetingRooms.Add(roomid, roomname);
                }
            }

            return MyCachedMeetingRooms;
        }

        public static Dictionary<string, string> MyCachedLocations = new Dictionary<string, string>();
        public static Dictionary<string, string> GetLocations(string token)
        {
            if (MyCachedLocations.Count > 0)
                return MyCachedLocations;

            HttpClient client1 = new HttpClient();
            client1.DefaultRequestHeaders.Add("X-Token", token);
            HttpResponseMessage response = client1.GetAsync("https://api.yarooms.com/locations").Result;
            string responseBody = response.Content.ReadAsStringAsync().Result;

            JObject dynobj = JObject.Parse(responseBody);

            if (dynobj.ContainsKey("error"))
            {
                throw new InvalidOperationException("Yarooms returned: " + dynobj["error"]);
            }
            else
            {

                foreach (var room in dynobj["data"]["list"])
                {
                    string roomid = room["id"]?.ToString();
                    string roomname = room["name"]?.ToString();
                    MyCachedLocations.Add(roomid, roomname);
                }
            }

            return MyCachedLocations;
        }

        private static void AddMeetingsFromLocationToList(string token, string location, List<YaroomsMeeting> ret)
        {
            HttpClient client1 = new HttpClient();
            client1.DefaultRequestHeaders.Add("X-Token", token);
            HttpResponseMessage response = client1.GetAsync(string.Format("https://api.yarooms.com/meetings?scope[where]=location:{0}&scope[when]=week:{1}", location, GetMyCurrentDate())).Result;
            string responseBody = response.Content.ReadAsStringAsync().Result;

            JObject dynobj = JObject.Parse(responseBody);

            if (dynobj.ContainsKey("error"))
            {
                throw new InvalidOperationException("Yarooms returned: " + dynobj["error"]);
            }
            else
            {

                foreach (var room in dynobj["data"]["list"])
                {
                    string id = room["id"]?.ToString();
                    string room_id = room["room_id"]?.ToString();
                    string name = room["name"]?.ToString();
                    string description = room["description"]?.ToString();
                    string start = room["start"]?.ToString();
                    string end = room["end"]?.ToString();
                    ret.Add(new YaroomsMeeting(id, location, room_id, name, description, start, end));
                }
            }
        }

        private static string mycacheddate = "";

        public static string GetMyCurrentDate()
        {
            if (!string.IsNullOrWhiteSpace(mycacheddate))
                return mycacheddate;
            HttpClient client1 = new HttpClient();
            HttpResponseMessage response = client1.GetAsync("http://worldtimeapi.org/api/ip").Result;
            string responseBody = response.Content.ReadAsStringAsync().Result;

            JObject dynobj = JObject.Parse(responseBody);
            DateTime mydate = (DateTime)dynobj["datetime"];
            mycacheddate = mydate.ToString("yyyy-MM-dd");
            return mycacheddate;
        }
    }

    public class YaroomsMeeting
    {
        public YaroomsMeeting(string id, string location_id, string room_id, string name, string description, string start, string end)
        {
            this.id = id;
            this.room = room_id;
            if (YaroomsHelper.MyCachedMeetingRooms.ContainsKey(this.room))
                this.room = YaroomsHelper.MyCachedMeetingRooms[this.room];
            this.location = location_id;
            if (YaroomsHelper.MyCachedLocations.ContainsKey(this.location))
                this.location = YaroomsHelper.MyCachedLocations[this.location];
            this.name = name;
            this.description = description;
            this.start = start.Substring(11, 5);
            this.end = end.Substring(11, 5);
            this.date = start.Substring(0, 10);

        }
        public string id;
        public string location;
        public string room;
        public string name;
        public string description;
        public string start;
        public string end;
        public string date;

        public override string ToString()
        {
            return date + " - " + location + " - " + room + " - " + name;
        }
    }
}
