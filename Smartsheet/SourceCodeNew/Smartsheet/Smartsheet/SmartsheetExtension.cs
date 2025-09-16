using System;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using Peakboard.ExtensionKit;

namespace Smartsheet
{
    [ExtensionIcon("Smartsheet.Smartsheet.png")]
    public class SmartsheetExtension : ExtensionBase
    {
        public SmartsheetExtension(IExtensionHost host) : base(host) 
        { 
        
        }
        
        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition
            {
                ID = "Smartsheet",
                Name = "Smartsheet",
                Description = "Get your data from Smartsheet",
                Version = "1.0",
                Author = "Michelle Wu",
                Company = "Peakboard GmbH.",
                Copyright = "Copyright © Peakboard GmbH",
            };
        }

        protected override CustomListCollection GetCustomListsOverride()
        {
            return new CustomListCollection
            {
                new SmartsheetTableListCustomList(),
                new SmartsheetTableDataCustomList(),
            };
        }

        private static string MyLocalToken = string.Empty;


    }
}
