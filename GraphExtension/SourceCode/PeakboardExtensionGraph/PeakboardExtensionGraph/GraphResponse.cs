namespace PeakboardExtensionGraph
{
    public class GraphResponse
    {
        public GraphContentType Type { get; set; }
        public string Content { get; set; }
    }

    public enum GraphContentType
    {
        Json, OctetStream, Text
    }
}