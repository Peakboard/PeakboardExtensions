using System;
using System.ComponentModel;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace NetworkFiles;

public sealed class NetworkConnection : IDisposable
{
    private const int NO_ERROR = 0;
    private const int ERROR_ACCESS_DENIED = 5;
    private const int ERROR_BAD_NET_NAME = 67;
    private const int ERROR_BAD_NETPATH = 53;
    private const int ERROR_SESSION_CREDENTIAL_CONFLICT = 1219;
    private const int ERROR_LOGON_FAILURE = 1326;
    private const int ERROR_NTLM_BLOCKED = 1937;

    public string NetworkName { get; }

    private string? _connectedAnchor;

    public NetworkConnection(string networkName, NetworkCredential credentials)
    {
        if (string.IsNullOrWhiteSpace(networkName))
            throw new ArgumentException("UNC path must not be empty.", nameof(networkName));

        NetworkName = networkName.TrimEnd('/', '\\');

        var server = ExtractServer(NetworkName)
            ?? throw new ArgumentException($"'{networkName}' is not a valid UNC path (\\\\server\\share\\...).", nameof(networkName));

        var shareRoot = ExtractShareRoot(NetworkName);

        var userName = string.IsNullOrEmpty(credentials.Domain)
            ? credentials.UserName
            : $@"{credentials.Domain}\{credentials.UserName}";

        var anchors = shareRoot is not null
            ? new[] { $@"\\{server}\IPC$", shareRoot }
            : new[] { $@"\\{server}\IPC$" };

        int lastError = NO_ERROR;

        foreach (var anchor in anchors)
        {
            var isIpc = anchor.EndsWith(@"\IPC$", StringComparison.OrdinalIgnoreCase);

            var netResource = new NetResource
            {
                Scope = ResourceScope.GlobalNetwork,
                ResourceType = isIpc ? ResourceType.Any : ResourceType.Disk,
                DisplayType = ResourceDisplaytype.Share,
                RemoteName = anchor
            };

            var result = WNetAddConnection2(netResource, credentials.Password, userName, 0);

            switch (result)
            {
                case NO_ERROR:
                    _connectedAnchor = anchor;
                    return;

                case ERROR_SESSION_CREDENTIAL_CONFLICT:
                    _connectedAnchor = null;
                    return;

                case ERROR_LOGON_FAILURE:
                    throw new UnauthorizedAccessException(
                        $"Invalid credentials for user '{userName}' on server '{server}'.");

                case ERROR_NTLM_BLOCKED:
                    throw new UnauthorizedAccessException(
                        $"NTLM authentication is disabled on '{server}' or by group policy, " +
                        $"and Kerberos requires a hostname (SPN). " +
                        $"Use the server's hostname/FQDN in 'UNCFolder' instead of an IP address " +
                        $"(e.g. \\\\fileserver.domain.local\\share\\... instead of \\\\{server}\\...).");

                case ERROR_ACCESS_DENIED:
                    lastError = result;
                    continue;

                case ERROR_BAD_NETPATH:
                case ERROR_BAD_NET_NAME:
                    lastError = result;
                    continue; // try next anchor

                default:
                    lastError = result;
                    continue;
            }
        }

        throw new Win32Exception(lastError == NO_ERROR ? ERROR_BAD_NETPATH : lastError,
            $"Could not establish an SMB session to server '{server}' " +
            $"(tried: {string.Join(", ", anchors)}). Win32 error: {lastError}.");
    }

    private static string? ExtractServer(string uncPath)
    {
        var m = Regex.Match(uncPath, @"^\\\\([^\\]+)");
        return m.Success ? m.Groups[1].Value : null;
    }

    private static string? ExtractShareRoot(string uncPath)
    {
        var m = Regex.Match(uncPath, @"^(\\\\[^\\]+\\[^\\]+)");
        return m.Success ? m.Groups[1].Value : null;
    }

    ~NetworkConnection()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (_connectedAnchor is not null)
        {
            _ = WNetCancelConnection2(_connectedAnchor, 0, false);
            _connectedAnchor = null;
        }
    }

#pragma warning disable CA2101 
    [DllImport("mpr.dll", CharSet = CharSet.Ansi)]
    private static extern int WNetAddConnection2(NetResource netResource, string password, string username, int flags);

    [DllImport("mpr.dll", CharSet = CharSet.Ansi)]
    private static extern int WNetCancelConnection2(string name, int flags, bool force);
#pragma warning restore CA2101
}

[StructLayout(LayoutKind.Sequential)]
public class NetResource
{
    public ResourceScope Scope;
    public ResourceType ResourceType;
    public ResourceDisplaytype DisplayType;
    public int Usage;
    public string? LocalName;
    public string? RemoteName;
    public string? Comment;
    public string? Provider;
}

public enum ResourceScope
{
    Connected = 1,
    GlobalNetwork,
    Remembered,
    Recent,
    Context
}

public enum ResourceType
{
    Any = 0,
    Disk = 1,
    Print = 2,
    Reserved = 8
}

public enum ResourceDisplaytype
{
    Generic = 0x0,
    Domain = 0x01,
    Server = 0x02,
    Share = 0x03,
    File = 0x04,
    Group = 0x05,
    Network = 0x06,
    Root = 0x07,
    Shareadmin = 0x08,
    Directory = 0x09,
    Tree = 0x0a,
    Ndscontainer = 0x0b
}