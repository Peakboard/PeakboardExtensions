# Peakboard Extension: Microsoft Dataverse

This extension connects Peakboard to [Microsoft Dataverse](https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-intro), the data backbone of the Microsoft Power Platform. It lets you query Dataverse tables directly from your Peakboard applications using either simple entity queries or advanced FetchXML.

## Data Sources

### Dataverse Entities

Query Dataverse tables by specifying the entity name and the attributes (columns) you want to retrieve. Supports configurable row limits up to 100,000 rows.

| Parameter | Description |
|-----------|-------------|
| DataverseURL | Your environment URL, e.g. `https://yourorg.crm4.dynamics.com/` (must end with `/`) |
| ClientId | Azure AD app registration client ID |
| ClientSecret | Azure AD app registration client secret |
| TenantId | Azure AD tenant ID |
| Entity | Logical name of the table, e.g. `account` |
| Attributes | Comma-separated list of column names, e.g. `accountid,name,emailaddress1,telephone1` |
| MaxRows | Maximum number of rows to return (1 - 100,000, default 10) |

### Dataverse FetchXML

Run [FetchXML](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/use-fetchxml-construct-query) queries for advanced filtering, sorting, aggregation, and linked-entity joins. Columns are derived from the FetchXML `<attribute>` elements, and linked-entity attributes are returned with their alias prefix (e.g. `contact.fullname`).

| Parameter | Description |
|-----------|-------------|
| DataverseURL | Your environment URL (same as above) |
| ClientId | Azure AD app registration client ID |
| ClientSecret | Azure AD app registration client secret |
| TenantId | Azure AD tenant ID |
| FetchXML | The full FetchXML query (multiline) |

## Type Mapping

The extension automatically maps Dataverse column types to Peakboard types:

| Dataverse Type | Peakboard Type |
|----------------|----------------|
| Boolean | Boolean |
| Integer, BigInt, Double, Decimal, Money | Number |
| OptionSet (Picklist) | String (resolved to display label) |
| EntityReference (Lookup) | String (GUID) |
| All other types | String |

## Authentication

This extension uses **Azure AD App Registration** with a client secret (OAuth 2.0 client credentials flow). To set it up:

1. Register an application in [Azure AD](https://portal.azure.com/#blade/Microsoft_AAD_RegisteredApps/ApplicationsListBlade).
2. Create a client secret under **Certificates & secrets**.
3. In the [Power Platform Admin Center](https://admin.powerplatform.microsoft.com/), add the app as an **Application User** with appropriate security roles.
4. Note the **Client ID**, **Client Secret**, and **Tenant ID** for use in the extension configuration.

## Installation

1. Download `DataverseNew.zip` from the `Binaries` folder.
2. In Peakboard Designer, go to **Manage Extensions** and add the zip file.
3. Add a new data source and select either **Dataverse Entities** or **Dataverse FetchXML**.

## Documentation

[Master of the Dataverse - How to connect your Peakboard App to the Microsoft Power Platform](https://how-to-dismantle-a-peakboard-box.com/Master-of-the-Dataverse-How-to-connect-your-Peakboard-App-to-the-Microsoft-Power-Platform.html)

## Release Notes

| Version | Date | Changes |
|---------|------|---------|
| 1.0 | 2026-01-15 | Initial release with entity query support |
| 2.0 | | Added FetchXML data source |
| 2.3 | | Fixed FetchXML column misalignment and null attribute handling |
