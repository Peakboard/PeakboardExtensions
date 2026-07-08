# Peakboard Extension: Network Files

This extension lists files (and optionally folders) of a folder on a network share from Peakboard. It connects to the target server via an authenticated SMB session and enumerates the contents of a UNC path.

## How authentication works

The extension establishes the SMB session against a connectable anchor rather than the deep folder path:

1. `\\server\IPC$` (always exists, needs no share or NTFS permission)
2. `\\server\share` (share root) as a fallback

The deep folder path is only used afterwards for the actual file enumeration, where the configured user's normal NTFS permissions apply. This avoids `ERROR_BAD_NETPATH (53)` failures that occur when no SMB session to the server exists yet (typical on a headless player/box).

> **Note:** If NTLM is disabled by policy, Kerberos requires a hostname (SPN). In that case use the server's hostname/FQDN in `UNCFolder` (e.g. `\\fileserver.domain.local\share\...`) instead of an IP address.

## Custom List: Network Files

Lists the files of a folder on a network path, with optional subfolder traversal and folder entries.

### Configuration

| Parameter | Description | Default |
|-----------|-------------|---------|
| Domain | Domain of the user account (optional; leave empty for local accounts) | `domain` |
| User | Username for network share authentication | `johndoe` |
| Password | Password for network share authentication (masked) | |
| UNCFolder | UNC path to the folder to enumerate | `\\server\folder` |
| CheckSubfolders | If `True`, recurses into subfolders | `False` |
| AddFolders | If `True`, includes folder entries and adds an `IsFolder` column | `False` |

### Output columns

| Column | Type | Description |
|--------|------|-------------|
| Path | String | Full path of the file or folder |
| Name | String | File or folder name |
| LastModified | String | Last write time, formatted `yyyyMMddHHmmss` (empty for folders) |
| IsFolder | Boolean | Only present when `AddFolders` is `True`; `True` for folders, `False` for files |

## Installation

1. Download `NetworkFiles.zip` from the `Binaries` folder.
2. Add the extension to Peakboard Designer via **Manage Extensions**.
3. Add a new data source and select **Network Files** from the extensions list.
4. Configure the connection properties (Domain, User, Password, UNCFolder, …).