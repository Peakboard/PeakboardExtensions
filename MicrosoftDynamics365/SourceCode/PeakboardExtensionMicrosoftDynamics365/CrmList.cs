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
            try
            { 
            return new CrmUIControl();
            }
            catch (Exception e1)
            {
                MessageBox.Show(e1.ToString(), "Dynamics Extension");
            }
            return new CrmUIControl();
        }

        protected override void CheckDataOverride(CustomListData data)
        {
            var mysplits = data.Parameter.Split(';');
            string URL = string.Empty;
            string username = string.Empty;
            string password = string.Empty;
            string maxRows = string.Empty; ;
            string logicalNameViewOrTable = string.Empty;
            string displayNameColumn = string.Empty;
            string logicalNameColumn = string.Empty;
            string chooseEntityOrView = string.Empty;
            string clientid = string.Empty;
            string clientsecret = string.Empty;
            string fetchxml = string.Empty;

            if (mysplits.Length >= 8)
            {
                URL = mysplits[0];
                username = mysplits[1];
                password = mysplits[2];
                maxRows = mysplits[3];
                logicalNameViewOrTable = mysplits[4];
                displayNameColumn = mysplits[5];
                logicalNameColumn = mysplits[6];
                chooseEntityOrView = mysplits[7];
            }

            if (mysplits.Length >= 11)
            {
                clientid = mysplits[8];
                clientsecret = mysplits[9];
                fetchxml = mysplits[10];
            }

            if (string.IsNullOrWhiteSpace(URL))
            {
                throw new InvalidOperationException("Please provide a URL");
            }
            
            if (string.IsNullOrWhiteSpace(maxRows))
            {
                throw new InvalidOperationException("Please provide a number of rows");
            }
            if (string.IsNullOrWhiteSpace(logicalNameViewOrTable))
            {
                throw new InvalidOperationException("Please provide a View or an Entity");
            }

            if (!int.TryParse(maxRows, out int i))
            {
                throw new InvalidOperationException("Invalid max rows property. Please check carefully!");
            }
        }




        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {

            var columnCollection = new CustomListColumnCollection();

            var mysplits = data.Parameter.Split(';');
            string URL = string.Empty;
            string username = string.Empty;
            string password = string.Empty;
            string maxRows = string.Empty; ;
            string ObjectName = string.Empty;
            string DisplayNameColumns = string.Empty;
            string LogicalNameColumns = string.Empty;
            string ExtractionType = string.Empty;
            string clientid = string.Empty;
            string clientsecret = string.Empty;
            string fetchxml = string.Empty;

            if (mysplits.Length >= 8)
            {
                URL = mysplits[0];
                username = mysplits[1];
                password = mysplits[2];
                maxRows = mysplits[3];
                ObjectName = mysplits[4];
                DisplayNameColumns = mysplits[5];
                LogicalNameColumns = mysplits[6];
                ExtractionType = mysplits[7];
            }

            if (mysplits.Length >= 11)
            {
                clientid = mysplits[8];
                clientsecret = mysplits[9];
                fetchxml = mysplits[10];
            }

            if (ExtractionType == "View")
            {
                columnCollection = CrmHelper.GetViewColumns(URL, username, password, clientid, clientsecret, ObjectName);
            }
            else if (ExtractionType == "Entity")
            {
                columnCollection = CrmHelper.GetEntityColumns(URL, username, password, clientid, clientsecret, ObjectName);
            }
            else
            {
                throw new InvalidOperationException("You have to select View or Entity");
            }



            return columnCollection;
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            CustomListObjectElementCollection itemsCollection = new CustomListObjectElementCollection();

            var mysplits = data.Parameter.Split(';');
            string URL = string.Empty;
            string username = string.Empty;
            string password = string.Empty;
            string maxRows = string.Empty; ;
            string ObjectName = string.Empty;
            string DisplayNameColumns = string.Empty;
            string LogicalNameColumns = string.Empty;
            string ExtractionType = string.Empty;
            string clientid = string.Empty;
            string clientsecret = string.Empty;
            string fetchxml = string.Empty;

            if (mysplits.Length >= 8)
            {
                URL = mysplits[0];
                username = mysplits[1];
                password = mysplits[2];
                maxRows = mysplits[3];
                ObjectName = mysplits[4];
                DisplayNameColumns = mysplits[5];
                LogicalNameColumns = mysplits[6];
                ExtractionType = mysplits[7];
            }

            if (mysplits.Length >= 11)
            {
                clientid = mysplits[8];
                clientsecret = mysplits[9];
                fetchxml = mysplits[10];
            }

            if (ExtractionType == "View")
            {
                itemsCollection = CrmHelper.GetViewData(URL, username, password, clientid, clientsecret, maxRows, ObjectName);
            }
            else if (ExtractionType == "Entity")
            {
                itemsCollection = CrmHelper.GetDataFromEntity(URL, username, password, clientid, clientsecret, maxRows, ObjectName, DisplayNameColumns, LogicalNameColumns);
            }
            else
            {
                throw new InvalidOperationException("You have to select View or Entity");
            }

            return itemsCollection;
        }

        

        
    }
}
