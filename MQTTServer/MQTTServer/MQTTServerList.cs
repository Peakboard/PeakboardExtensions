using MQTTnet.Server;
using MQTTnet;
using Peakboard.ExtensionKit;
using System;
using System.Threading.Tasks;
using System.Diagnostics;

namespace MQTTServer
{
    [Serializable]
    [CustomListIcon("MQTTServer.mqtt.png")]
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

            Log?.Verbose("MQTT Server Started");

            Data?.Push(listName).Update(0, item);
        }

        protected override void SetupOverride(CustomListData data)
        {
            base.SetupOverride(data);

            listName = data?.ListName;

            var item = new CustomListObjectElement
            {
                { "State", "Init" }
            };

            Data?.Push(listName).Update(0, item);
            Log?.Info("MQTT Server initialisation");
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
            if (!data.Properties.TryGetValue("Port", StringComparison.OrdinalIgnoreCase, out var port)
                || !int.TryParse(port, out var portInt))
            {
                throw new InvalidOperationException("The port is not defined as a number");
            }

            var mqttServerOptions = new MqttServerOptionsBuilder()
                .WithDefaultEndpoint()
                .WithDefaultEndpointPort(portInt)
                .Build();

            var item = new CustomListObjectElement
            {
                { "State", "Starting" }
            };

            Data?.Push(listName).Update(0, item);

            var mqttFactory = new MqttFactory();
            mqttServer = mqttFactory.CreateMqttServer(mqttServerOptions);

            mqttServer.StartedAsync += MqttServer_StartedAsync;
            mqttServer.StoppedAsync += MqttServer_StoppedAsync;

            mqttServer.StartAsync().Wait(2000);
        }

        private void Stop()
        {
            mqttServer.StopAsync().Wait(2000);
            mqttServer.StartedAsync -= MqttServer_StartedAsync;
            mqttServer.StoppedAsync -= MqttServer_StoppedAsync;
            mqttServer?.Dispose();

            var item = new CustomListObjectElement
            {
                { "State", "Stopped" }
            };

            Data?.Push(listName).Update(0, item);
        }
    }
}