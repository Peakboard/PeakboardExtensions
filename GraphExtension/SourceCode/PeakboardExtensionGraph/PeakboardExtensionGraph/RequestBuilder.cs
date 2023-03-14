using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;

namespace PeakboardExtensionGraph
{
    public class RequestBuilder
    {
        private string _accessToken;
        private string path = @"C:\Users\Yannis\Documents\Peakboard\queries.json";
    
        private const string BaseUrl = "https://graph.microsoft.com/v1.0/me";
        private Dictionary<string, string> _queries;

        public RequestBuilder(string accessToken)
        {
            _accessToken = accessToken;
            _queries = JsonConvert.DeserializeObject<Dictionary<string, string>>(readJson(path));
        }

        private string readJson(string p)
        {
            var streamReader = new StreamReader(p);
            string value = streamReader.ReadToEnd();
            streamReader.Close();
            return value;
        }

        public HttpRequestMessage GetRequest(string key = null, RequestParameters parameters = null)
        {
            var suffix = "";
            if(key != null)
            {
                _queries.TryGetValue(key, out suffix);
            }

            string url = BaseUrl + suffix;

            string queryParams = "";

            if(parameters != null)
            {
                queryParams = "?";
                if (parameters.Filter != null)
                {
                    queryParams += $"$filter={parameters.Filter}";
                }

                if (parameters.OrderBy != null)
                {
                    if (queryParams != "?")
                    {
                        queryParams += "&";
                    }

                    queryParams += $"$orderBy={parameters.OrderBy}";
                }

                if (parameters.Skip != 0)
                {
                    if (queryParams != "?")
                    {
                        queryParams += "&";
                    }

                    queryParams += $"$skip={parameters.Skip}";
                }

                if (parameters.Top != 0)
                {
                    if (queryParams != "?")
                    {
                        queryParams += "&";
                    }

                    queryParams += $"top={parameters.Top}";
                }

                if (parameters.Select != null)
                {
                    if (queryParams != "?")
                    {
                        queryParams += "&";
                    }

                    queryParams += "$select=";
                    foreach (var field in parameters.Select)
                    {
                        queryParams += $"{field},";
                    }

                    queryParams.Remove(queryParams.Length-1);
                }
            }
            
            
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(url+queryParams),
                Method = HttpMethod.Get
            };
        
            request.Headers.Authorization = new AuthenticationHeaderValue("bearer", _accessToken);

            return request;
        }

        public void RefreshToken(string token)
        {
            _accessToken = token;
        }

    }
}