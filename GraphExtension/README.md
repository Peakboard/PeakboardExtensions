# Peakboard Extension: Microsoft Graph API

This extension connects Peakboard to the [Microsoft Graph API](https://learn.microsoft.com/en-us/graph/overview), providing access to Office 365, Azure AD, Teams, SharePoint, and other Microsoft cloud services.

## Custom Lists

### MS Graph (User Auth)
Access Microsoft Graph data using delegated user authentication. Requires the user to sign in with their Microsoft account.

### MS Graph (App-Only Auth)
Access Microsoft Graph data using application-level authentication. Useful for unattended scenarios where no user is present.

### MS Graph Functions
Provides custom functions for calling Microsoft Graph API endpoints from Peakboard scripts.

## Configuration

| Parameter | Description |
|-----------|-------------|
| Tenant ID | Azure AD tenant identifier |
| Client ID | Application (client) ID from Azure AD app registration |
| Client Secret | Client secret (for app-only auth) |
| API Endpoint | The Graph API endpoint URL to query |

## Documentation

- [MS Graph API -- Understand the basics and get started](https://how-to-dismantle-a-peakboard-box.com/MS-Graph-API-Understand-the-basis-and-get-started)
- [Reading and writing SharePoint lists with Graph extension](https://how-to-dismantle-a-peakboard-box.com/Reading-and-writing-Sharepoint-lists-with-Graph-extension)
- [MS Teams communication via MS Graph API extension -- Peakboard Help](https://help.peakboard.com/data_sources/Extension/en-MsGraphAPI-teams.html)
- [Microsoft Graph API Extension -- Peakboard Help](https://help.peakboard.com/data_sources/Extension/en-MsGraphAPI.html)

## Installation

1. Download `GraphExtension.zip` from the `Binaries` folder.
2. Add the extension to Peakboard Designer via **Manage Extensions**.
3. Add a new data source and select the desired Microsoft Graph custom list.
