using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PeakboardExtensionODataV4
{
	public static class RouteService
	{
		public static string GetEntityRoute(string _url, string _entityUrl)
		{
			return _url + "/" + _entityUrl;
		}

		public static string GetEntityRouteWithSelect(string _url, string _entityUrl, string _entityPropertyList)
		{
			string selectOption = "$select=" + _entityPropertyList;

			return _url + "/" + _entityUrl + "/?" +selectOption;
		}

		public static string GetEntityRouteWithSelectAndTop(string _url, string _entityUrl, string _entityPropertyList, int _maxRows)
		{
			string selectOption = "$select=" + _entityPropertyList;

			string topOption = "$top=" + _maxRows.ToString();

			return _url + "/" + _entityUrl + "/?" + selectOption + "&" + topOption;
		}

		public static string GetEntityRouteWithSelectAndQueryOption(string _url, string _entityUrl, string _entityPropertyList, string _queryOption)
		{
			string selectOption = "$select=" + _entityPropertyList;


			return _url + "/" + _entityUrl + "/?" + selectOption + "&" + _queryOption;
		}

		public static string GetEntityRouteWithSelectAndTopAndQueryOption(string _url, string _entityUrl, string _entityPropertyList, int _maxRows, string _queryOption)
		{
			string selectOption = "$select=" + _entityPropertyList;

			string topOption = "$top=" + _maxRows.ToString();

			return _url + "/" + _entityUrl + "/?" + selectOption + "&" + topOption + "&" + _queryOption;
		}
	}
}
