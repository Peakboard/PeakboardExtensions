using System;
using System.Collections.Generic;
using System.IO;
using System.IO.BACnet;
using System.Linq;
using System.Xml.Serialization;
using Peakboard.ExtensionKit;

namespace BacNetExtension.CustomLists.Helpers
{
    public static class BacNetHelper
    {
        public static bool ReadAllPropertiesBySingle(BacnetClient comm, BacnetAddress adr, BacnetObjectId object_id, out IList<BacnetReadAccessResult> value_list, ref List<BacnetObjectDescription> objectsDescriptionExternal,ref List<BacnetObjectDescription> objectsDescriptionDefault)
        {

            if (objectsDescriptionDefault == null)  // first call, Read Objects description from internal & optional external xml file
            {
                StreamReader sr;
                XmlSerializer xs = new XmlSerializer(typeof(List<BacnetObjectDescription>));

                // embedded resource
                System.Reflection.Assembly _assembly;
                _assembly = System.Reflection.Assembly.GetExecutingAssembly();
                sr = new StreamReader(_assembly.GetManifestResourceStream("BacNetExtension.ReadSinglePropDescrDefault.xml"));
                objectsDescriptionDefault = (List<BacnetObjectDescription>)xs.Deserialize(sr);

                try  // External optional file
                {
                    sr = new StreamReader("ReadSinglePropDescr.xml");
                    objectsDescriptionExternal = (List<BacnetObjectDescription>)xs.Deserialize(sr);
                }
                catch { }

            }

            value_list = null;

            IList<BacnetPropertyValue> values = new List<BacnetPropertyValue>();

            int old_retries = comm.Retries;
            comm.Retries = 1;       //we don't want to spend too much time on non existing properties
            try
            {
                // PROP_LIST was added as an addendum to 135-2010
                // Test to see if it is supported, otherwise fall back to the the predefined delault property list.
                bool objectDidSupplyPropertyList = ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_PROPERTY_LIST, ref values);

                //Used the supplied list of supported Properties, otherwise fall back to using the list of default properties.
                if (objectDidSupplyPropertyList)
                {
                    var proplist = values.Last();
                    foreach (var enumeratedValue in proplist.value)
                    {
                        BacnetPropertyIds bpi = (BacnetPropertyIds)(uint)enumeratedValue.Value;
                        // read all specified properties given by the xml file
                        ReadProperty(comm, adr, object_id, bpi, ref values);
                    }
                }
                else
                {
                    // Three mandatory common properties to all objects : PROP_OBJECT_IDENTIFIER,PROP_OBJECT_TYPE, PROP_OBJECT_NAME

                    // ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_OBJECT_IDENTIFIER, ref values)
                    // No need to query it, known value
                    BacnetPropertyValue new_entry = new BacnetPropertyValue();
                    new_entry.property = new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_OBJECT_IDENTIFIER, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL);
                    new_entry.value = new BacnetValue[] { new BacnetValue(object_id) };
                    values.Add(new_entry);

                    // ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_OBJECT_TYPE, ref values);
                    // No need to query it, known value
                    new_entry = new BacnetPropertyValue();
                    new_entry.property = new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_OBJECT_TYPE, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL);
                    new_entry.value = new BacnetValue[] { new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED, (uint)object_id.type) };
                    values.Add(new_entry);

                    // We do not know the value here
                    ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_OBJECT_NAME, ref values);

                    // for all other properties, the list is comming from the internal or external XML file

                    BacnetObjectDescription objDescr = new BacnetObjectDescription(); ;

                    int Idx = -1;
                    // try to find the Object description from the optional external xml file
                    if (objectsDescriptionExternal != null)
                        Idx = objectsDescriptionExternal.FindIndex(o => o.typeId == object_id.type);

                    if (Idx != -1)
                        objDescr = objectsDescriptionExternal[Idx];
                    else
                    {
                        // try to find from the embedded resoruce
                        Idx = objectsDescriptionDefault.FindIndex(o => o.typeId == object_id.type);
                        if (Idx != -1)
                            objDescr = objectsDescriptionDefault[Idx];
                    }

                    if (Idx != -1)
                        foreach (BacnetPropertyIds bpi in objDescr.propsId)
                            // read all specified properties given by the xml file
                            ReadProperty(comm, adr, object_id, bpi, ref values);
                }
            }
            catch { }

            comm.Retries = old_retries;
            value_list = new BacnetReadAccessResult[] { new BacnetReadAccessResult(object_id, values) };
            return true;
        }
        public static bool ReadProperty(BacnetClient comm, BacnetAddress adr, BacnetObjectId object_id, BacnetPropertyIds property_id, ref IList<BacnetPropertyValue> values, uint array_index = System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL)
        {
            BacnetPropertyValue new_entry = new BacnetPropertyValue();
            new_entry.property = new BacnetPropertyReference((uint)property_id, array_index);
            IList<BacnetValue> value;
            try
            {
                if (!comm.ReadPropertyRequest(adr, object_id, property_id, out value, 0, array_index))
                    return false;     //ignore
            }
            catch
            {
                return false;         //ignore
            }
            new_entry.value = value;
            values.Add(new_entry);
            return true;
        }
        
        
        public static BacnetClient CreateBacNetClient(CustomListData data)
        {
            if (!int.TryParse(data.Properties["Port"], out int tcpPort))
            {
                throw new Exception("Invalid port number");
            }

            ValidatePort(tcpPort);
            var transport = new BacnetIpUdpProtocolTransport(tcpPort);
            var client = new BacnetClient(transport);
            client.Start();
            return client;
        }
        
        public static void ValidateAddress(BacnetAddress address)
        {
            if (address == null)
                throw new Exception("Address cannot be null");
        }

        public static void ValidateObjectId(BacnetObjectId objectId)
        {
            if (objectId == null)
                throw new Exception("Object ID cannot be null");
        }

        public static void ValidatePropertyId(BacnetPropertyIds propertyId)
        {
            if (!Enum.IsDefined(typeof(BacnetPropertyIds), propertyId))
                throw new Exception($"Invalid property ID: {propertyId}");
        }

        public static void ValidateInstanceNumber(string instance)
        {
            if (string.IsNullOrWhiteSpace(instance))
                throw new Exception("Instance number cannot be empty");

            if (instance.Split(';').Length>1)
            {
                foreach (string instanceNumber in instance.Split(';'))
                {
                    if (!uint.TryParse(instanceNumber, out _))
                        throw new Exception($"Invalid instance number format: {instance}");
                }
            }
            else
            {
                foreach (string instanceNumber in instance.Split('-'))
                {
                    if (!uint.TryParse(instanceNumber, out _))
                        throw new Exception($"Invalid instance number format: {instance}");
                }
            }
        }

        public static void ValidatePort(int port)
        {
            if (port <= 0 || port > 65535)
                throw new Exception($"Invalid port number: {port}");
        }
    }
}
