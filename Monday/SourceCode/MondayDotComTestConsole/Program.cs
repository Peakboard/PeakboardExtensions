using PeakboardExtensionMonday;
using PeakboardExtensionMonday.MondayEntities;
using System;
using System.Collections.Generic;

namespace MondayDotComTestConsole
{
    class Program
	{

        static void Main(string[] args)
        {
            string query = "";
            
            string token = "";
            string url = "https://api.monday.com/v2/";

            string boardId = "629503595";
            using (var client = new MondayClient(token, url))
            {
                var service = new MondayService(client);


                // get all boards
                List<Group> groups = service.GetGroups(Int32.Parse(boardId));

                if (groups == null || groups.Count == 0)
                {
                    return;
                }
                else
                {
                    foreach (var group in groups)
                    {
                        Console.WriteLine(group.Id + " " + group.Title);
                    }
                }


            }

            Console.WriteLine("--------------------------------");
            string groupId = "allGroups";
            using (var client = new MondayClient(token, url))
            {
                var service = new MondayService(client);

                if (groupId == "allGroups")
                {
                    // get items for the first board in the list
                    Board board = service.GetBoardWithItems(Int32.Parse(boardId));
                    foreach (var columnName in board.Items[0].ItemColumnValues)
                    {
                        Console.WriteLine(columnName.Title + " " + columnName.Text);
                    }
                }
                else
                {
                    Group group = service.GetGroupWithItems(Int32.Parse(boardId), groupId);
                    foreach (var groupItem in group.Items)
                    {
                        Console.WriteLine(groupItem.Id);
                        Console.WriteLine(groupItem.Name);
                        foreach (var itemColumn in groupItem.ItemColumnValues)
                        {
                            Console.WriteLine(itemColumn.Title + " " + itemColumn.Text);
                        }
                    }
                }

            }

            //var helper = new MondayService();
            //string jsonString = helper.GetDataFromQuery(url, token, query).Result;

            //JObject jObject = JObject.Parse(jsonString);

            //string lastAttribute = JsonExtensions.GetLastAttribute(jObject);
            ////Console.WriteLine(lastAttribute);

            //bool isLooping = false;
            //string loopName = "";
            //string loopValue = "";

            //foreach (JToken t in jObject.FindTokens(lastAttribute).Children().Children())
            //{
            //    if ((t is IEnumerable<JToken> resultList) && resultList.Children().Children().Count() > 0)
            //    {
            //        isLooping = true;
            //        break;
            //    }
            //}

            //foreach (JToken t in jObject.FindTokens(lastAttribute).Children())
            //{

            //    foreach (var children in t.Children())
            //    {

            //        if ((children is IEnumerable<JToken> resultList))
            //        {
            //            if (resultList.Children().Children().Count() > 0)
            //            {


            //                foreach (JToken items in resultList.Children())
            //                {
            //                    Console.WriteLine();
            //                    Console.Write("NEW ITEM ");
            //                    if (isLooping)
            //                        Console.Write(loopValue + " ");
            //                    foreach (JToken item in items.Children())
            //                    {
            //                        if (item.Type == JTokenType.Property)
            //                        {
            //                            var property = item as JProperty;
            //                            Console.Write(property.Value + " " + "ITEM");
            //                        }
            //                    }
            //                    Console.WriteLine(" FIN NEW ITEM");

            //                }

            //            }
            //            else
            //            {
            //                if (children.Type == JTokenType.Property)
            //                {
            //                    var property = children as JProperty;
            //                    if (property.Name != "items")
            //                    {
            //                        if (isLooping)
            //                        {
            //                            loopName = property.Name.ToString();
            //                            loopValue = property.Value.ToString();
            //                        }
            //                        else
            //                        {
            //                            Console.Write(property.Value+" ");
            //                        }
            //                    }
            //                }
            //            }
            //        }
            //    }
            //}


            //List<string> columnName = new List<string>();

            //foreach (JToken t in jObject.FindTokens(lastAttribute).Children())
            //{
            //    foreach (var children in t.Children())
            //    {

            //        if ((children is IEnumerable<JToken> resultList))
            //        {
            //            if (resultList.Children().Children().Count() > 0)
            //            {
            //                foreach (JToken mondayItem in resultList.Children())
            //                {
            //                    foreach (JToken items in mondayItem.Children())
            //                    {
            //                        if (items.Type == JTokenType.Property)
            //                        {
            //                            var property = items as JProperty;
            //                            string parentName = JsonExtensions.GetParentName(mondayItem.Path);
            //                            if (!columnName.Contains(parentName + "." + property.Name))
            //                                columnName.Add(parentName+"."+property.Name);

            //                        }
            //                    }
            //                }
            //            }
            //            else
            //            {
            //                if (children.Type == JTokenType.Property)
            //                {
            //                    var property = children as JProperty;
            //                    string parentName = JsonExtensions.GetParentName(t.Path);
            //                    if (!columnName.Contains(parentName + "." + property.Name))
            //                        columnName.Add(parentName + "." + property.Name);


            //                }
            //            }
            //        }
            //    }


            //}

            //foreach(string s in columnName)
            //{
            //    Console.WriteLine(s);
            //}











            //foreach (JToken t in jObject.FindTokens(lastAttribute).Children().Children())
            //{
            //    Console.WriteLine("Nouvel Item crée");
            //    if ((t is IEnumerable<JToken> resultList))
            //    {
            //        if(resultList.Children().Children().Count()>0)
            //        {
            //            foreach (JToken item in resultList.Children().Children())
            //            {
            //                if (item.Type == JTokenType.Property)
            //                {
            //                    var property = item as JProperty;
            //                    Console.WriteLine(property.Name + " " + property.Value);
            //                }

            //            }
            //        }
            //        else
            //        {
            //            if (t.Type == JTokenType.Property)
            //            {
            //                var property = t as JProperty;
            //                if (property.Name != "items")
            //                {
            //                    Console.WriteLine(property.Name + " " + property.Value);
            //                }

            //            }
            //        }
            //    }
            //}
            //if(child.Count()<=1)
            //{
            //    if (children.Type == JTokenType.Property)
            //    {
            //        property = children as JProperty;
            //        Console.WriteLine(" " + property.Name + " " + property.Value);
            //        Console.WriteLine("test");
            //    }
            //}
            //else
            //{
            //    foreach (var item in children.Children())
            //    {
            //        if (item.Type == JTokenType.Property)
            //        {
            //            var itemProperty = item as JProperty;
            //            Console.WriteLine(itemProperty.Name + " " + itemProperty.Value.ToString());


            //        }
            //    }
            //}



            //if (children.Type == JTokenType.Array)
            //{
            //    foreach (var item in children.Children())
            //    {
            //        if (item.Type == JTokenType.Property)
            //        {
            //            var itemProperty = item as JProperty;
            //            Console.WriteLine(property.Name ?? " " + itemProperty.Name + " " + itemProperty.Value.ToString());


            //        }
            //    }


            //}

            //if (children.Type == JTokenType.Property)
            //{
            //    property = children as JProperty;
            //    Console.WriteLine( " " + property.Name + " " + property.Value);
            //}





            //foreach (var i in jObject.SelectToken("data"))
            //{
            //    while(i)

            //    //if (i.Type == JTokenType.Property)
            //    //{
            //    //    var property = i as JProperty;
            //    //    Console.WriteLine(property.Name);
            //    //}
            //    //foreach (var c in i.Children())
            //    //{
            //    //    //while(c.Children().Count()>0)
            //    //    //{

            //    //    //}
            //    //    if (c.Type == JTokenType.Property)
            //    //    {
            //    //        var property = c as JProperty;
            //    //        Console.WriteLine(property.Name);
            //    //    }
            //    //}

            //    //foreach (var c in i.Children())
            //    //{
            //    //    if (c.Type == JTokenType.Property)
            //    //    {
            //    //        var property = c as JProperty;
            //    //        Console.WriteLine(property.Name);
            //    //    }
            //    //}

            //}

            //Recursive(jObject)

            //Console.ReadLine();

            //Console.WriteLine(query);
            //var helper = new MondayHelper();
            //string json = helper.QueryMondayApiV2(query, MondayApiKey, MondayApiUrl).Result;
            //var obj = JObject.Parse(json);

            //using (var client = new MondayClient(token, url))
            //{
            //    var service = new MondayService(client);

            //    // get items for the first board in the list
            //    Board board = service.GetBoardWithItems(Int32.Parse("859044450"));
            //    foreach (var boardItem in board.Items)
            //    {
            //        Console.WriteLine(boardItem.Id + " " + boardItem.Name);
            //        foreach (var subItem in boardItem.ItemColumnValues)
            //        {
            //            Console.WriteLine(subItem.Title + ": " + subItem.Text);
            //        }

            //    }
            //}

            //using (var client = new MondayClient(token, url))
            //{
            //    var service = new MondayService(client);

            //    // get items for the first board in the list
            //    Board board = service.GetBoardWithItems(Int32.Parse("859044450"));
            //    foreach(var columnName in board.Items[0].ItemColumnValues)
            //    {
            //        Console.WriteLine(columnName.Title);
            //    }
            //}


            //Console.WriteLine(query3);

            //foreach (var c in obj["data"]["boards"])
            //{
            //    string id = c["id"].ToString();
            //    string name = c["name"].ToString();
            //    Console.WriteLine(id + "    " + name);
            //}

            //var helper = new MondayService();
            //string jsonString = helper.GetDataFromQuery(url, token, query).Result;



            //JObject jo = JObject.Parse(jsonString);

            //foreach(var i in jo.SelectToken("data.boards[0].groups[0].items"))
            //{
            //    foreach(var c in i.Children())
            //    {
            //        if (c.Type == JTokenType.Property)
            //        {
            //            var property = c as JProperty;
            //            Console.WriteLine(property.Name);
            //        }



            //    }
            //    break;
            //}

            //foreach (var i in jo.SelectToken("data.boards[0].groups[0].items"))
            //{
            //    foreach (var c in i.Children())
            //    {
            //        if (c.Type == JTokenType.Property)
            //        {
            //            var property = c as JProperty;
            //            Console.WriteLine(property.Value.ToString());
            //        }


            //    }
            //}

            //string test = "data.boards[0].groups[0].items";
            //foreach(var c in jObj)
            //{
            //    Console.WriteLine(c);
            //}
            //foreach (string typeStr in obj.ChildrenTokens)
            //{
            //var values = JsonConvert.DeserializeObject<Dictionary<string, object>>(obj);

            //IDictionary<string, object> dict = Program.ToDictionary(responseObj);


            //}
            //dynamic jsonDe = JsonConvert.DeserializeObject(toString);

            //Board myDeserializedClass = JsonConvert.DeserializeObject<Board>(obj);


            //int i = 0;
            //foreach (var c in jObj.Children())
            //{
            //    if (c.Type == JTokenType.Property)
            //    {
            //        var property = c as JProperty;
            //        Console.WriteLine(property.Name);
            //    }
            //}

            // PrintObject(responseObj, 0);

            //while(jObj.Children().Any())
            //{
            //    if(jObj)
            //}
            //bool hasChildren = true;
            //var j = jObj.Children();
            //while(hasChildren)
            //{
            //    if(j.Children().Any())
            //    {
            //        hasChildren = true;
            //        //foreach (Jtoken c in j.Children())
            //        //{
            //        //    if (c.Type == JTokenType.Property)
            //        //    {
            //        //        var property = c as JProperty;
            //        //        Console.WriteLine(property.Name);

            //        //    }
            //        //}

            //    }
            //    else
            //    {
            //        hasChildren = false;
            //    }
            //}

        }



