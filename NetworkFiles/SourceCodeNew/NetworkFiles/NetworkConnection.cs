using System;
using System.ComponentModel;
using System.Net;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace NetworkFiles;

public class NetworkConnection : IDisposable
{
    public string NetworkName { get; }

    public NetworkConnection(string networkName, NetworkCredential credentials)
    {
        var originalNetworkName = networkName;

        // Try it first with the ip of the dns
        // Here we get a error if the designer (windows desktop) user is an other user as the credentials
        // this does not happen if we change the dns to ip
        for (var i = 0; i < 2; i++)
        {
            // first try it with the ip
            if (i == 0)
            {
                var rgx = new Regex(@"^\\\\(.*?)\\");
                var dns = rgx.Match(networkName).Value;
                if (!string.IsNullOrEmpty(dns))
                {
                    dns = dns.Trim('\\');
                    var ip = Dns.GetHostAddresses(dns).Length > 0 ? Dns.GetHostAddresses(dns)[0].ToString() : null;

                    if (!string.IsNullOrEmpty(ip))
                    {
                        networkName = Regex.Replace(networkName, @"^\\\\.*?\\", $@"\\{ip}\");
                    }
                }
            }
            // if it did now worked try it with the dns (which mostly will fail then as well)
            else
            {
                networkName = originalNetworkName;
            }

            NetworkName = networkName;

            var netResource = new NetResource { Scope = ResourceScope.GlobalNetwork, ResourceType = ResourceType.Disk, DisplayType = ResourceDisplaytype.Share, RemoteName = networkName };

            var userName = string.IsNullOrEmpty(credentials.Domain)
                ? credentials.UserName
                : $@"{credentials.Domain}\{credentials.UserName}";

            var result = WNetAddConnection2(
                netResource,
                credentials.Password,
                userName,
                0);

            // try it with the original name
            if (result != 0 && i == 0 && !networkName.Equals(originalNetworkName, StringComparison.InvariantCulture))
            {
                continue;
            }

            if (result != 0)
            {
                throw new Win32Exception(result);
            }

            return;
        }
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

    protected virtual void Dispose(bool disposing)
    {
        _ = WNetCancelConnection2(NetworkName, 0, true);
    }

#pragma warning disable CA2101 // This is not working with CharSet Unicode
    [DllImport("mpr.dll", CharSet = CharSet.Ansi)]
    private static extern int WNetAddConnection2(NetResource netResource, string password, string username, int flags);

    [DllImport("mpr.dll", CharSet = CharSet.Ansi)]
    private static extern int WNetCancelConnection2(string name, int flags, bool force);
#pragma warning restore CA2101  // This is not working with CharSet Unicode
}

[StructLayout(LayoutKind.Sequential)]
public class NetResource
{
    public ResourceScope Scope;
    public ResourceType ResourceType;
    public ResourceDisplaytype DisplayType;
    public int Usage;
    public string LocalName;
    public string RemoteName;
    public string Comment;
    public string Provider;
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