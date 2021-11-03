using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace PeakboardExtensionODataV4
{
	public static class HttpService
	{
		public static string StartHttpRequest(string _uri, string _authentication)
		{
			string responseBody = null;

			if(String.IsNullOrEmpty(_authentication))
			{
				responseBody = null;
			}
			else if(_authentication=="none")
			{
				responseBody = DoHttpRequest(_uri);
			}
			else if(_authentication.StartsWith("basic/"))
			{
				_authentication = _authentication.Remove(0, 6);
				responseBody = DoHttpRequestBasicAuth(_uri, _authentication);
			}
			else if (_authentication.StartsWith("bearer/"))
			{
				_authentication = _authentication.Remove(0, 7);
				responseBody = DoHttpRequestBearerAuth(_uri, _authentication);
			}
			else
			{
				responseBody = null;
			}

			return responseBody;
		}

		private static string DoHttpRequest(string _uri)
		{
			HttpClient client = new HttpClient();

			HttpResponseMessage response = client.GetAsync(_uri).Result;
			if (response.IsSuccessStatusCode)
			{
				var responseBody = response.Content;

				return responseBody.ReadAsStringAsync().Result;
			}
			else
			{
				return null;
			}
		}

		private static string DoHttpRequestBasicAuth(string _uri, string _creditials)
		{
			HttpClient client = new HttpClient();

			var byteArray = Encoding.ASCII.GetBytes(_creditials);
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

			HttpResponseMessage response = client.GetAsync(_uri).Result;
			if (response.IsSuccessStatusCode)
			{
				var responseBody = response.Content;

				return responseBody.ReadAsStringAsync().Result;
			}
			else
			{
				return null;
			}
		}

		private static string DoHttpRequestBearerAuth(string _uri, string _token)
		{
			HttpClient client = new HttpClient();

			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);

			HttpResponseMessage response = client.GetAsync(_uri).Result;
			if (response.IsSuccessStatusCode)
			{
				var responseBody = response.Content;

				return responseBody.ReadAsStringAsync().Result;
			}
			else
			{
				return null;
			}
		}
	}
}
