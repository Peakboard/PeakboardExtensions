using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace POSPrinter.DTO
{
    [XmlRoot(ElementName = "Configuration")]
    public class Configuration
    {
        [XmlElement(ElementName = "Properties")]
        public Properties Properties { get; set; }

        [XmlArray("Peakboards")]
        [XmlArrayItem("Peakboard")]
        public List<Peakboard> Peakboards { get; set; }
    }

    public class Properties
    {
        [XmlElement("Property")]
        public List<Property> PropertyList { get; set; }
    }

    public class Property
    {
        [XmlAttribute(AttributeName = "Name")]
        public string Name { get; set; }

        [XmlText]
        public string Value { get; set; }
    }

    public class Peakboard
    {
        [XmlAttribute(AttributeName = "ID")]
        public string ID { get; set; }

        [XmlElement(ElementName = "UploadTime")]
        public String UploadTime { get; set; }
    }
}
