using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Peakboard.ExtensionKit;
namespace CheckMkExtension
{
    [Serializable]
    [ExtensionIcon("ServerExtension.icon.png")]
    public class CheckMkExtensionCustomList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "ServerExtensionCustomList",
                Name = "CheckMkExtension Api",
                Description = "Interface with Server und Host Problems",
                PropertyInputPossible = true,
                PropertyInputDefaults =
                {
                    new CustomListPropertyDefinition(){Name = "IP",Value="Enter CheckMk Server IP or host here"},
                    new CustomListPropertyDefinition(){Name = "LiveStatusPort",Value="6557"}
                }
            };
        }
        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            return new CustomListColumnCollection
            {
              new CustomListColumn("Host",CustomListColumnTypes.String),
              new CustomListColumn("Service",CustomListColumnTypes.String),
              new CustomListColumn("Summary",CustomListColumnTypes.String),
              new CustomListColumn("State",CustomListColumnTypes.String),
            };
        }
        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            var ip = data.Properties["IP"];
            var port = data.Properties["LiveStatusPort"];

            try
            {
                var problems = GetProblems(ip, port);
                var problemsCustomList = new CustomListObjectElementCollection();
                if (problems == null || problems.Count == 0)
                {
                    return problemsCustomList;
                }

                for (int i = 0; i < problems.Count; i++)
                {
                    var row = problems[i] as JArray;
                    if (row == null || row.Count < 4)
                        continue;

                    problemsCustomList.Add(new CustomListObjectElement
                    {
                        { "Host", row[0]?.ToString() ?? string.Empty },
                        { "Service", row[1]?.ToString() ?? string.Empty },
                        { "Summary", row[2]?.ToString() ?? string.Empty },
                        { "State", GetTypeOfError(row[3]) }
                    });
                }
                return problemsCustomList;
            }
            catch (ParseReplyException ex)
            {
                var preview = Preview(ex.Reply, 2000);
                return new CustomListObjectElementCollection
                {
                    new CustomListObjectElement
                    {
                        {"Host", string.Empty},
                        {"Service", "ParseError"},
                        {"Summary", $"{ex.Message}. Raw response preview: {preview}"},
                        {"State", "UNKNOWN"}
                    }
                };
            }
            catch (Exception ex)
            {
                // Generic fallback so user sees the error in the list as well
                return new CustomListObjectElementCollection
                {
                    new CustomListObjectElement
                    {
                        {"Host", string.Empty},
                        {"Service", "Error"},
                        {"Summary", ex.Message},
                        {"State", "UNKNOWN"}
                    }
                };
            }
        }

        private JArray GetProblems(string ip, string port)
        {
            if (string.IsNullOrWhiteSpace(ip))
                throw new Exception("IP/Host is empty.");
            if (!int.TryParse(port, out var _port))
                throw new Exception("LiveStatusPort must be a number. Default 6557.");

            // Livestatus query (Json) - terminate with extra newline
            string query = "GET services\nColumns: host_name description plugin_output state\nFilter: state >= 1\nOutputFormat: json\n\n";

            try
            {
                var endpoint = new IPEndPoint(ResolveIPAddress(ip), _port);
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                {
                    ReceiveTimeout = 8000,
                    SendTimeout = 8000
                };

                socket.Connect(endpoint);

                byte[] requestBytes = Encoding.UTF8.GetBytes(query);
                socket.Send(requestBytes);
                socket.Shutdown(SocketShutdown.Send);

                var buffer = new byte[8192];
                var resBuilder = new StringBuilder();
                int bytesRead;
                while ((bytesRead = socket.Receive(buffer)) > 0)
                {
                    resBuilder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                }

                string reply = resBuilder.ToString();

                if (string.IsNullOrWhiteSpace(reply))
                {
                    return new JArray();
                }

                // Common non-JSON responses we want to surface with raw text
                if (reply.StartsWith("HTTP/", StringComparison.OrdinalIgnoreCase) ||
                    reply.StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase) ||
                    reply.StartsWith("<html", StringComparison.OrdinalIgnoreCase) ||
                    reply.StartsWith("ERROR", StringComparison.OrdinalIgnoreCase) ||
                    reply.StartsWith("OMD", StringComparison.OrdinalIgnoreCase) ||
                    reply.StartsWith("Livestatus", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ParseReplyException("Received a non-JSON response from Livestatus.", reply);
                }

                reply = reply.Trim();
                try
                {
                    if (reply.StartsWith("["))
                    {
                        return JArray.Parse(reply);
                    }
                    else if (reply.StartsWith("{"))
                    {
                        var obj = JObject.Parse(reply);
                        if (obj.TryGetValue("result", out var resultToken) && resultToken is JArray arr)
                        {
                            return arr;
                        }
                        throw new ParseReplyException("Unexpected JSON object received from Livestatus.", reply);
                    }
                    else
                    {
                        throw new ParseReplyException("Unexpected non-JSON response from Livestatus.", reply);
                    }
                }
                catch (JsonReaderException jex)
                {
                    throw new ParseReplyException($"JSON parse failed: {jex.Message}", reply);
                }
            }
            catch (SocketException sex)
            {
                throw new Exception($"Socket error connecting to {ip}:{_port} - {sex.Message}");
            }
            catch (ParseReplyException)
            {
                throw; // bubble up for UI display
            }
            catch (Exception ex)
            {
                throw new Exception($"CheckMK Livestatus query failed: {ex.Message}");
            }
        }

        private static IPAddress ResolveIPAddress(string host)
        {
            if (IPAddress.TryParse(host, out var ip))
                return ip;
            var addresses = Dns.GetHostAddresses(host);
            var ipv4 = addresses.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork);
            if (ipv4 != null) return ipv4;
            if (addresses.Length > 0) return addresses[0];
            throw new Exception("Unable to resolve host name.");
        }

        private string GetTypeOfError(JToken number)
        {
            int error;
            try
            {
                error = number.Value<int>();
            }
            catch
            {
                if (!int.TryParse(number?.ToString(), out error))
                    error = -1;
            }

            return error switch
            {
                1 => "WARN",
                2 => "CRIT",
                3 => "UNKNOWN",
                0 => "OK",
                _ => string.Empty
            };
        }

        private static string Preview(string? text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            if (text.Length <= maxLength) return text;
            return text.Substring(0, maxLength) + $"...(truncated, totalLength={text.Length})";
        }
    }

    internal class ParseReplyException : Exception
    {
        public string Reply { get; }
        public ParseReplyException(string message, string reply) : base(message)
        {
            Reply = reply ?? string.Empty;
        }
    }
}
