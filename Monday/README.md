# Peakboard Extension for Monday.com
This extension enables easy access to the Monday.com API for the Peakboard Designer. Monday.com is a Cloud-based platform that allows companies to create their own applications and work management software. It is designed to help teams and organizations increase operational efficiency by tracking projects and workflows, visualizing data, and team collaboration.<br>
If you're only intested in the using the extension, just download the zip file in the Binary
folder and install the extension by using the Manage dialog.

The extension has two Custom List, the Monday Querying list, where you must specify a GraphQL query, and the Monday Data by Board, where you must select a board.

The default UI of the Monday Querying list looks like this:

![image](https://user-images.githubusercontent.com/82028654/135236787-22fef2b6-561d-4237-b5a1-6380c4d7f0e9.png)

The default UI of the Monday Data by Board looks like this:

![image](https://user-images.githubusercontent.com/82028654/135236701-a8139beb-d97e-4e25-be56-29486f23cb35.png)

In both list, you have to specify the **API Url**. By default, it is set to https://api.monday.com/v2/ and it should not be changed. You also must enter your **Authorization Token** in order to connect to your account. 


## How to get the Authorization Token

The Authorization Token is personnal to your account and grant access to every query you are allowed to perform in your account.
Once you are connected on the Monday.com web application, please follow the steps on the screens below from the home page.

![image](https://user-images.githubusercontent.com/82028654/135240285-cccf6e49-5a1d-44d2-9352-ed4991a62f63.png)

![image](https://user-images.githubusercontent.com/82028654/135240763-d5baa897-40b2-4fdc-80d8-31703276d80a.png)

![image](https://user-images.githubusercontent.com/82028654/135240952-3652efb8-8662-48db-a623-1afd3968f700.png)


## Getting started with the extension:

### Retrieve the data by using a GraphQL query

If you want to retrieve the data by using a GraphQL, simply type your query in the corresponding field. Then click on the "Load Data" button on the top-right corner.

*If you want to learn more about GraphQL, you can check this guide : https://api.developer.monday.com/docs/introduction-to-graphql.
You can also try your queries here : https://monday.com/developers/v2/try-it-yourself.*

![image](https://user-images.githubusercontent.com/82028654/135249892-7758f7b8-86cb-4bdb-b57f-72581e52a2d0.png)

### Retrieve the data by selecting a board

If you want to retrieve the data by selecting a board, once you have enterred your connection properties, click on the "Connect" button. It should retrieve all the boards from your account in the dropdown list.

![image](https://user-images.githubusercontent.com/82028654/135249463-9505cba2-08f5-4bc4-b69c-b92ed8caa097.png)

You can now select a group. Then click on the "Load Data" button on the top-right corner.

*A board is composed of one or different groups, by default it is set to "All Groups" in order to retrieve all the data from the board.*

![image](https://user-images.githubusercontent.com/82028654/135249263-94059495-0129-4eb1-9537-fd79083122a6.png)


If you encounter any troubles, please contact support@peakboard.com

# Release notes
2021-18-06 Initial Release
