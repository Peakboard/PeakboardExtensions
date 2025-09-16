using System;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Peakboard.ExtensionKit;
namespace GPT
{
    [Serializable]
    [CustomListIcon("GPT.icon.png")]
    public class GPTCustomList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "ChatGPTCustomList",
                Name = "ChatGPT API",
                Description = "Interface with ChatGPT API",
                PropertyInputPossible = true,
                PropertyInputDefaults = {
                    new CustomListPropertyDefinition() { Name = "Prompt", Value = "Enter your question here" },
                    new CustomListPropertyDefinition() { Name = "Token", Value = "Enter your API token here", Masked = true }
                }
            };
        }
        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            return new CustomListColumnCollection
            {
                new CustomListColumn("Answer", CustomListColumnTypes.String),
            };
        }
        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            string prompt = data.Properties["Prompt"];
            string token = data.Properties["Token"];
            string answer = GetChatGPTResponse(prompt, token).Result;
            var items = new CustomListObjectElementCollection();
            items.Add(new CustomListObjectElement { { "Answer", answer } });
            return items;
        }
        public static async Task<string> GetChatGPTResponse(string prompt, string token)
        {
            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("Authorization", "Bearer " + token);
                var requestData = new
                {
                    model = "gpt-3.5-turbo",
                    messages = new object[]
                    {
                        new { role = "system", content = "Your name is PeakBot. You are an assistant integrated into the runtime of a visualization software. Users can access you by interacting with the runtime." }, // Fit this to your needs
                        new { role = "user", content = prompt }
                    }
                };
                var response = await client.PostAsync("https://api.openai.com/v1/chat/completions",
                                                      new StringContent(JsonConvert.SerializeObject(requestData), Encoding.UTF8, "application/json"));
                var responseBody = await response.Content.ReadAsStringAsync();
                var jsonResponse = JObject.Parse(responseBody);
                return jsonResponse["choices"]?.First?["message"]?["content"]?.ToString().Trim() ?? "Error retrieving response.";
            }
        }
    }
}