using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
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
                    new CustomListPropertyDefinition(){Name = "Ip",Value="Enter CheckMk Server ip here"},
                    new CustomListPropertyDefinition(){Name = "Port",Value="Enter your API port here"}
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
            var problems = GetProblems(data.Properties["Ip"], data.Properties["Port"]);
            if (problems != null)
            {
                var problemsCustomList = new CustomListObjectElementCollection();
               for (int i = 0; i < problems.Count; i++) {
                   
                    problemsCustomList.Add(new CustomListObjectElement 
                    {
                        { "Host", problems[i][0].ToString() },
                        { "Service", problems[i][1].ToString() },
                        { "Summary", problems[i][2].ToString() },
                        { "State", GetTypeOfError(problems[i][3]) }
                     });                                      
                }            
                return problemsCustomList;
            }
            throw new NotImplementedException("null problems");
        }
        private JArray GetProblems(string ip,string port)  
        {          
            int _port;
            bool isTokenInt  = int.TryParse(port, out _port);
            string query = "GET services\nColumns: host_name description plugin_output state\nFilter: state >= 1\nOutputFormat: json\n";
            if (isTokenInt) 
            {
                try
                { 
                    var endpoint = new IPEndPoint(IPAddress.Parse(ip), _port);
                    Socket socket = new Socket(AddressFamily.InterNetwork,SocketType.Stream, ProtocolType.Tcp);
                    socket.Connect(endpoint);
                    byte[] requestBytes = Encoding.UTF8.GetBytes(query);
                    socket.Send(requestBytes);
                    socket.Shutdown(SocketShutdown.Send);
                    byte[] buffer = new byte[4096];
                    var resBuilder = new StringBuilder();
                    int bytesRead;
                    while ((bytesRead = socket.Receive(buffer)) > 0) 
                    {
                        resBuilder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                    }                   
                    string reply = resBuilder.ToString();
                    if (reply != null || reply != "")
                    {
                        var services = JArray.Parse(reply);
                        return services;
                    }
                }
                catch (Exception ex) 
                {
                    throw new Exception(ex.Message);
                }
            }
            throw new NotImplementedException("the token must be a digit!");           
        }
        private string GetTypeOfError(JToken number)
        {
            int error = int.Parse((string)number);
            switch (error)
            {
                case 1:
                   return "WARN"; 
                case 2:
                   return "CRIT"; 
                case 3:
                   return "UNKNOWN";
                default:
                    return "";
            }
        }
    }
}
