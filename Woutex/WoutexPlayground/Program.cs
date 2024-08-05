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
                var authString = Convert.ToBase64String(Encoding.UTF8.GetBytes("wtx:1a7cf68fa7ecc6fef376dfa44999b89f"));
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authString);

                Console.WriteLine(authString);

                ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => { return true; };

                HttpResponseMessage response = client.GetAsync("https://ec2-54-175-232-22.compute-1.amazonaws.com/flex/v2/displays/669938081349562181/status").Result;
                var responseBody = response.Content.ReadAsStringAsync().Result;

                if (response.IsSuccessStatusCode)
                {

                    JArray rawlist = JArray.Parse(responseBody);
                    Console.WriteLine($"Found {rawlist.Count} boxes");
                    foreach (var row in rawlist)
                    {
                        Console.WriteLine($"{row["id"]} - {row["name"]}");
                    }

                }
                else
                {
                    Console.WriteLine("Error during call -> " + response.StatusCode + response.ReasonPhrase);
                    Console.WriteLine(responseBody.ToString());
                }

            }



        }
    }
}
