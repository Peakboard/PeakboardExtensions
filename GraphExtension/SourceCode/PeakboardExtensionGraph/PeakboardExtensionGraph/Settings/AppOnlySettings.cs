using System;
using System.Collections.Generic;
using System.Globalization;

namespace PeakboardExtensionGraph.Settings
{
    public class AppOnlySettings : SettingsBase
    {
        public string Secret { get; set; }
        public string CustomCall { get; set; }
        public Dictionary<string, string> CustomEntities { get; set; }

        public static SettingsBase ConvertOldParameter(string parameter)
        {
            var parameters = parameter.Split(';');
            Int32.TryParse(parameters[8], out var top);
            Int32.TryParse(parameters[9], out var skip);
            
            var customEndpoints = new Dictionary<string, string>();

            if(parameters[11] != ""){
                string[] endpoints = parameters[11].Split(' ');
                foreach (var endpoint in endpoints)
                {
                    customEndpoints.Add(endpoint.Split(',')[0], endpoint.Split(',')[1]);
                }
            }

            return new AppOnlySettings()
            {
                ClientId = parameters[0],
                TenantId = parameters[1],
                Secret = parameters[2],
                EndpointUri = parameters[3],
                Parameters = new RequestParameters()
                {
                    Select = parameters[4],
                    OrderBy = parameters[5],
                    Filter = parameters[6],
                    ConsistencyLevelEventual = parameters[7] == "true",
                    Top = top,
                    Skip = skip
                },
                CustomCall = parameters[10],
                CustomEntities = customEndpoints
            };
        }
    }
}