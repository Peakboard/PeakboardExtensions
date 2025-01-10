using Peakboard.ExtensionKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProGloveExtension.CustomLists
{
    [Serializable]
    public class ProGloveExtensionReportsList : CustomListBase
    {

        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "CustomListReports",
                Name = "RepotsList",
                Description = "Add Reports of Scanner",
                PropertyInputPossible = true,
                PropertyInputDefaults =
                {
                    new CustomListPropertyDefinition(){Name = "ClientId"},
                    new CustomListPropertyDefinition(){Name = "BasedUrl"},
                    new CustomListPropertyDefinition(){Name = "Username"},
                    new CustomListPropertyDefinition(){Name = "Password"}
                }
            };
        }
        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            var column = new CustomListColumnCollection();
            return column;
        }
        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            return new CustomListObjectElementCollection();
        }
    }
}
