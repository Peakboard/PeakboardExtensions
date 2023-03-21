using Peakboard.ExtensionKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PeakboardExtensionODataV4
{
	class ODataV4EntityList : CustomListBase
	{
		protected override CustomListDefinition GetDefinitionOverride()
		{
			return new CustomListDefinition
			{
				ID = $"ODataV4List",
				Name = "ODataV4 List",
				Description = "Returns data from ODataV4 sources",
				PropertyInputPossible = true,
			};
		}

		protected override FrameworkElement GetControlOverride()
		{
			// return an instance of the UI user control
			return new ODataV4EntityControl();
		}

		protected override void CheckDataOverride(CustomListData data)
		{
			var url = data.Parameter.Split(';')[0];
			var entityUrl = data.Parameter.Split(';')[1];
			var maxRows = data.Parameter.Split(';')[2];
			var entityProperties = data.Parameter.Split(';')[3];
			var authentication = data.Parameter.Split(';')[4];

			if (string.IsNullOrWhiteSpace(url))
			{
				throw new InvalidOperationException("Please provide an URI.");
			}
			if (string.IsNullOrWhiteSpace(entityUrl))
			{
				throw new InvalidOperationException("Please provide an Entity Set.");
			}
			if (string.IsNullOrWhiteSpace(maxRows))
			{
				throw new InvalidOperationException("Please provide a number of rows.");
			}
			if (string.IsNullOrWhiteSpace(entityProperties))
			{
				throw new InvalidOperationException("Please select some Entity Properties.");
			}
			if (string.IsNullOrWhiteSpace(authentication))
			{
				throw new InvalidOperationException("Invalid Authentication properties.");
			}

			if (!int.TryParse(maxRows, out int i))
			{
				throw new InvalidOperationException("Invalid Max. rows property. Please enter a number.");
			}
		}

		protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
		{
			CustomListColumnCollection columnCollection = new CustomListColumnCollection();
			var entityProperties = data.Parameter.Split(';')[3];
			string[] entityPropertiesList = entityProperties.Split(',');

			foreach (string column in entityPropertiesList)
			{
				columnCollection.Add(new CustomListColumn(column, CustomListColumnTypes.String));
			}

			return columnCollection;
		}


		protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
		{
			CustomListObjectElementCollection itemsCollection = new CustomListObjectElementCollection();

			var url = data.Parameter.Split(';')[0];
			var entityUrl = data.Parameter.Split(';')[1];
			var maxRows = data.Parameter.Split(';')[2];
			var entityProperties = data.Parameter.Split(';')[3];
			var authentication = data.Parameter.Split(';')[4];
			var queryOption = data.Parameter.Split(';')[5];

			itemsCollection = ODataV4Service.GetItemsFromEntity(url, entityUrl, entityProperties, int.Parse(maxRows), authentication, queryOption);

			return itemsCollection;
		}
	}
}
