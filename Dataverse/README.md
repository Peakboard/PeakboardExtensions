# Peakboard Extension: Microsoft Dataverse

This extension connects Peakboard to [Microsoft Dataverse](https://learn.microsoft.com/en-us/power-apps/maker/data-platform/data-platform-intro), the data backbone of the Microsoft Power Platform. It supports both entity-based queries and FetchXML queries.

## Custom Lists

### Dataverse Entity
Browse and query Dataverse tables (entities) directly. Select a table and retrieve its data.

### Dataverse FetchXML
Execute [FetchXML](https://learn.microsoft.com/en-us/power-apps/developer/data-platform/use-fetchxml-construct-query) queries for advanced filtering, sorting, and aggregation.

## Configuration

| Parameter | Description |
|-----------|-------------|
| Environment URL | Dataverse environment URL (e.g., `https://yourorg.crm.dynamics.com`) |
| Tenant ID | Azure AD tenant ID |
| Client ID | Azure AD app registration client ID |
| Client Secret | Azure AD app registration client secret |

## Documentation

[Master of the Dataverse - How to connect your Peakboard App to the Microsoft Power Platform](https://how-to-dismantle-a-peakboard-box.com/Master-of-the-Dataverse-How-to-connect-your-Peakboard-App-to-the-Microsoft-Power-Platform.html)

## Installation

1. Download `DataverseNew.zip` from the `Binaries` folder.
2. Add the extension to Peakboard Designer via **Manage Extensions**.
3. Add a new data source and select the desired Dataverse custom list.

## Release Notes

2026-01-15 Initial Release
