using System;
using System.Collections.Generic;

using System.Net.Http;
using System.Net.Http.Headers;

namespace PeakboardExtensionGraph
{
    public class RequestBuilder
    {
        private string _accessToken;
        private const string BaseUrl = "https://graph.microsoft.com/v1.0/me";
        private Dictionary<string, string> _queries;

        public RequestBuilder(string accessToken)
        {
            _accessToken = accessToken;
            _queries = new Dictionary<string, string>()
            {
                { "mail", "/messages" },
                { "calendar", "/calendarview" },
                { "people", "/people" },
                { "contacts", "/contacts" },
                { "todos", "/todo/lists/{0}/tasks" },
                { "todolists", "/todo/lists" }
            };
        }

        public HttpRequestMessage GetRequest(string suffix = null, RequestParameters parameters = null)
        {

            string url = BaseUrl + suffix;

            string queryParams = "";

            if(parameters != null)
            {
                queryParams = "?";
                
                if (suffix == "/calendarview")
                {
                    // add required parameter for calendar view request
                    var start = DateTime.Now.ToString("yyyy-MM-ddThh:mm:ssZ");
                    var end = DateTime.Now.AddDays(7).ToString("yyyy-MM-ddThh:mm:ssZ");
                    queryParams += $"startdatetime={start}&enddatetime={end}";
                }
                
                if (!string.IsNullOrEmpty(parameters.Filter))
                {
                    queryParams += $"$filter={parameters.Filter}";
                }

                if (!string.IsNullOrEmpty(parameters.OrderBy))
                {
                    if (queryParams != "?")
                    {
                        queryParams += "&";
                    }

                    queryParams += $"$orderby={parameters.OrderBy}";
                }

                if (parameters.Skip != 0)
                {
                    if (queryParams != "?")
                    {
                        queryParams += "&";
                    }

                    queryParams += $"$skip={parameters.Skip}";
                }

                if (parameters.Top > 0)
                {
                    if (queryParams != "?")
                    {
                        queryParams += "&";
                    }

                    queryParams += $"$top={parameters.Top}";
                }

                if (!string.IsNullOrEmpty(parameters.Select))
                {
                    if (queryParams != "?")
                    {
                        queryParams += "&";
                    }

                    queryParams += $"$select={parameters.Select}";
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