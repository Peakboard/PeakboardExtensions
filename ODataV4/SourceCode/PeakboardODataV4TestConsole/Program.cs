using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PeakboardExtensionODataV4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PeakboardODataV4TestConsole
{
	class Program
	{
		static void Main(string[] args)
		{

			//string responseBody = HttpResponse("https://services.odata.org/V4/(S(qzmdxlqbgyyigpp4cphkkjeg))/TripPinServiceRW/People").Result;

			//Console.WriteLine("JSON Body : ---------------------------------------------------");
			//Console.WriteLine(responseBody);

			////var objData = (JObject)JsonConvert.DeserializeObject(responseBody);
			//JObject jObject = JObject.Parse(responseBody);


			//Console.WriteLine("JSON Elements : ---------------------------------------------------");


			//foreach (var values in jObject.SelectToken("value"))
			//{
			//	Console.WriteLine("JSON Element : ---------------------");
			//	foreach (var c in values.Children())
			//	{
			//		if (!c.ToString().StartsWith("\"@odata"))
			//		{
			//			if (c.Type == JTokenType.Property)
			//			{
			//				var property = c as JProperty;
			//				Console.WriteLine(property.Name + " : " +property.Value);
			//			}
			//		}
			//	}

			//}

			//List<Entity> entities = ODataV4Service.GetEntitiesName("https://services.odata.org/V4/(S(qzmdxlqbgyyigpp4cphkkjeg))/TripPinServiceRW");

			//if (entities != null || entities.Count != 0)
			//{
			//	foreach (Entity entity in entities)
			//	{
			//		if (entity.kind == "EntitySet")
			//		{
			//			Console.WriteLine(entity.name + " : " + entity.url);
			//		}
			//	}
			//}

			//List<string> entitiesName = ODataV4Service.GetColumnsFromEntity("https://services.odata.org/V4/(S(qzmdxlqbgyyigpp4cphkkjeg))/TripPinServiceRW","People");

			//List<string> items = ODataV4Service.GetDataFromEntity("https://services.odata.org/V4/(S(qzmdxlqbgyyigpp4cphkkjeg))/TripPinServiceRW", "People");




			Console.ReadLine();
		}

		//private static async Task<string> HttpResponse(string _uri)
		//{
		//	HttpClient client = new HttpClient();
		//	HttpResponseMessage response = await client.GetAsync(_uri);
		//	response.EnsureSuccessStatusCode();
		//	string _responseBody = await response.Content.ReadAsStringAsync();

		//	return _responseBody;
		//}
	}

}
