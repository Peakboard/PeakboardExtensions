using Peakboard.ExtensionKit;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace SickExtension
{
    [ExtensionIcon("Sick.Sick.png")]
    internal class SickListBase : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "SickCustomList",
                Name = "Sick Statement",
                Description = "Returns data from a Sick sensor",
                PropertyInputPossible = true,
                PropertyInputDefaults = {
                    new CustomListPropertyDefinition() { Name = "Host", Value = "localhost", EvalParameters = true },
                    new CustomListPropertyDefinition() { Name = "Port", Value = "2111" }, // cola-b 2112; cola-a 2111
                    new CustomListPropertyDefinition() { Name = "Payload", Value = "sRN DeviceIdent", EvalParameters = true },
                },
            };
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            var columns = new CustomListColumnCollection()
            {
                new CustomListColumn("Response", CustomListColumnTypes.String),
            };

            return columns;
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            var resultValue = LoadData(data);
            var items = new CustomListObjectElementCollection()
            {
                new CustomListObjectElement {
                    { "Response", resultValue }
                },
            };
            return items;
        }

        protected string LoadData(CustomListData data)
        {
            try
            {
                Int32 port = Convert.ToInt32(data.Properties["Port"]);
                var host = data.Properties["Host"];
                var client = new TcpClient(host, port);

                var message = data.Properties["Payload"];


                // convert payload to CoLa-A
                //char STX = (char)2;
                //char ETX = (char)3;

                Byte[] byteData = System.Text.Encoding.ASCII.GetBytes($"\x02{message}\x03");

                using (NetworkStream stream = client.GetStream())
                {
                    stream.Write(byteData, 0, byteData.Length);
                    byteData = new Byte[256];
                    String responseData = String.Empty;

                    Int32 bytes = stream.Read(byteData, 0, byteData.Length);
                    responseData = System.Text.Encoding.ASCII.GetString(byteData, 0, bytes);

                    // remove start and end char
                    responseData = responseData.TrimStart(trimChars: '\x02').TrimEnd(trimChars: '\x03');

                    return responseData;
                }
            }
            catch (ArgumentNullException e)
            {
                return $"ArgumentNullException: {e}";
            }
            catch (SocketException e)
            {
                return $"SocketException: {e}";
            }
            catch(Exception e)
            {
                return $"Exception: {e}";
            }
        }
    }
}
