using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Peakboard.ExtensionKit;
using PeakboardExtensionGraph.UserAuth;

namespace PeakboardExtensionGraph
{
    public class JsonHelper
    {

        public static void FindGraphError(string json)
        {
            JsonTextReader reader = new JsonTextReader(new StringReader(json));
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.PropertyName && reader.Value?.ToString() == "error")
                {
                    GraphHelperBase.DeserializeError(json);
                }
            }
        }
        public static void SkipArray(JsonReader reader)
        {
            // skip nested array
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.StartArray)
                {
                    // nested arrays in nested array get skipped separately
                    SkipArray(reader);
                }
                else if (reader.TokenType == JsonToken.EndArray)
                {
                    // return to upper recursion layer
                    return;
                }
            }
        }

        public static void SkipObject(JsonReader reader)
        {
            // skip nested object
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.StartObject)
                {
                    // nested objects in nested object get skipped separately
                    SkipObject(reader);
                }

                if (reader.TokenType == JsonToken.StartArray)
                {
                    // nested arrays in nested object get skipped separately
                    SkipArray(reader);
                }
                else if (reader.TokenType == JsonToken.EndObject)
                {
                    // return to upper recursion layer
                    return;
                }
            }
        }
        
        public static void OrderByWalkThroughObject(JsonReader reader, string objPrefix, List<string> list)
        {
            // used to get every primitive property of a graph response
            string lastName = "";
            bool value = false;

            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    // store name of property and set value true
                    lastName = (string)reader.Value ?? "";
                    value = true;
                }
                else if (reader.TokenType == JsonToken.StartObject)
                {
                    // if object starts after value is set true
                    // -> property isn't primitive
                    // value is set false and object gets walked recursively
                    // prefix is modified to ensure correct designation for graph call
                    value = false;
                    OrderByWalkThroughObject(reader, $"{objPrefix}/{lastName}", list);
                }
                else if (reader.TokenType == JsonToken.StartArray)
                {
                    // if array starts after value is set true
                    // -> property isn't primitive
                    // value is set false and array gets skipped
                    value = false;
                    JsonHelper.SkipArray(reader);
                }
                else if (reader.TokenType == JsonToken.EndObject)
                {
                    // nested object ends -> return to upper recursion layer
                    return;
                }
                else if (value)
                {
                    // if no array or object starts after value is set
                    // -> property is primitive
                    // property gets designated correctly and added to _orderByAttributes list
                    list.Add($"{objPrefix}/{lastName}");
                    value = false;
                }
            }
        }
        
        public static void ColumnsWalkThroughObject(JsonReader reader, string objPrefix, CustomListColumnCollection cols)
        {
            var lastName = "";
            var value = false;
        
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    // store property name and set value true
                    // next token is either the value or a nested object/array
                    lastName = (string)reader.Value ?? "";
                    value = true;
                }
                else if (reader.TokenType == JsonToken.StartObject)
                {
                    // nested object starts -> walk through recursively
                    // object Prefix is passed on to ensure unique designation
                    // terminology is root-nestedObject-nestedObjectInNestedObject-...
                    value = false;
                    ColumnsWalkThroughObject(reader, $"{objPrefix}-{lastName}", cols);
                }
                else if (reader.TokenType == JsonToken.StartArray)
                {
                    // nested array starts -> skip array
                    cols.Add(new CustomListColumn($"{objPrefix}-{lastName}-Array", CustomListColumnTypes.String));
                    JsonHelper.SkipArray(reader);
                    value = false;
                }
                else if (reader.TokenType == JsonToken.EndObject)
                {
                    // nested object ends -> return to upper recursion layer
                    return;
                }
                else if(value && !lastName.Contains("odata"))
                {
                    // primitive property -> add CustomListColumn with corresponding type
                    CustomListColumn newCol;
                    if (reader.TokenType == JsonToken.Boolean)
                    {
                        newCol = new CustomListColumn($"{objPrefix}-{lastName}", CustomListColumnTypes.Boolean);
                    }
                    else if (reader.TokenType == JsonToken.Integer || reader.TokenType == JsonToken.Float)
                    {
                        newCol = new CustomListColumn($"{objPrefix}-{lastName}", CustomListColumnTypes.Number);
                    }
                    else
                    {
                        newCol = new CustomListColumn($"{objPrefix}-{lastName}", CustomListColumnTypes.String);
                    }
                    cols.Add(newCol);
                    value = false;
                }
            }
        }
        
        public static void ItemsWalkThroughObject(JsonReader reader, string objPrefix, CustomListObjectElement item, JObject obj)
        {
            var lastName = "";
            var value = false;
        
            while (reader.Read())
            {
                if (reader.TokenType == JsonToken.PropertyName)
                {
                    // store property name and set value true
                    // next token is either the value or a nested object/array
                    lastName = (string)reader.Value ?? "";
                    value = true;
                }
                else if (reader.TokenType == JsonToken.StartObject)
                {
                    // nested object starts -> walk through recursively
                    value = false;
                    ItemsWalkThroughObject(reader, $"{objPrefix}-{lastName}", item, obj);
                }
                else if (reader.TokenType == JsonToken.StartArray)
                {
                    // nested array starts -> store entire array json into column and skip the array
                    JsonHelper.SkipArray(reader);
                    item.Add($"{objPrefix}-{lastName}-Array", $"{obj.SelectToken(reader.Path)}");
                }
                else if (reader.TokenType == JsonToken.EndObject)
                {
                    // nested object ends -> return to upper recursion layer
                    return;
                }
                else if(value && !lastName.Contains("odata"))
                {
                    // primitive property -> store the value in corresponding column
                    if (reader.TokenType == JsonToken.Boolean || reader.TokenType == JsonToken.Float ||
                        reader.TokenType == JsonToken.Integer)
                    {
                           item.Add($"{objPrefix}-{lastName}", reader.Value);
                    }
                    else
                    {
                        string objectValue = reader.Value?.ToString();
                        // if value is bigger than 1024 chars -> cut at 1024
                        if (objectValue?.Length - 1024 > 0)
                        { 
                            objectValue = objectValue.Remove(1024);
                        }
                        item.Add($"{objPrefix}-{lastName}", objectValue);
                    }
                }
            }
        }
        
        
    }
}