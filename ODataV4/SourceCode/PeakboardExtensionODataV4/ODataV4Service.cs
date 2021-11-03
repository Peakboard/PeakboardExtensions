using Newtonsoft.Json.Linq;
using Peakboard.ExtensionKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PeakboardExtensionODataV4
{
	public static class ODataV4Service
	{
		public static List<Entity> GetEntitiesName(string _url, string _authentication)
		{
			string responseBody = HttpService.StartHttpRequest(_url, _authentication);

			if (responseBody == null)
			{
				return null;
			}

			List<Entity> entities = new List<Entity>();
			JObject jObject = JObject.Parse(responseBody);

			foreach (var values in jObject.SelectToken("value"))
			{
				Entity entity = new Entity();
				foreach (var value in values.Children())
				{
					if (!value.ToString().StartsWith("\"@odata") && value.Type == JTokenType.Property)
					{
						var property = value as JProperty;
						switch (property.Name)
						{
							case "name":
								entity.name = property.Value.ToString();
								break;
							case "kind":
								entity.kind = property.Value.ToString();
								break;
							case "url":
								entity.url = property.Value.ToString();
								break;
						}
					}
				}
				entities.Add(entity);
			}

			return entities;
		}
		public static List<string> GetColumnsFromEntity(string _url, string _entityUrl, string _authentication)
		{
			string finalUrl = RouteService.GetEntityRoute(_url, _entityUrl);
			string responseBody = HttpService.StartHttpRequest(finalUrl, _authentication);

			if (responseBody == null)
			{
				return null;
			}

			List<string> column = new List<string>();
			JObject jObject = JObject.Parse(responseBody);

			foreach (var values in jObject.SelectToken("value"))
			{
				foreach (var value in values.Children())
				{
					if (!value.ToString().Contains("@odata") && value.Type == JTokenType.Property)
					{
						var property = value as JProperty;
						column.Add(property.Name);
					}
				}
				break;

			}

			return column;

		}
		public static CustomListObjectElementCollection GetItemsFromEntity(string _url, string _entityUrl, string _entityPropertyList, int _maxRows, string _authentication, string _queryOption)
		{
			string finalUrl = "";

			if(_maxRows==0)
			{
				if(String.IsNullOrEmpty(_queryOption))
				{
					finalUrl = RouteService.GetEntityRouteWithSelect(_url, _entityUrl, _entityPropertyList);
				}
				else
				{
					finalUrl = RouteService.GetEntityRouteWithSelectAndQueryOption(_url, _entityUrl, _entityPropertyList, _queryOption);
				}

			}
			else if(_maxRows>0)
			{
				if (String.IsNullOrEmpty(_queryOption))
				{
					finalUrl = RouteService.GetEntityRouteWithSelectAndTop(_url, _entityUrl, _entityPropertyList, _maxRows);
				}
				else
				{
					finalUrl = RouteService.GetEntityRouteWithSelectAndTopAndQueryOption(_url, _entityUrl, _entityPropertyList, _maxRows, _queryOption);
				}

			}
			else
			{
				return null;
			}

			string responseBody = HttpService.StartHttpRequest(finalUrl, _authentication);

			if (responseBody == null)
			{
				return null;
			}

			CustomListObjectElementCollection itemsCollection = new CustomListObjectElementCollection();
			JObject jObject = JObject.Parse(responseBody);

			foreach (var values in jObject.SelectToken("value"))
			{
				CustomListObjectElement item = new CustomListObjectElement();
				foreach (var value in values.Children())
				{
					if (!value.ToString().Contains("@odata") && value.Type == JTokenType.Property)
					{
						var property = value as JProperty;
						item.Add(property.Name,property.Value.ToString());
					}
				}
				itemsCollection.Add(item);
			}
			return itemsCollection;
		}

		
	}
}
