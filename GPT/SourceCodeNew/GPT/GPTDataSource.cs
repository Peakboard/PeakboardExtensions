using Peakboard.ExtensionKit;

namespace GPT
{
    [ExtensionIcon("GPT.icon.png")]
    public class GPTExtension : ExtensionBase
    {
        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "ChatGPT",
                Name = "ChatGPT API Extension",
                Description = "Interface with the ChatGPT API to retrieve chatbot answers",
                Version = "1.0",
                Author = "Your Name",
                Company = "Your Company",
                Copyright = "Your Copyright"
            };
        }

        protected override CustomListCollection GetCustomListsOverride()
        {
            return new CustomListCollection
            {
                new GPTCustomList(),
            };
        }
    }
}
