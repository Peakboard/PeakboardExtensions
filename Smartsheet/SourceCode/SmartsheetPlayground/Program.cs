using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WoutexPlayground
{
    class Program
    {
        static void Main(string[] args)
        {

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + "XXX");

                HttpResponseMessage response = client.GetAsync("https://api.smartsheet.com/2.0/sheets/6952350955923332").Result;
                var responseBody = response.Content.ReadAsStringAsync().Result;

                if (response.IsSuccessStatusCode)
                {
                    var dyn = JsonConvert.DeserializeObject<dynamic>(responseBody);

                    if (dyn.columns == null) { throw new Exception("Columns are empty"); }

                    foreach (var row in dyn.columns)
                    {
                        string id = row["id"];
                        string title = row["title"];
                        string type = row["type"];

                        Console.WriteLine($"{id} - {title}");
                    }

                }
                else
                {
                    Console.WriteLine("Error during call -> " + response.StatusCode + response.ReasonPhrase);
                    Console.WriteLine(responseBody.ToString());
                }

                Console.ReadLine();
            }



        }
    }
}
