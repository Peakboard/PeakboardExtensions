using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Peakboard.ExtensionKit;

namespace PeakboardExtensionCatFacts
{
    [Serializable]
    class CatFactsCustomList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = $"CatFactsCustomList",
                Name = "Cat Facts List",
                Description = "Random cute stuff about cats",
                DataInputPossible = true,
                DataInputRequired = false,
                PropertyDefaultValues = { { "MaxLength", "140" },  }
            };
        }

        protected override void CheckDataOverride(CustomListData data)
        {
            CheckAndGetMaxLengthProperty(data);
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            return new CustomListColumnCollection
            {
                new CustomListColumn("Fact", CustomListColumnTypes.String),
                new CustomListColumn("Length", CustomListColumnTypes.Number),
            };
        }


        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            Int32 maxLength = CheckAndGetMaxLengthProperty(data);

            CatFact myfact = GetRandomCatfact(maxLength).Result;

            var items = new CustomListObjectElementCollection();
            items.Add(new CustomListObjectElement { { "Fact", myfact.fact }, { "Length", myfact.length }, });
            
            return items;
        }

        private Int32 CheckAndGetMaxLengthProperty(CustomListData data)
        {
            if (!data.Properties.TryGetValue("MaxLength", StringComparison.OrdinalIgnoreCase, out var MaxLength))
            {
                throw new InvalidOperationException("The property MaxLength is not defined");
            }

            int length;

            try
            {
                length = Convert.ToInt32(MaxLength);
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("Max Length data type mismatch", e);
            }

            if (length < 20)
            {
                throw new InvalidOperationException("Max Length property is not long enough");
            }

            return length;
        }

        public static async Task<CatFact> GetRandomCatfact(int MaxLength)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, string.Format("https://catfact.ninja/fact?max_length={0}", MaxLength));
            var httpHandler = new HttpClientHandler();
            var httpClient = new HttpClient(httpHandler);
            var responseUserTimeLine = await httpClient.SendAsync(request);

            //JObject dynobj = JObject.Parse(await responseUserTimeLine.Content.ReadAsStringAsync());
            //string myfact = dynobj["fact"].ToString();

            var json = Newtonsoft.Json.JsonConvert.DeserializeObject(await responseUserTimeLine.Content.ReadAsStringAsync());
            CatFact myfact = Newtonsoft.Json.JsonConvert.DeserializeObject<CatFact>(await responseUserTimeLine.Content.ReadAsStringAsync());

            return myfact;
        }

        public class CatFact
        {
            public string fact;
            public int length;
        }

    }
}
