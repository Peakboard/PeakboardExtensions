using System;

namespace PeakboardExtensionGraph
{
    public class RootMsGraphError
    {
        public MsGraphError Error { get; set; }
    }
    public class MsGraphError
    {
        public string Code { get; set; }
        public string Message { get; set; }
        
    }

    public class MsGraphInnerError
    {
        public DateTime Date { get; set; }
        public string RequestId { get; set; }
        public string RequestClientId { get; set; }
    }

    public class MsGraphException : Exception
    {
        public MsGraphException(string message) : base(message)
        {
            
        }
    }
}