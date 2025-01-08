using MQTTnet.Server;
using MQTTnet;
using Peakboard.ExtensionKit;
using System;
using System.Threading.Tasks;
using System.Diagnostics;

namespace MQTTServer
{
    [Serializable]
    [CustomListIcon("MQTTServer.beer.png")]
    internal class MQTTServerList : CustomListBase
    {
        private MqttServer mqttServer;
        private string listName;

        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "MQTTServerList",
                Name = "MQTTServer List",
                Description = "Hosts a MQTT Server.",
                PropertyInputPossible = true,
                PropertyInputDefaults =
                {
                    new CustomListPropertyDefinition {Name = "Port", Value = "1883"}
                },
                SupportsPushOnly = true,
                Functions =
                {
                    new CustomListFunctionDefinition()
                    {
                        Name = "start"
                    },
                    new CustomListFunctionDefinition()
                    {
                        Name = "stop"
                    }
                }
            };
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            return new CustomListColumnCollection
            {
                new CustomListColumn("State", CustomListColumnTypes.String)
            };
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            return new CustomListObjectElementCollection();
        }

        private async Task MqttServer_StoppedAsync(EventArgs arg)
        {
            var item = new CustomListObjectElement
            {
                { "State", "Stopped" }
            };

            Log?.Verbose("Stopped");

            Data?.Push(listName).Update(0, item);
        }

        private async Task MqttServer_StartedAsync(EventArgs arg)
        {
            var item = new CustomListObjectElement
            {
                { "State", "Running" }
            };

            Log?.Verbose("Started");

            Data?.Push(listName).Update(0, item);
        }

        protected override void SetupOverride(CustomListData data)
        {
            base.SetupOverride(data);

            Log?.Info("Init");
        }

        protected override CustomListExecuteReturnContext ExecuteFunctionOverride(CustomListData data, CustomListExecuteParameterContext context)
        {
            if (context.FunctionName.Equals("start", StringComparison.InvariantCultureIgnoreCase))
            {
                Start(data);
            }
            else if (context.FunctionName.Equals("stop", StringComparison.InvariantCultureIgnoreCase))
            {
                Stop();
            }

            return default;
        }

        private void Start(CustomListData data)
        {
            listName = data?.ListName;

            if (!data.Properties.TryGetValue("Port", StringComparison.OrdinalIgnoreCase, out var port)
                || !int.TryParse(port, out var portInt))
            {
                throw new InvalidOperationException("The port is not defined as a number");
            }


            var mqttServerOptions = new MqttServerOptionsBuilder()
                .WithDefaultEndpoint()
                .WithDefaultEndpointPort(portInt)
                .Build();

            var mqttFactory = new MqttFactory();
            mqttServer = mqttFactory.CreateMqttServer(mqttServerOptions);
            mqttServer.StartAsync();

            mqttServer.StartedAsync += MqttServer_StartedAsync;
            mqttServer.StoppedAsync += MqttServer_StoppedAsync;

            var item = new CustomListObjectElement
            {
                { "State", "Starting" }
            };

            Data?.Push(listName).Add(item);
        }

        private void Stop()
        {
            mqttServer.StopAsync();
            mqttServer.StartedAsync -= MqttServer_StartedAsync;
            mqttServer.StoppedAsync -= MqttServer_StoppedAsync;
            mqttServer?.Dispose();

            var item = new CustomListObjectElement
            {
                { "State", "Stopped" }
            };

            Data?.Push(listName).Update(0, item);
        }

        private void OpenPort(int port)
        {
            string command = "netsh";
            string arguments = "advfirewall firewall add rule name=\"MyTCP1234\" "
                             + "dir=in action=allow protocol=TCP localport=" + port;

            // Process-Start-Info konfigurieren
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                Verb = "runas"   // sorgt dafür, dass mit Admin-Rechten ausgeführt wird (UAC-Prompt)
            };

            try
            {
                using (Process process = new Process())
                {
                    process.StartInfo = psi;
                    process.Start();

                    // Optional: Konsolenausgabe lesen
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    process.WaitForExit();

                    Console.WriteLine("Output:\n" + output);
                    Console.WriteLine("Error:\n" + error);

                    if (process.ExitCode == 0)
                    {
                        Console.WriteLine("Firewall-Regel wurde erfolgreich hinzugefügt.");
                    }
                    else
                    {
                        Console.WriteLine($"Firewall-Regel konnte nicht hinzugefügt werden. Exit Code: {process.ExitCode}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Ausführen des netsh-Befehls: " + ex.Message);
            }

            Console.ReadLine();
        }
    }
}
