using Peakboard.ExtensionKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Security.Policy;
namespace HubSpot
{
    [Serializable]
    [ExtensionIcon("HubSpot.icon.png")]
    public class HubSpotCustomList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "HubSpotCustomList",
                Name = "HubSpot API",
                Description = "Interface with HubSpot API",
                PropertyInputPossible = true,
                PropertyInputDefaults =
                {
                    new CustomListPropertyDefinition() { Name = "Token", Value = "Enter your API token here", Masked = true }
                }
            };
        }
        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            var resData = GetResult(data.Properties["Token"]).Result;
            if (resData != null)
            {
                var column = new CustomListColumnCollection();        
                var columnNamesAndTypes = GetNamesAndTypes(resData);                
                foreach (var item in columnNamesAndTypes)
                {
                    column.Add(new CustomListColumn(item.Key, item.Value));
                }
                column.Add(new CustomListColumn("Emails",CustomListColumnTypes.String));
                return column;   
            }
            return null;
        }
        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            string token = data.Properties["Token"];
            var result = GetResult(token).Result;
            string id = "";          
            if (result != null)
            {
                var items = new CustomListObjectElementCollection();
                var allproperties = GetPropertiesWithValues(result);
                if (allproperties != null) 
                { 
                    int rowCount = allproperties.First().Value.Count;
                    for (int i = 0; i < rowCount; i++)
                    {
                        var customElement = new CustomListObjectElement();                  
                        foreach (var key in allproperties.Keys)
                        {                        
                            var value = allproperties[key][i];
                            if (key == "id")
                            {
                                id =value.ToString();
                            }
                            if (value is JValue jValue)
                            {
                                customElement.Add(key, jValue.Value);
                            }
                            else
                            {
                                customElement.Add(key, value);
                            }
                        }
                          if (id.Length > 0)
                            {
                                string emails = GetEmails(token, id);
                                customElement.Add("Emails", emails);
                            }
                        items.Add(customElement);                       
                    }                    
                    return items;
                }
            }
            return null;
        }
        private Dictionary<string,List<object>> GetPropertiesWithValues(JToken results)
        {
            Dictionary<string, List<object>> result = new Dictionary<string, List<object>>();
            var names = GetNamesAndTypes(results);
            foreach (var name in names)
            {
                var values = GetValues(results, name.Key);
                result.Add(name.Key, values);
            }
            return result;
        }
        private List<object> GetValues(JToken results, string propertyName)
        {
            var res = new List<object>();
            foreach (var result in results)
            {
                if (result[propertyName] != null)
                {
                    res.Add(result[propertyName]);
                }
                else
                {
                    var properties = result["properties"] as JObject;
                    if (properties != null && properties[propertyName] != null)
                    {
                        res.Add(properties[propertyName]);
                    }
                }
            }
            return res;
        }
        private Dictionary<string,CustomListColumnTypes> GetNamesAndTypes(JToken results)
        {
            var ticket = results[0];
            Dictionary<string, CustomListColumnTypes> names = new Dictionary<string, CustomListColumnTypes>();
            foreach (var property in ticket.Children<JProperty>())
            {
                if (property.Name == "properties")
                {
                    var properties = property.Value as JObject;
                    foreach (var value in properties.Properties())
                    {
                        var columnType = GetCustomListColumnTypes(value.Value);
                        names.Add(value.Name,columnType);                    
                    }
                }
                else
                {
                    var columnType = GetCustomListColumnTypes(property.Value);
                    names.Add(property.Name, columnType);
                }
            }
            return names;
        }
        protected async Task<JToken> GetResult(string token)
        {
            var customPropertyNames = GetCustomPropertys(token,"name");
            string queryProperty = string.Join(",", customPropertyNames);
            string url = $"https://api.hubapi.com/crm/v3/objects/tickets?properties={queryProperty}";
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                var responce = client.GetAsync(url).Result;
                if (responce.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    var responceBody = await responce.Content.ReadAsStringAsync();
                    var jsonObject = JObject.Parse(responceBody);
                    var result = jsonObject["results"];
                    return result;                 
                }
                throw new Exception($"HTTP Error: {responce.StatusCode} - {responce.ReasonPhrase}");
                
            }
        }
        private CustomListColumnTypes GetCustomListColumnTypes(JToken jToken)
        {
            switch (jToken.Type) 
            {              
                case JTokenType.Boolean:
                    return CustomListColumnTypes.Boolean;           
                case JTokenType.Float:
                    return CustomListColumnTypes.Number;
                case JTokenType.Integer:
                    return CustomListColumnTypes.Number;
                default:
                    return CustomListColumnTypes.String;            
            }
        }
        private List<string> GetCustomPropertys(string token,string property)
        {
            using (HttpClient client = new HttpClient())
            {
                string url = "https://api.hubapi.com/crm/v3/properties/ticket/";
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                var responce = client.GetAsync(url).Result;
                JObject json = JObject.Parse(responce.Content.ReadAsStringAsync().Result);
                JArray properties = (JArray)json["results"];
                List<string> values = new List<string>();
                foreach (var item in properties)
                {
                    string name = (string)item[property];
                    values.Add(name);
                }
                return values;
            }
        }
        string GetEmails(string token,string id)
        {
            string url = $"https://api.hubapi.com/engagements/v1/engagements/associated/ticket/{id}/";
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                var responce = client.GetAsync(url).Result;
                var res = responce.Content.ReadAsStringAsync().Result;
                JObject responceJson = JObject.Parse(res);
                JArray properties = (JArray)responceJson["results"];
                StringBuilder sb = new StringBuilder();
                StringBuilder stringBuilder = new StringBuilder();
                foreach (var item in properties)
                {
                    if (item["engagement"]["type"].ToString() == "EMAIL")
                    {
                        sb.Append("    Answer    " + (string)item["engagement"]["bodyPreview"]);
                    }
                    else if (item["engagement"]["type"].ToString() == "INCOMING_EMAIL")
                    {
                        sb.Append("      INCOMING_EMAIL      " + (string)item["engagement"]["bodyPreview"]);
                    }
                }
                return sb.ToString();

            }
           
           
        }
    }
}
