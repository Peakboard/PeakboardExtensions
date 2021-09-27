using Microsoft.Crm.Sdk.Messages;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Query;
using Peakboard.ExtensionKit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Description;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PeakboardExtensionMicrosoftDynamics365
{
    [Serializable]
    [CustomListIcon("PeakboardExtensionMicrosoftDynamics365.d365Icon.png")]

    public class CrmList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = $"CRM List",
                Name = "CRM List",
                Description = "Returns data from Microsoft Dynamics 365 Extension",
                PropertyInputPossible = true,
            };
        }


        protected override FrameworkElement GetControlOverride()
        {
            // return an instance of the UI user control
            return new CrmUIControl();
        }

        protected override void CheckDataOverride(CustomListData data)
        {
            var link = data.Parameter.Split(';')[0];
            var username = data.Parameter.Split(';')[1];
            var password = data.Parameter.Split(';')[2];
            var maxRows = data.Parameter.Split(';')[3];
            var logicalNameViewOrTable = data.Parameter.Split(';')[4];
            //var displayNameColumn = data.Parameter.Split(';')[5];
            var logicalNameColumn = data.Parameter.Split(';')[6];
            var chooseEntityOrView = data.Parameter.Split(';')[7];

            if (string.IsNullOrWhiteSpace(link))
            {
                throw new InvalidOperationException("Please provide a link");
            }
            if (string.IsNullOrWhiteSpace(username))
            {
                throw new InvalidOperationException("Please provide a username");
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("Please provide a password");
            }
            if (string.IsNullOrWhiteSpace(maxRows))
            {
                throw new InvalidOperationException("Please provide a number of rows");
            }
            if (string.IsNullOrWhiteSpace(logicalNameViewOrTable))
            {
                throw new InvalidOperationException("Please provide a View or an Entity");
            }

            if (chooseEntityOrView == "Entity")
            {

                if (string.IsNullOrWhiteSpace(logicalNameColumn))
                {
                    throw new InvalidOperationException("Please select some columns");
                }
            }

            if (!int.TryParse(maxRows, out int i))
            {
                throw new InvalidOperationException("Invalid max rows property. Please check carefully!");
            }
        }




        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {

            var columnCollection = new CustomListColumnCollection();

            //try
            //{
                var link = data.Parameter.Split(';')[0];
                var username = data.Parameter.Split(';')[1];
                var password = data.Parameter.Split(';')[2];
                var logicalNameViewOrTable = data.Parameter.Split(';')[4];
                var chooseEntityOrView = data.Parameter.Split(';')[7];

                if (chooseEntityOrView == "View")
                {
                    columnCollection = CrmHelper.GetViewColumns(link, username, password, logicalNameViewOrTable);
                }
                else if (chooseEntityOrView == "Entity")
                {
                    var displayNameColumn = data.Parameter.Split(';')[5];
                    var logicalNameColumn = data.Parameter.Split(';')[6];

                    columnCollection = CrmHelper.GetEntityColumns(link, username, password, logicalNameViewOrTable, displayNameColumn, logicalNameColumn);
                }
                else
                {
                    throw new InvalidOperationException("You have to select View or Entity");
                }
            //}
            //catch (Exception exception)
            //{
            //    throw new InvalidOperationException("Error, please try again");
            //}


            return columnCollection;
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            CustomListObjectElementCollection itemsCollection = new CustomListObjectElementCollection();

            //try
            //{
                var chooseEntityOrView = data.Parameter.Split(';')[7];

                var link = data.Parameter.Split(';')[0];
                var username = data.Parameter.Split(';')[1];
                var password = data.Parameter.Split(';')[2];
                var maxRows = data.Parameter.Split(';')[3];
                var logicalNameViewOrTable = data.Parameter.Split(';')[4];

                if (chooseEntityOrView == "View")
                {
                    itemsCollection = CrmHelper.GetDataFromView(link, username, password, maxRows, logicalNameViewOrTable);
                }
                else if (chooseEntityOrView == "Entity")
                {
                    var displayNameColumn = data.Parameter.Split(';')[5];
                    var logicalNameColumn = data.Parameter.Split(';')[6];

                    itemsCollection = CrmHelper.GetDataFromEntity(link, username, password, maxRows, logicalNameViewOrTable, displayNameColumn, logicalNameColumn);
                }
                else
                {
                    throw new InvalidOperationException("You have to select View or Entity");
                }
            //}
            //catch (Exception exception)
            //{
            //    throw new InvalidOperationException("Error, please try again");
            //}


            return itemsCollection;
        }

        

        
    }
}
