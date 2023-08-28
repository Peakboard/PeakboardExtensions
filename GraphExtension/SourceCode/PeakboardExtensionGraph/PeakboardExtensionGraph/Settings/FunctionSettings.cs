using System;
using System.Collections.Generic;

namespace PeakboardExtensionGraph.Settings
{
    public class FunctionSettings
    {
        public string Scope { get; set; }
        public string RefreshToken { get; set; }
        
        public string AccessToken { get; set; }
        public string ExpirationTime { get; set; }
        public long Millis { get; set; }
        private Dictionary<string, Tuple<string,string>> Functions { get; set; }
    }
}