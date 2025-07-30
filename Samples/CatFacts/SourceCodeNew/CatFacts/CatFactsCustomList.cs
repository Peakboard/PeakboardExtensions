using Peakboard.ExtensionKit;

namespace CatFacts;

[CustomListIcon("CatFacts.CatIcon.png")]
public class CatFactsCustomList : CustomListBase
{
    protected override CustomListDefinition GetDefinitionOverride()
    {
        return new CustomListDefinition
        {
            ID = "CatFactsCustomList",
            Name = "Cat Facts",
            Description = "Random cute stuff about cats",
            PropertyInputPossible = true,
            PropertyInputDefaults = {
                new CustomListPropertyDefinition(){ Name = "MaxLength", Value = "140"}
            }
        };
    }

    protected override void CheckDataOverride(CustomListData data)
    {
        CheckAndGetMaxLengthProperty(data);
    }

    protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
    {
        return
        [
            new CustomListColumn("Fact", CustomListColumnTypes.String),
            new CustomListColumn("Length", CustomListColumnTypes.Number),
        ];
    }


    protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
    {
        var maxLength = CheckAndGetMaxLengthProperty(data);

        var myfact = GetRandomCatfact(maxLength).Result;

        var items = new CustomListObjectElementCollection
        {
            new CustomListObjectElement { { "Fact", myfact.fact }, { "Length", myfact.length }, }
        };

        return items;
    }

    private int CheckAndGetMaxLengthProperty(CustomListData data)
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

    public static async Task<CatFactData> GetRandomCatfact(int MaxLength)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, string.Format("https://catfact.ninja/fact?max_length={0}", MaxLength));
        var httpHandler = new HttpClientHandler();
        var httpClient = new HttpClient(httpHandler);
        var responseUserTimeLine = await httpClient.SendAsync(request);

        //JObject dynobj = JObject.Parse(await responseUserTimeLine.Content.ReadAsStringAsync());
        //string myfact = dynobj["fact"].ToString();

        var json = Newtonsoft.Json.JsonConvert.DeserializeObject(await responseUserTimeLine.Content.ReadAsStringAsync());
        var myfact = Newtonsoft.Json.JsonConvert.DeserializeObject<CatFactData>(await responseUserTimeLine.Content.ReadAsStringAsync());

        return myfact;
    }

    public class CatFactData
    {
        public string fact;
        public int length;
    }
}
