namespace InfluxDbExtension
{
    public class Content
    {
        public Dialect Dialect { get; set; }

        public string Query { get; set; }

        public string Type { get; set; }
    }

    public class Dialect
    {
        public string[] Annotations { get; set; }
        public string CommentPrefix { get; set; }
        public string Delimiter { get; set; }
        public bool Header { get; set; }
    }
}