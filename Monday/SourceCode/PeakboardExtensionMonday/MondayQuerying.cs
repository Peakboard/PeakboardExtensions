using Newtonsoft.Json.Linq;
using Peakboard.ExtensionKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace PeakboardExtensionMonday
{
    [Serializable]
    [CustomListIcon("PeakboardExtensionMonday.monday_icon.png")]
    class MondayQuerying : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = $"MondayQuerying",
                Name = "Monday.com Querying",
                Description = "Returns data from Monday.com by using a GRAPHQL query",
                PropertyInputPossible = true,
            };
        }

        protected override FrameworkElement GetControlOverride()
        {
            // return an instance of the UI user control
            return new MondayQueryingUIControl();
        }

        protected override void CheckDataOverride(CustomListData data)
        {
            string url = data.Parameter.Split(';')[0];
            string token = data.Parameter.Split(';')[1];
            string query = data.Parameter.Split(';')[2];

            if (string.IsNullOrWhiteSpace(url))
            {
                throw new InvalidOperationException("Please provide an Url");
            }
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidOperationException("Please provide a Token");
            }
            if (string.IsNullOrWhiteSpace(query))
            {
                throw new InvalidOperationException("Please select a GraphQL Query");
            }

        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            CustomListColumnCollection columnCollection = new CustomListColumnCollection();

            string url = data.Parameter.Split(';')[0];
            string token = data.Parameter.Split(';')[1];
            string query = data.Parameter.Split(';')[2];

            try
            {
                var helper = new MondayService();
                string jsonString = helper.GetDataFromQuery(url, token, query).Result;
                JObject jObject = JObject.Parse(jsonString);
                string lastAttribute = JsonExtensions.GetLastAttribute(jObject);

                List<string> columnName = new List<string>();

                foreach (JToken t in jObject.FindTokens(lastAttribute).Children())
                {
                    foreach (var children in t.Children())
                    {
                        if ((children is IEnumerable<JToken> resultList))
                        {
                            if (resultList.Children().Children().Count() > 0)
                            {
                                foreach (JToken mondayItem in resultList.Children())
                                {
                                    foreach (JToken items in mondayItem.Children())
                                    {
                                        if (items.Type == JTokenType.Property)
                                        {
                                            var property = items as JProperty;
                                            string parentName = JsonExtensions.GetParentName(mondayItem.Path);
                                            if (!columnName.Contains(parentName + "." + property.Name))
                                                columnName.Add(parentName + "." + property.Name);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                if (children.Type == JTokenType.Property)
                                {
                                    var property = children as JProperty;
                                    string parentName = JsonExtensions.GetParentName(t.Path);
                                    if (!columnName.Contains(parentName + "." + property.Name))
                                        columnName.Add(parentName + "." + property.Name);

                                }
                            }
                        }
                    }
                }

                foreach (string column in columnName)
                {
                    columnCollection.Add(new CustomListColumn(column, CustomListColumnTypes.String));
                }
            }
            catch(Exception e1)
            {
                throw new InvalidOperationException("An error has occured while trying to retrieve the data. Please try again or change connection properties.");
            }
            

            

            return columnCollection;
        }



        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            CustomListObjectElementCollection itemsCollection = new CustomListObjectElementCollection();

            string url = data.Parameter.Split(';')[0];
            string token = data.Parameter.Split(';')[1];
            string query = data.Parameter.Split(';')[2];

            try
            {
                var helper = new MondayService();
                string jsonString = helper.GetDataFromQuery(url, token, query).Result;

                JObject jObject = JObject.Parse(jsonString);

                string lastAttribute = JsonExtensions.GetLastAttribute(jObject);

                bool isLooping = false;
                string loopName = "";
                string loopValue = "";

                foreach (JToken t in jObject.FindTokens(lastAttribute).Children().Children())
                {
                    if ((t is IEnumerable<JToken> resultList) && resultList.Children().Children().Count() > 0)
                    {
                        isLooping = true;
                        break;
                    }
                }

                if (isLooping)
                {
                    foreach (JToken t in jObject.FindTokens(lastAttribute).Children())
                    {
                        foreach (var children in t.Children())
                        {
                            if ((children is IEnumerable<JToken> resultList))
                            {
                                if (resultList.Children().Children().Count() > 0)
                                {

                                    foreach (JToken mondayItem in resultList.Children())
                                    {
                                        CustomListObjectElement item = new CustomListObjectElement();

                                        item.Add(loopName, loopValue);

                                        foreach (JToken items in mondayItem.Children())
                                        {
                                            if (items.Type == JTokenType.Property)
                                            {
                                                var property = items as JProperty;
                                                string parentName = JsonExtensions.GetParentName(mondayItem.Path);
                                                item.Add(parentName + "." + property.Name, property.Value.ToString());
                                            }
                                        }
                                        itemsCollection.Add(item);
                                    }
                                }
                                else
                                {
                                    if (children.Type == JTokenType.Property)
                                    {
                                        var property = children as JProperty;
                                        string parentName = JsonExtensions.GetParentName(t.Path);
                                        loopName = parentName + "." + property.Name;
                                        loopValue = property.Value.ToString();
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    foreach (JToken t in jObject.FindTokens(lastAttribute).Children())
                    {
                        CustomListObjectElement item = new CustomListObjectElement();

                        foreach (var children in t.Children())
                        {
                            if (children.Type == JTokenType.Property)
                            {
                                var property = children as JProperty;
                                string parentName = JsonExtensions.GetParentName(t.Path);
                                item.Add(parentName + "." + property.Name, property.Value.ToString());
                            }
                        }
                        itemsCollection.Add(item);
                    }
                }
            }
            catch(Exception e1)
            {
                throw new InvalidOperationException("An error has occured while trying to retrieve the data. Please try again or change connection properties.");
            }
            

            return itemsCollection;
        }
    }
}
