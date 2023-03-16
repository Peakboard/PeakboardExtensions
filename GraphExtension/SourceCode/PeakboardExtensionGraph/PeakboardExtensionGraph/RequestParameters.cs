namespace PeakboardExtensionGraph
{
    public class RequestParameters
    {
        public string Select { get; set; }
        public int Top = 0;
        public string OrderBy { get; set; }
        public int Skip = 0;
        public string Filter { get; set; }
    }
}