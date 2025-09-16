using Peakboard.ExtensionKit;

namespace InfluxDbExtension
{
    
    [ExtensionIcon("InfluxDbExtension.pb_datasource_influx.png")]
    public class InfluxExtension : ExtensionBase
    {
        protected override ExtensionDefinition GetDefinitionOverride()
        {
            return new ExtensionDefinition()
            {
                ID = "InfluxDbExtension",
                Name = "InfluxDB Extension",
                Author = "Yannis Hartmann",
                Version = "1.0",
            };
        }

        protected override CustomListCollection GetCustomListsOverride()
        {
            return new CustomListCollection()
            {
                new InfluxDbQueryCustomList(),
                new InfluxDbWriteCustomList()
            };
        }
    }
}