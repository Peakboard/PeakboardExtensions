# Peakboard Extension: LDAP

This extension allows Peakboard to query LDAP directory services such as Microsoft Active Directory. Use it to display user, group, or organizational data on your dashboards.

## Custom List: LDAP Query

Execute LDAP queries and return the results as a Peakboard data source.

### Configuration

| Parameter | Description |
|-----------|-------------|
| Server | LDAP server hostname or IP address |
| Port | LDAP port (default: 389, LDAPS: 636) |
| Base DN | Base distinguished name for the search |
| Filter | LDAP search filter |
| Credentials | Bind DN and password for authentication |

## Installation

1. Download the ZIP file from the `Binaries` folder.
2. Add the extension to Peakboard Designer via **Manage Extensions**.
3. Add a new data source and select **LDAP** from the extensions list.
