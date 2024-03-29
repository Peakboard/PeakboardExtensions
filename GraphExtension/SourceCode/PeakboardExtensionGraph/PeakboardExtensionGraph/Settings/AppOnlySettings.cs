﻿using System;
using System.Collections.Generic;
using System.Globalization;

namespace PeakboardExtensionGraph.Settings
{
    public class AppOnlySettings : SettingsBase
    {
        public string Secret { get; set; }
        public string CustomCall { get; set; }
        public string RequestBody { get; set; }
        public Dictionary<string, string> CustomEntities { get; set; }

        public static AppOnlySettings GetSettingsFromParameterString(string parameter)
        {
            var parameters = parameter.Split(';');
            
            if (parameters.Length != 13) return null;
            
            Int32.TryParse(parameters[8], out var top);
            Int32.TryParse(parameters[9], out var skip);
            
            string endpointUrl = parameters[3];

            if (!endpointUrl.StartsWith("https://graph.microsoft.com"))
                endpointUrl = "https://graph.microsoft.com/v1.0" + endpointUrl;
            
            var customEndpoints = new Dictionary<string, string>();

            if(parameters[12] != ""){
                string[] endpoints = parameters[12].Split(' ');
                foreach (var endpoint in endpoints)
                {
                    string url = endpoint.Split(',')[1];
                    if (!url.StartsWith("https://graph.microsoft.com")) url = "https://graph.microsoft.com/v1.0" + url;
                    
                    customEndpoints.Add(endpoint.Split(',')[0], url);
                }
            }

            return new AppOnlySettings()
            {
                ClientId = parameters[0],
                TenantId = parameters[1],
                Secret = parameters[2],
                EndpointUri = endpointUrl,
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
                RequestBody = parameters[11],
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
            
            return $"{ClientId};{TenantId};{Secret};{EndpointUri};{Parameters.Select};{Parameters.OrderBy};{Parameters.Filter};" + 
                   $"{Parameters.ConsistencyLevelEventual};{Parameters.Top};{Parameters.Skip};{CustomCall};{RequestBody};{customEndpoints}";
        }

        public void Validate()
        {
            if (ClientId == null) ClientId = "";
            if (TenantId == null) TenantId = "";
            if (Secret == null) Secret = "";
            if (EndpointUri == null) EndpointUri = "https://graph.microsoft.com/v1.0/users";
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