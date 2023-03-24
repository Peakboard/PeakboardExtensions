using System.IO;
using Newtonsoft.Json;

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
                    GraphHelper.DeserializeError(json);
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
        
        
    }
}