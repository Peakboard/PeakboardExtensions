using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Peakboard.ExtensionKit;

namespace HubSpot;

[Serializable]
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
                new CustomListPropertyDefinition() { Name = "Token", Value = "", Masked = true }
            }
        };
    }

    protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
    {
        try
        {
            var token = GetToken(data);
            if (string.IsNullOrWhiteSpace(token))
                return GetErrorColumns();

            var resData = GetTickets(token).Result;
            if (resData == null || resData.Type != JTokenType.Array || !resData.Any())
                return GetErrorColumns();

            var columns = new CustomListColumnCollection();
            var columnNamesAndTypes = GetNamesAndTypesSafe(resData);
            if (columnNamesAndTypes.Count == 0)
                return GetErrorColumns();

            foreach (var item in columnNamesAndTypes)
                columns.Add(new CustomListColumn(item.Key, item.Value));

            columns.Add(new CustomListColumn("Emails", CustomListColumnTypes.String));
            columns.Add(new CustomListColumn("_Status", CustomListColumnTypes.String));
            columns.Add(new CustomListColumn("_Message", CustomListColumnTypes.String));

            return columns;
        }
        catch (HubSpotApiException hex)
        {
            Log.Error($"HubSpot.GetColumns API error: {(int)hex.StatusCode} {hex.StatusCode} - {hex.Message}");
            return GetErrorColumns();
        }
        catch (Exception ex)
        {
            Log.Error($"HubSpot.GetColumns failed: {ex.Message}");
            return GetErrorColumns();
        }
    }

    protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
    {
        try
        {
            var token = GetToken(data);
            if (string.IsNullOrWhiteSpace(token))
                return CreateErrorItems("ERROR", "HubSpot token is missing. Please enter a valid Private App Token.");

            var result = GetTickets(token).Result;
            if (result == null || result.Type != JTokenType.Array)
                return CreateErrorItems("ERROR", "HubSpot returned an unexpected response format.");

            if (!result.Any())
                return CreateErrorItems("OK", "No tickets returned from HubSpot.");

            var items = new CustomListObjectElementCollection();

            var allproperties = GetPropertiesWithValuesSafe(result);
            if (allproperties.Count == 0)
                return CreateErrorItems("ERROR", "HubSpot returned data but no readable properties were found.");

            var first = allproperties.FirstOrDefault();
            if (first.Value == null)
                return CreateErrorItems("ERROR", "HubSpot returned data but row parsing failed.");

            int rowCount = first.Value.Count;
            if (rowCount <= 0)
                return CreateErrorItems("OK", "No ticket rows returned from HubSpot.");

            for (int i = 0; i < rowCount; i++)
            {
                var customElement = new CustomListObjectElement();
                string id = "";

                foreach (var key in allproperties.Keys)
                {
                    var list = allproperties[key] ?? new List<object>();
                    var value = i < list.Count ? list[i] : "";

                    if (key == "id")
                        id = SafeToString(value);

                    customElement.Add(key, ToSimpleValue(value));
                }

                string emails = "";
                if (!string.IsNullOrWhiteSpace(id))
                    emails = GetEmailsSafe(token, id);

                customElement.Add("Emails", emails);
                customElement.Add("_Status", "OK");
                customElement.Add("_Message", "");

                items.Add(customElement);
            }

            return items;
        }
        catch (HubSpotApiException hex)
        {
            Log.Error($"HubSpot.GetItems API error: {(int)hex.StatusCode} {hex.StatusCode} - {hex.Message}");
            return CreateErrorItems("ERROR", hex.Message);
        }
        catch (Exception ex)
        {
            Log.Error($"HubSpot.GetItems failed: {ex.Message}");
            return CreateErrorItems("ERROR", "Unexpected error while loading HubSpot tickets. Please check the log for details.");
        }
    }

    private static string GetToken(CustomListData data)
    {
        try
        {
            return data?.Properties?["Token"] ?? "";
        }
        catch
        {
            return "";
        }
    }

    private static CustomListColumnCollection GetErrorColumns()
    {
        return
        [
            new CustomListColumn("_Status", CustomListColumnTypes.String),
            new CustomListColumn("_Message", CustomListColumnTypes.String)
        ];
    }

    private static CustomListObjectElementCollection CreateErrorItems(string status, string message)
    {
        var item = new CustomListObjectElement
        {
            { "_Status", status ?? "ERROR" },
            { "_Message", message ?? "" }
        };

        return new CustomListObjectElementCollection { item };
    }

    private static object ToSimpleValue(object value)
    {
        if (value == null)
            return "";

        if (value is JValue jv)
        {
            if (jv.Type == JTokenType.Boolean)
                return jv.Value<bool>();
            if (jv.Type == JTokenType.Integer || jv.Type == JTokenType.Float)
                return jv.Value<double>();
            return jv.Value?.ToString() ?? "";
        }

        if (value is JToken jt)
        {
            if (jt.Type == JTokenType.Boolean)
                return jt.Value<bool>();
            if (jt.Type == JTokenType.Integer || jt.Type == JTokenType.Float)
                return jt.Value<double>();
            return jt.ToString();
        }

        if (value is bool)
            return value;

        if (value is int or long or float or double or decimal)
            return Convert.ToDouble(value);

        return value.ToString() ?? "";
    }

    private static string SafeToString(object value)
    {
        if (value == null)
            return "";
        if (value is JValue jv)
            return jv.Value?.ToString() ?? "";
        if (value is JToken jt)
            return jt.ToString();
        return value.ToString() ?? "";
    }

    private Dictionary<string, List<object>> GetPropertiesWithValuesSafe(JToken results)
    {
        var dict = new Dictionary<string, List<object>>();
        try
        {
            var names = GetNamesAndTypesSafe(results);
            foreach (var name in names)
            {
                dict[name.Key] = GetValuesSafe(results, name.Key);
            }
        }
        catch (Exception ex)
        {
            Log.Error($"HubSpot.GetPropertiesWithValues failed: {ex.Message}");
        }
        return dict;
    }

    private static List<object> GetValuesSafe(JToken results, string propertyName)
    {
        var res = new List<object>();
        if (results == null || string.IsNullOrWhiteSpace(propertyName))
            return res;

        foreach (var row in results)
        {
            try
            {
                if (row == null)
                {
                    res.Add("");
                    continue;
                }

                var direct = row[propertyName];
                if (direct != null)
                {
                    res.Add(direct);
                    continue;
                }

                var props = row["properties"] as JObject;
                if (props != null && props[propertyName] != null)
                    res.Add(props[propertyName]);
                else
                    res.Add("");
            }
            catch
            {
                res.Add("");
            }
        }

        return res;
    }

    private Dictionary<string, CustomListColumnTypes> GetNamesAndTypesSafe(JToken results)
    {
        var names = new Dictionary<string, CustomListColumnTypes>();
        try
        {
            if (results == null || results.Type != JTokenType.Array || !results.Any())
                return names;

            var first = results.First;
            if (first == null)
                return names;

            foreach (var property in first.Children<JProperty>())
            {
                if (property == null)
                    continue;

                if (property.Name == "properties")
                {
                    var properties = property.Value as JObject;
                    if (properties == null)
                        continue;

                    foreach (var value in properties.Properties())
                    {
                        if (value == null)
                            continue;

                        var columnType = GetCustomListColumnTypes(value.Value);
                        if (!names.ContainsKey(value.Name))
                            names.Add(value.Name, columnType);
                    }
                }
                else
                {
                    var columnType = GetCustomListColumnTypes(property.Value);
                    if (!names.ContainsKey(property.Name))
                        names.Add(property.Name, columnType);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"HubSpot.GetNamesAndTypes failed: {ex.Message}");
        }

        return names;
    }

    private static CustomListColumnTypes GetCustomListColumnTypes(JToken jToken)
    {
        if (jToken == null)
            return CustomListColumnTypes.String;

        return jToken.Type switch
        {
            JTokenType.Boolean => CustomListColumnTypes.Boolean,
            JTokenType.Float => CustomListColumnTypes.Number,
            JTokenType.Integer => CustomListColumnTypes.Number,
            _ => CustomListColumnTypes.String,
        };
    }

    private async Task<JToken> GetTickets(string token)
    {
        var customPropertyNames = GetCustomProperties(token, "name");
        if (customPropertyNames == null || customPropertyNames.Count == 0)
            throw new HubSpotApiException(HttpStatusCode.Forbidden, "Unable to read ticket properties from HubSpot. Please verify token permissions (required scopes for tickets).", "");

        string queryProperty = string.Join(",", customPropertyNames);
        string url = $"https://api.hubapi.com/crm/v3/objects/tickets?limit=100&properties={queryProperty}";

        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);

        var response = client.GetAsync(url).Result;
        var body = await response.Content.ReadAsStringAsync();

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new HubSpotApiException(response.StatusCode, "Unauthorized (401): The provided token is invalid or expired. Please enter a valid HubSpot Private App Token.", body);

        if (response.StatusCode == HttpStatusCode.Forbidden)
            throw new HubSpotApiException(response.StatusCode, "Forbidden (403): The token does not have permission to read tickets. Please add the required scopes to your HubSpot Private App.", body);

        if (!response.IsSuccessStatusCode)
            throw new HubSpotApiException(response.StatusCode, $"HubSpot request failed: {(int)response.StatusCode} {response.ReasonPhrase}", body);

        var jsonObject = JObject.Parse(body);
        return jsonObject["results"];
    }

    private List<string> GetCustomProperties(string token, string property)
    {
        using var client = new HttpClient();
        string url = "https://api.hubapi.com/crm/v3/properties/ticket/";
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);

        var response = client.GetAsync(url).Result;
        var body = response.Content.ReadAsStringAsync().Result;

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new HubSpotApiException(response.StatusCode, "Unauthorized (401): The provided token is invalid or expired. Please enter a valid HubSpot Private App Token.", body);

        if (response.StatusCode == HttpStatusCode.Forbidden)
            throw new HubSpotApiException(response.StatusCode, "Forbidden (403): The token does not have permission to read ticket properties. Please add the required scopes to your HubSpot Private App.", body);

        if (!response.IsSuccessStatusCode)
            throw new HubSpotApiException(response.StatusCode, $"HubSpot request failed while reading ticket properties: {(int)response.StatusCode} {response.ReasonPhrase}", body);

        JObject json = JObject.Parse(body);
        JArray properties = (JArray)json["results"];

        var values = new List<string>();
        if (properties == null)
            return values;

        foreach (var item in properties)
        {
            try
            {
                string name = (string)item[property];
                if (!string.IsNullOrWhiteSpace(name))
                    values.Add(name);
            }
            catch
            {
                // ignore broken entries
            }
        }

        return values;
    }

    private string GetEmailsSafe(string token, string id)
    {
        try
        {
            return GetEmails(token, id);
        }
        catch (HubSpotApiException hex)
        {
            Log.Error($"HubSpot email load failed for ticket {id}: {(int)hex.StatusCode} {hex.StatusCode} - {hex.Message}");
            return "";
        }
        catch (Exception ex)
        {
            Log.Error($"HubSpot email load failed for ticket {id}: {ex.Message}");
            return "";
        }
    }

    private string GetEmails(string token, string id)
    {
        string url = $"https://api.hubapi.com/engagements/v1/engagements/associated/ticket/{id}/";
        using var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);

        var response = client.GetAsync(url).Result;
        var body = response.Content.ReadAsStringAsync().Result;

        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new HubSpotApiException(response.StatusCode, "Unauthorized (401): The provided token is invalid or expired.", body);

        if (response.StatusCode == HttpStatusCode.Forbidden)
            throw new HubSpotApiException(response.StatusCode, "Forbidden (403): The token does not have permission to read engagements/emails.", body);

        if (!response.IsSuccessStatusCode)
            throw new HubSpotApiException(response.StatusCode, $"HubSpot request failed while reading emails: {(int)response.StatusCode} {response.ReasonPhrase}", body);

        JObject responseJson = JObject.Parse(body);
        var results = responseJson["results"] as JArray;
        if (results == null || results.Count == 0)
            return "";

        var sb = new StringBuilder();
        foreach (var item in results)
        {
            try
            {
                var type = item?["engagement"]?["type"]?.ToString() ?? "";
                var preview = item?["engagement"]?["bodyPreview"]?.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(preview))
                    continue;

                if (type == "EMAIL")
                    sb.Append("Answer: " + preview + " ");
                else if (type == "INCOMING_EMAIL")
                    sb.Append("Incoming email: " + preview + " ");
            }
            catch
            {
                // ignore single broken engagement entries
            }
        }

        return sb.ToString().Trim();
    }
}

[Serializable]
internal class HubSpotApiException : Exception
{
    public HttpStatusCode StatusCode { get; }
    public string ResponseBody { get; }

    public HubSpotApiException(HttpStatusCode statusCode, string message, string responseBody) : base(message)
    {
        StatusCode = statusCode;
        ResponseBody = responseBody ?? "";
    }
}
