using System;

namespace PeakboardExtensionGraph.Settings
{
    [Serializable]
    public abstract class SettingsBase
    {
        public string ClientId { get; set; }
        public string TenantId { get; set; }
        public string EndpointUri { get; set; }
        public RequestParameters Parameters { get; set; }
        

    }
}    