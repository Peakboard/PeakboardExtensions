# Peakboard Extension for Microsoft Dynamics 365
This extension enables easy access to the Microsoft Dynamics 365 API for the Peakboard Designer. Microsoft Dynamics 365 is a product line of Enterprise Resource Planning (ERP) and Customer Relationship Management (CRM) intelligent business applications developped by Microsoft.
If you're only intested in the using the extension, just download the zip file in the Binary
folder and install the extension by using the Manage dialog.

The default UI of the extension looks like this :

![image](https://user-images.githubusercontent.com/82028654/134884799-7b0ec757-9f43-4a8f-9d7e-521a3c65faa4.png)

In order to connect to the CRM services, you have to specify in the extension the **Organisation Service Link**, the **Username** and the **Password** that you commonly use to connect to Microsoft Dynamics 365.


## How to get the Organisation Service Link:

You have to be connected on the Microsoft Dynamics 365 web application. From the home page, please follow the steps on the screens below.

![image](https://user-images.githubusercontent.com/82028654/134886302-c8874f09-e088-4764-bf7e-bdeb4d97f217.png)

![image](https://user-images.githubusercontent.com/82028654/134886769-58974b8e-d51e-4a30-b21c-f7ed3fd30a82.png)

![image](https://user-images.githubusercontent.com/82028654/134886953-5988a548-da49-4e3d-8307-17399a717d3f.png)

![image](https://user-images.githubusercontent.com/82028654/134887551-26a7f326-738a-469f-8634-ef126939c0f6.png)

Your **Organisation Service Link** should looks like this : **https://**********************/XRMServices/2011/Organization.svc**.

Once you enterred your connection properties, click on the "Connect" button. Then you should be connected to the service.

![image](https://user-images.githubusercontent.com/82028654/134890089-282e64e5-9b03-400b-967b-83ee41af98ce.png)


## Getting started with the extension:

Now you can choose if you want to retrieve the data by selecting a **View** or an **Entity**. The view is the most common way to see the data.

*A **view** is a grid filled with filtered records. It is a type of saved query. Users can select different views to look at a subset of records of the same entity that fit into pre-specified filter conditions. A view is basically shows the filtered records.*

*An **entity** is a table that holds a certain type of data, with the attributes functioning as the columns of the table and determining which information goes into the records of that entity type.*


### Retrieve the data by View:

If you want to retrieve the data by selecting a view, you just have to select one in the dropdown list. Then click on the "Load Data" button on the top-right corner.

![image](https://user-images.githubusercontent.com/82028654/134897091-0e507b5c-7f42-475b-b96c-5bc678604e6f.png)

Also, don't forget to provide a maximum number of rows.

![image](https://user-images.githubusercontent.com/82028654/134899793-5a492872-9fbe-4fc8-a406-62e4f7d6a978.png)


### Retrieve the data by Entity:

If you want to retrieve the data by selecting an entity, you have to select one in the dropdown list. Then click on the "Search Columns" button. You will get all the columns from the selected entity in the list below.

![image](https://user-images.githubusercontent.com/82028654/134897700-5fa5feb2-6d69-46be-9ff5-b18582e8bb8f.png)

You can now select the columns you want by checking the corresponding box. When you have selected all the columns that you wanted, click on the "Load Data" button on the top-right corner.

![image](https://user-images.githubusercontent.com/82028654/134898251-9f28cc4e-695f-4731-a371-c198311b2df6.png)

Also, don't forget to provide a maximum number of rows.

![image](https://user-images.githubusercontent.com/82028654/134899793-5a492872-9fbe-4fc8-a406-62e4f7d6a978.png)

If you encounter any troubles, please contact support@peakboard.com

# Release notes
2021-18-06 Initial Release
