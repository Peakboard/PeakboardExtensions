using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;

// You find a full description of the API from AVM here:
// https://avm.de/fileadmin/user_upload/Global/Service/Schnittstellen/AHA-HTTP-Interface.pdf
namespace AVMFritz
{
    public class FritzHelper
    {
        public FritzHelper()
        {
            InitiateSSLTrust();
        }

        #region Calls

        public List<FritzThermostat> GetThermostats(string hostname, string user, string password)
        {
            var thermostats = new List<FritzThermostat>();

            var xml = GetDevices(hostname, GetSessionId(hostname, user, password));
            foreach (var device in xml.Elements("device"))
            {
                var attributes = device.Attributes();

                if (attributes.Any(a => a.Name.LocalName.Equals("productname")))
                {
                    // May you have to improve the error handling if elements are not existing here. Or if Value cannot be parsed to int.
                    var productname = attributes.First(a => a.Name.LocalName.Equals("productname"));
                    if (productname.Value.Equals("FRITZ!DECT 301"))
                    {
                        thermostats.Add(new FritzThermostat() { Id = attributes.First(a => a.Name.LocalName.Equals("identifier")).Value,
                            Name = device.Element("name").Value,
                            Battery = int.Parse(device.Element("battery").Value),
                            Present = device.Element("present").Value == "1" ? true : false,
                            TempCurrent = double.Parse(device.XPathSelectElement("hkr/tist").Value) / 2,
                            // Target delivers strange high values if the thermostat is turned off
                            TempTarget = double.Parse(device.XPathSelectElement("hkr/tsoll").Value) > 100 ? -1 : double.Parse(device.XPathSelectElement("hkr/tsoll").Value) / 2
                        } );
                    }
                }
            }

            return thermostats;
        }

        public bool SetThermostatTemperature(string hostname, string user, string password, FritzThermostat thermostat, double temp)
        {
            var sid = GetSessionId(hostname, user, password);

            var client = new WebClient() { Encoding = Encoding.UTF8 };
            var resp = client.DownloadString(
                string.Format($@"http://{hostname}/webservices/homeautoswitch.lua?switchcmd=sethkrtsoll&sid={sid}&ain={thermostat.Id}&param={temp*2}")
                ).TrimEnd('\n');

            return resp.Equals(temp.ToString()) ? true : false;
        }

        #endregion

        // Sourcecode from AVM https://avm.de/fileadmin/user_upload/Global/Service/Schnittstellen/AVM_Technical_Note_-_Session_ID.pdf
        #region Create SID

        public string GetSessionId(string hostname, string benutzername, string kennwort)
        {
            XDocument doc = XDocument.Load($@"http://{hostname}/login_sid.lua");
            string sid = GetValue(doc, "SID");
            if (sid == "0000000000000000")
            {
                string challenge = GetValue(doc, "Challenge");
                string uri = $@"http://{hostname}/login_sid.lua?username=" + benutzername + @"&response=" + GetResponse(challenge, kennwort);
                doc = XDocument.Load(uri);
                sid = GetValue(doc, "SID");
            }
            return sid;
        }

        private string GetResponse(string challenge, string kennwort)
        {
            return challenge + "-" + GetMD5Hash(challenge + "-" + kennwort);
        }

        private string GetMD5Hash(string input)
        {
            MD5 md5Hasher = MD5.Create();
            byte[] data = md5Hasher.ComputeHash(Encoding.Unicode.GetBytes(input));
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sb.Append(data[i].ToString("x2"));
            }
            return sb.ToString();
        }

        private string GetValue(XDocument doc, string name)
        {
            XElement info = doc.FirstNode as XElement;
            return info.Element(name).Value;
        }

        #endregion

        #region Helper Methods

        private void InitiateSSLTrust()
        {
            try
            {
                //Change SSL checks so that all checks pass
                ServicePointManager.ServerCertificateValidationCallback =
                   new RemoteCertificateValidationCallback(
                        delegate
                        { return true; }
                    );
            }
            catch
            {
            }
        }

        private XElement GetDevices(string hostname, string sid)
        {
            var client = new WebClient() { Encoding = Encoding.UTF8 };
            var resp = client.DownloadString(string.Format($@"http://{hostname}/webservices/homeautoswitch.lua?switchcmd=getdevicelistinfos&sid={sid}"));
            return XElement.Parse(resp);
        }

        #endregion
    }
}