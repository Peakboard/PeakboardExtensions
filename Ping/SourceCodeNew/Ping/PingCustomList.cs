using System;
using System.Net.NetworkInformation;
using Peakboard.ExtensionKit;

namespace Ping
{
    [Serializable]
    [CustomListIcon("Ping.Ping.png")]
    class PingCustomList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition
            {
                ID = "PingCustomList",
                Name = "Ping",
                Description = "Pings a device and returns OK if it is reachable, NOK otherwise.",
                PropertyInputPossible = true,
                PropertyInputDefaults =
                {
                    new CustomListPropertyDefinition() { Name = "Device", Value = "127.0.0.1" },
                }
            };
        }

        protected override void CheckDataOverride(CustomListData data)
        {
            if (string.IsNullOrWhiteSpace(data.Properties["Device"]))
            {
                throw new InvalidOperationException("Invalid Device. Please provide one or more IP addresses or hostnames, separated by commas.");
            }
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            return new CustomListColumnCollection
            {
                new CustomListColumn("Device", CustomListColumnTypes.String),
                new CustomListColumn("Result", CustomListColumnTypes.String),
            };
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            var devices = data.Properties["Device"]
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(d => d.Trim())
                .Where(d => !string.IsNullOrWhiteSpace(d));

            var items = new CustomListObjectElementCollection();

            foreach (var device in devices)
            {
                var result = "NOK";

                try
                {
                    using var ping = new System.Net.NetworkInformation.Ping();
                    var reply = ping.Send(device, 2000);
                    if (reply != null && reply.Status == IPStatus.Success)
                    {
                        result = "OK";
                    }
                }
                catch (Exception ex)
                {
                    this.Log.Warning($"Ping to '{device}' failed: {ex.Message}");
                }

                var item = new CustomListObjectElement();
                item.Add("Device", device);
                item.Add("Result", result);
                items.Add(item);
            }

            return items;
        }
    }
}