        //private static void PrintObject(JToken token, int depth)
        //{
        //    if (token is JProperty)
        //    {
        //        var jProp = (JProperty)token;
        //        var spacer = string.Join("", Enumerable.Range(0, depth).Select(_ => "\t"));
        //        var val = jProp.Value is JValue ? ((JValue)jProp.Value).Value : "-";

        //        Console.WriteLine($"{spacer}{jProp.Name}  -> {val}");

        //        foreach (var child in jProp.Children())
        //        {
        //            PrintObject(child, depth + 1);
        //        }
        //    }
        //    else if (token is JObject)
        //    {
        //        foreach (var child in ((JObject)token).Children())
        //        {
        //            PrintObject(child, depth + 1);
        //        }
        //    }
        //}



        //public static IDictionary<string, object> ToDictionary(JObject @object)
        //{
        //    var result = @object.ToObject<Dictionary<string, object>>();

        //    var JObjectKeys = (from r in result
        //                       let key = r.Key
        //                       let value = r.Value
        //                       where value.GetType() == typeof(JObject)
        //                       select key).ToList();

        //    var JArrayKeys = (from r in result
        //                      let key = r.Key
        //                      let value = r.Value
        //                      where value.GetType() == typeof(JArray)
        //                      select key).ToList();

        //    JArrayKeys.ForEach(key => result[key] = ((JArray)result[key]).Values().Select(x => ((JValue)x).Value).ToArray());
        //    JObjectKeys.ForEach(key => result[key] = ToDictionary(result[key] as JObject));

        //    return result;
        //}










    }	
}
