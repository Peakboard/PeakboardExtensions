using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PeakboardExtensionMonday
{
    public static class JsonExtensions
    {
        public static List<JToken> FindTokens(this JToken containerToken, string name)
        {
            List<JToken> matches = new List<JToken>();
            FindTokens(containerToken, name, matches);
            return matches;
        }

        private static void FindTokens(JToken containerToken, string name, List<JToken> matches)
        {
            if (containerToken.Type == JTokenType.Object)
            {
                foreach (JProperty child in containerToken.Children<JProperty>())
                {
                    if (child.Name == name)
                    {
                        matches.Add(child.Value);
                    }
                    FindTokens(child.Value, name, matches);
                }
            }
            else if (containerToken.Type == JTokenType.Array)
            {
                foreach (JToken child in containerToken.Children())
                {
                    FindTokens(child, name, matches);
                }
            }
        }

        public static string GetLastAttribute(JObject jObject)
        {
            JToken getLastAttribute = jObject;
            string lastAttribute = "";
            while (getLastAttribute.HasValues)
            {
                if (getLastAttribute.Type == JTokenType.Array)
                {
                    lastAttribute = getLastAttribute.Path;
                }
                getLastAttribute = getLastAttribute.Children().First();
            }

            while (lastAttribute.Contains("."))
            {
                lastAttribute = lastAttribute.Substring(lastAttribute.IndexOf(".") + 1).Trim();
            }

            return lastAttribute;
        }

        public static string GetParentName(string parentName)
        {
            while (parentName.Contains("."))
            {
                parentName = parentName.Substring(parentName.IndexOf(".") + 1).Trim();
            }
            parentName = Regex.Replace(parentName, @"\d", "");
            parentName = parentName.Replace("[]", "");
            return parentName;
        }
    }
}
