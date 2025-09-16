using System;
using System.Globalization;
using System.IO;
using Peakboard.ExtensionKit;

namespace InfluxDbExtension
{
    [Serializable]
    [CustomListIcon("InfluxDbExtension.pb_datasource_influx.png")]
    public class InfluxDbQueryCustomList : CustomListBase
    {
        protected override CustomListDefinition GetDefinitionOverride()
        {
            return new CustomListDefinition()
            {
                ID = "InfluxDbQueryCustomList",
                Name = "InfluxDB Query Custom List",
                Description = "Query data from InfluxDB",
                PropertyInputDefaults = new CustomListPropertyDefinitionCollection()
                {
                    new CustomListPropertyDefinition()
                    {
                        Name = "URL",
                        Value = ""
                    },
                    new CustomListPropertyDefinition()
                    {
                        Name = "Token",
                        Value = ""
                    },
                    new CustomListPropertyDefinition()
                    {
                        Name = "FluxQuery",
                        Value = "",
                        MultiLine = true
                    }/*,
                    new CustomListPropertyDefinition()
                    {
                        Name = "Type",
                        Value = "flux"
                    }*/
                },
                PropertyInputPossible = true,
            };
        }

        protected override CustomListColumnCollection GetColumnsOverride(CustomListData data)
        {
            data.Properties.TryGetValue("Token", StringComparison.OrdinalIgnoreCase, out var token);
            data.Properties.TryGetValue("URL", StringComparison.OrdinalIgnoreCase, out var url);
            data.Properties.TryGetValue("FluxQuery", StringComparison.OrdinalIgnoreCase, out var query);
            //data.Properties.TryGetValue("Type", StringComparison.OrdinalIgnoreCase, out var type);

            var content = new Content()
            {
                Dialect = new Dialect()
                {
                    Annotations = new[] { "datatype" },
                    CommentPrefix = "#",
                    Delimiter = ",",
                    Header = true
                },
                Query = query,
                Type = "flux"
            };

            var task = QueryHelper.QueryAsync(url, token, content);
            task.Wait();

            var result = task.Result;
            string csv;
            
            if(result.Item2){
                csv = result.Item1;
            }
            else
            {
                throw new Exception(result.Item1);
            }

            var reader = new StringReader(csv);
            string line = reader.ReadLine() ?? throw new Exception("Response is empty");
            string[] datatypes = line.Split(',');
            line = reader.ReadLine() ?? throw new Exception("Response is empty");
            string[] names = line.Split(',');
            reader.Close();

            if (datatypes[0] != "#datatype" || datatypes.Length != names.Length) 
                throw new Exception("Response is broken");

            var cols = new CustomListColumnCollection();
            
            for (int i = 1; i < datatypes.Length; i++)
            {
                switch (datatypes[i])
                {
                    case "long":
                    case "double":
                    case "unsignedLong":
                        cols.Add(new CustomListColumn(names[i], CustomListColumnTypes.Number));
                        break;
                    case "boolean":
                        cols.Add(new CustomListColumn(names[i], CustomListColumnTypes.Boolean));
                        break;
                    default:
                        cols.Add(new CustomListColumn(names[i], CustomListColumnTypes.String));
                        break;
                }
            }

            return cols;
        }

        protected override CustomListObjectElementCollection GetItemsOverride(CustomListData data)
        {
            data.Properties.TryGetValue("Token", StringComparison.OrdinalIgnoreCase, out var token);
            data.Properties.TryGetValue("URL", StringComparison.OrdinalIgnoreCase, out var url);
            data.Properties.TryGetValue("FluxQuery", StringComparison.OrdinalIgnoreCase, out var query);
            data.Properties.TryGetValue("Type", StringComparison.OrdinalIgnoreCase, out var type);

            if (type != "flux")
            {
                this.Log?.Verbose("Other types than flux may lead to errors because they are not supported.");
            }
            
            var content = new Content()
            {
                Dialect = new Dialect()
                {
                    Annotations = new[] { "datatype" },
                    CommentPrefix = "#",
                    Delimiter = ",",
                    Header = true
                },
                Query = query,
                Type = type
            };

            var task = QueryHelper.QueryAsync(url, token, content);
            task.Wait();

            var result = task.Result;
            string csv;
            
            if(result.Item2){
                csv = result.Item1;
            }
            else
            {
                throw new Exception(result.Item1);
            }

            var reader = new StringReader(csv);
            var line = reader.ReadLine() ?? throw new Exception("Response is Empty");
            var datatypes = line.Split(',');
            
            // TODO: Throw exception if response is empty?
            line = reader.ReadLine() ?? throw new Exception("Response is Empty");
            var names = line.Split(',');

            line = reader.ReadLine();

            var items = new CustomListObjectElementCollection();
            
            while (!String.IsNullOrWhiteSpace(line))
            {
                var values = line.Split(',');
                /*if (values[0].Contains("#"))
                {
                    // TODO: Handle more than one table
                    break;
                }*/
                var item = new CustomListObjectElement();
                for (int i = 1; i < values.Length; i++)
                {
                    switch (datatypes[i])
                    {
                        case "long":
                        case "double":
                        case "unsignedLong":
                            if (Double.TryParse(values[i],NumberStyles.Any, CultureInfo.InvariantCulture, out var doubleVal))
                            {
                                item.Add(names[i], doubleVal);
                            }
                            else item.Add(names[i], Double.NaN);
                            break;
                        case "boolean":
                            if (Boolean.TryParse(values[i], out var boolVal))
                            {
                                item.Add(names[i], boolVal);
                            }
                            else item.Add(names[i], false); 
                            break;
                        default:
                            item.Add(names[i], values[i]);
                            break;
                    }
                }
                
                items.Add(item);
                line = reader.ReadLine();
            }

            return items;
        }
    }
}