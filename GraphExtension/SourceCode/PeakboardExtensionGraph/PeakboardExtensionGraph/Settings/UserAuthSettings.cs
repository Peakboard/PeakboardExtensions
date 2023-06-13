using System;
using System.Collections.Generic;

namespace PeakboardExtensionGraph.Settings
{
    [Serializable]
    public class UserAuthSettings : SettingsBase
    {
        public string Scope { get; set; }
        public string RefreshToken { get; set; }
        
        public string AccessToken { get; set; }
        public string ExpirationTime { get; set; }
        public long Millis { get; set; }
        
        public string CustomCall { get; set; }
        public string RequestBody { get; set; } // TODO: Add to parameter string
        public Dictionary<string, string> CustomEntities { get; set; }
        
        public static UserAuthSettings GetSettingsFromParameterString(string parameter)
        {
            var parameters = parameter.Split(';');

            if (parameters.Length != 17) return null;
            
            Int64.TryParse(parameters[5], out var millis);
            Int32.TryParse(parameters[12], out var top);
            Int32.TryParse(parameters[13], out var skip);

            string endpointUrl = parameters[7];

            if (!endpointUrl.StartsWith("https://graph.microsoft.com"))
                endpointUrl = "https://graph.microsoft.com/v1.0/me" + endpointUrl;
            
            var customEndpoints = new Dictionary<string, string>();

            if(parameters[16] != ""){
                string[] endpoints = parameters[16].Split(' ');
                foreach (var endpoint in endpoints)
                {
                    string url = endpoint.Split(',')[1];
                    if (!url.StartsWith("https://graph.microsoft.com")) url = "https://graph.microsoft.com/v1.0" + url;
                    
                    customEndpoints.Add(endpoint.Split(',')[0], url);
                }
            }

            return new UserAuthSettings()
            {
                ClientId = parameters[0],
                TenantId = parameters[1],
                Scope = parameters[2],
                AccessToken = parameters[3],
                ExpirationTime = parameters[4],
                Millis = millis,
                RefreshToken = parameters[6],
                EndpointUri = endpointUrl,
                Parameters = new RequestParameters()
                {
                    Select = parameters[8],
                    OrderBy = parameters[9],
                    Filter = parameters[10],
                    ConsistencyLevelEventual = parameters[11] == "true",
                    Top = top,
                    Skip = skip
                },
                CustomCall = parameters[14],
                RequestBody = parameters[15],
                CustomEntities = customEndpoints
            };
        }

        public string GetParameterStringFromSettings()
        {
            string customEndpoints = "";
            if (CustomEntities.Count != 0)
            {
                foreach (var endpoint in CustomEntities.Keys)
                {
                    customEndpoints += $"{endpoint},{CustomEntities[endpoint]} ";
                }

                customEndpoints = customEndpoints.Remove(customEndpoints.Length - 1);
            }
            
            return
                $"{ClientId};{TenantId};{Scope};{AccessToken};{ExpirationTime};{Millis};{RefreshToken};{EndpointUri};" +
                $"{Parameters.Select};{Parameters.OrderBy};{Parameters.Filter};{Parameters.ConsistencyLevelEventual};" +
                $"{Parameters.Top};{Parameters.Skip};{CustomCall};{RequestBody};{customEndpoints}";
        }

        public void Validate()
        {
            if (ClientId == null) ClientId = "";
            if (TenantId == null) TenantId = "";
            if (Scope == null) Scope = "user.read offline_access";
            if (AccessToken == null) AccessToken = "";
            if (ExpirationTime == null) ExpirationTime = "0";
            if (RefreshToken == null) RefreshToken = "";
            if (EndpointUri == null) EndpointUri = "https://graph.microsoft.com/v1.0/me";
            if (Parameters == null) Parameters = new RequestParameters()
            {
                Select = "",
                OrderBy = "",
                ConsistencyLevelEventual = false,
                Filter = ""
            };
            if (CustomCall == null) CustomCall = "";
            if (CustomEntities == null) CustomEntities = new Dictionary<string, string>();
        }
    }
    
}