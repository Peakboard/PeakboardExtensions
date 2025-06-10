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
        public static bool ReadAllPropertiesBySingle(BacnetClient comm, BacnetAddress adr, BacnetObjectId objectId, out IList<BacnetReadAccessResult> valueList, ref List<BacnetObjectDescription> objectsDescriptionExternal,ref List<BacnetObjectDescription> objectsDescriptionDefault)
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

            valueList = null;

            IList<BacnetPropertyValue> values = new List<BacnetPropertyValue>();

            int old_retries = comm.Retries;
            comm.Retries = 1;       //we don't want to spend too much time on non existing properties
            try
            {
                // PROP_LIST was added as an addendum to 135-2010
                // Test to see if it is supported, otherwise fall back to the the predefined delault property list.
                bool objectDidSupplyPropertyList = ReadProperty(comm, adr, objectId, BacnetPropertyIds.PROP_PROPERTY_LIST, ref values);

                //Used the supplied list of supported Properties, otherwise fall back to using the list of default properties.
                if (objectDidSupplyPropertyList)
                {
                    var proplist = values.Last();
                    foreach (var enumeratedValue in proplist.value)
                    {
                        BacnetPropertyIds bpi = (BacnetPropertyIds)(uint)enumeratedValue.Value;
                        // read all specified properties given by the xml file
                        ReadProperty(comm, adr, objectId, bpi, ref values);
                    }
                }
                else
                {
                    // Three mandatory common properties to all objects : PROP_OBJECT_IDENTIFIER,PROP_OBJECT_TYPE, PROP_OBJECT_NAME

                    // ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_OBJECT_IDENTIFIER, ref values)
                    // No need to query it, known value
                    BacnetPropertyValue new_entry = new BacnetPropertyValue();
                    new_entry.property = new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_OBJECT_IDENTIFIER, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL);
                    new_entry.value = new BacnetValue[] { new BacnetValue(objectId) };
                    values.Add(new_entry);

                    // ReadProperty(comm, adr, object_id, BacnetPropertyIds.PROP_OBJECT_TYPE, ref values);
                    // No need to query it, known value
                    new_entry = new BacnetPropertyValue();
                    new_entry.property = new BacnetPropertyReference((uint)BacnetPropertyIds.PROP_OBJECT_TYPE, System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL);
                    new_entry.value = new BacnetValue[] { new BacnetValue(BacnetApplicationTags.BACNET_APPLICATION_TAG_ENUMERATED, (uint)objectId.type) };
                    values.Add(new_entry);

                    // We do not know the value here
                    ReadProperty(comm, adr, objectId, BacnetPropertyIds.PROP_OBJECT_NAME, ref values);

                    // for all other properties, the list is comming from the internal or external XML file

                    BacnetObjectDescription objDescr = new BacnetObjectDescription(); ;

                    int Idx = -1;
                    // try to find the Object description from the optional external xml file
                    if (objectsDescriptionExternal != null)
                        Idx = objectsDescriptionExternal.FindIndex(o => o.typeId == objectId.type);

                    if (Idx != -1)
                        objDescr = objectsDescriptionExternal[Idx];
                    else
                    {
                        // try to find from the embedded resoruce
                        Idx = objectsDescriptionDefault.FindIndex(o => o.typeId == objectId.type);
                        if (Idx != -1)
                            objDescr = objectsDescriptionDefault[Idx];
                    }

                    if (Idx != -1)
                        foreach (BacnetPropertyIds bpi in objDescr.propsId)
                            // read all specified properties given by the xml file
                            ReadProperty(comm, adr, objectId, bpi, ref values);
                }
            }
            catch { }

            comm.Retries = old_retries;
            valueList = new BacnetReadAccessResult[] { new BacnetReadAccessResult(objectId, values) };
            return true;
        }
        public static bool ReadProperty(BacnetClient comm, BacnetAddress adr, BacnetObjectId objectId, BacnetPropertyIds propertyId, ref IList<BacnetPropertyValue> values, uint arrayIndex = System.IO.BACnet.Serialize.ASN1.BACNET_ARRAY_ALL)
        {
            BacnetPropertyValue new_entry = new BacnetPropertyValue();
            new_entry.property = new BacnetPropertyReference((uint)propertyId, arrayIndex);
            IList<BacnetValue> value;
            try
            {
                if (!comm.ReadPropertyRequest(adr, objectId, propertyId, out value, 0, arrayIndex))
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

        public static BacnetObjectTypes GetObjectTypeFromName(Dictionary<string, BacnetObjectTypes> objectMap,
            string objectName)
        {
            if (!objectMap.TryGetValue(objectName, out var type))
            {
                if (!uint.TryParse(objectName, out var objectNameAsUInt))
                {
                    throw new ArgumentException($"Error: Object '{objectName}' not found in BACnet map.");
                }

                type = (BacnetObjectTypes) objectNameAsUInt;
            }

            return type;
        }
        
        public static string GetStatusFlagsDescription(string bitString)
        {
            if (string.IsNullOrEmpty(bitString) || bitString.Length < 4)
                return "Unknown status";

            switch (bitString)
            {
                case "0000":
                    return "Normal";
                case "1000":
                    return "In alarm";
                case "0100":
                    return "Fault";
                case "0010":
                    return "Overridden";
                case "0001":
                    return "Out of service";
                default:
                    var parts = new List<string>();
                    if (bitString[0] == '1') parts.Add("In alarm");
                    if (bitString[1] == '1') parts.Add("Fault");
                    if (bitString[2] == '1') parts.Add("Overridden");
                    if (bitString[3] == '1') parts.Add("Out of service");
                    return parts.Any()
                        ? string.Join(", ", parts)
                        : "Unknown status";
            }
        }
        public static string GetUnitStringFromId(string unitId)
        {
            switch (unitId)
            {
                case "0": // UNITS_SQUARE_METERS
                    return "m²";
                case "1": // UNITS_SQUARE_FEET
                    return "ft²";
                case "2": // UNITS_MILLIAMPERES
                    return "mA";
                case "3": // UNITS_AMPERES
                    return "A";
                case "4": // UNITS_OHMS
                    return "Ω";
                case "5": // UNITS_VOLTS
                    return "V";
                case "6": // UNITS_KILOVOLTS
                    return "kV";
                case "7": // UNITS_MEGAVOLTS
                    return "MV";
                case "8": // UNITS_VOLT_AMPERES
                    return "VA";
                case "9": // UNITS_KILOVOLT_AMPERES
                    return "kVA";
                case "10": // UNITS_MEGAVOLT_AMPERES
                    return "MVA";
                case "11": // UNITS_VOLT_AMPERES_REACTIVE
                    return "var";
                case "12": // UNITS_KILOVOLT_AMPERES_REACTIVE
                    return "kvar";
                case "13": // UNITS_MEGAVOLT_AMPERES_REACTIVE
                    return "Mvar";
                case "14": // UNITS_DEGREES_PHASE
                    return "° (phase)";
                case "15": // UNITS_POWER_FACTOR
                    return "PF";
                case "16": // UNITS_JOULES
                    return "J";
                case "17": // UNITS_KILOJOULES
                    return "kJ";
                case "18": // UNITS_WATT_HOURS
                    return "Wh";
                case "19": // UNITS_KILOWATT_HOURS
                    return "kWh";
                case "20": // UNITS_BTUS
                    return "BTU";
                case "21": // UNITS_THERMS
                    return "therm";
                case "22": // UNITS_TON_HOURS
                    return "ton·h";
                case "23": // UNITS_JOULES_PER_KILOGRAM_DRY_AIR
                    return "J/(kg·dry air)";
                case "24": // UNITS_BTUS_PER_POUND_DRY_AIR
                    return "BTU/lb (dry air)";
                case "25": // UNITS_CYCLES_PER_HOUR
                    return "cph";
                case "26": // UNITS_CYCLES_PER_MINUTE
                    return "cpm";
                case "27": // UNITS_HERTZ
                    return "Hz";
                case "28": // UNITS_GRAMS_OF_WATER_PER_KILOGRAM_DRY_AIR
                    return "g/kg (dry air)";
                case "29": // UNITS_PERCENT_RELATIVE_HUMIDITY
                    return "% RH";
                case "30": // UNITS_MILLIMETERS
                    return "mm";
                case "31": // UNITS_METERS
                    return "m";
                case "32": // UNITS_INCHES
                    return "in";
                case "33": // UNITS_FEET
                    return "ft";
                case "34": // UNITS_WATTS_PER_SQUARE_FOOT
                    return "W/ft²";
                case "35": // UNITS_WATTS_PER_SQUARE_METER
                    return "W/m²";
                case "36": // UNITS_LUMENS
                    return "lm";
                case "37": // UNITS_LUXES
                    return "lx";
                case "38": // UNITS_FOOT_CANDLES
                    return "fc";
                case "39": // UNITS_KILOGRAMS
                    return "kg";
                case "40": // UNITS_POUNDS_MASS
                    return "lb";
                case "41": // UNITS_TONS
                    return "ton";
                case "42": // UNITS_KILOGRAMS_PER_SECOND
                    return "kg/s";
                case "43": // UNITS_KILOGRAMS_PER_MINUTE
                    return "kg/min";
                case "44": // UNITS_KILOGRAMS_PER_HOUR
                    return "kg/h";
                case "45": // UNITS_POUNDS_MASS_PER_MINUTE
                    return "lb/min";
                case "46": // UNITS_POUNDS_MASS_PER_HOUR
                    return "lb/h";
                case "47": // UNITS_WATTS
                    return "W";
                case "48": // UNITS_KILOWATTS
                    return "kW";
                case "49": // UNITS_MEGAWATTS
                    return "MW";
                case "50": // UNITS_BTUS_PER_HOUR
                    return "BTU/h";
                case "51": // UNITS_HORSEPOWER
                    return "hp";
                case "52": // UNITS_TONS_REFRIGERATION
                    return "ton ref";
                case "53": // UNITS_PASCALS
                    return "Pa";
                case "54": // UNITS_KILOPASCALS
                    return "kPa";
                case "55": // UNITS_BARS
                    return "bar";
                case "56": // UNITS_POUNDS_FORCE_PER_SQUARE_INCH
                    return "psi";
                case "57": // UNITS_CENTIMETERS_OF_WATER
                    return "cm H₂O";
                case "58": // UNITS_INCHES_OF_WATER
                    return "in H₂O";
                case "59": // UNITS_MILLIMETERS_OF_MERCURY
                    return "mm Hg";
                case "60": // UNITS_CENTIMETERS_OF_MERCURY
                    return "cm Hg";
                case "61": // UNITS_INCHES_OF_MERCURY
                    return "in Hg";
                case "62": // UNITS_DEGREES_CELSIUS
                    return "°C";
                case "63": // UNITS_DEGREES_KELVIN
                    return "K";
                case "64": // UNITS_DEGREES_FAHRENHEIT
                    return "°F";
                case "65": // UNITS_DEGREE_DAYS_CELSIUS
                    return "°C·day";
                case "66": // UNITS_DEGREE_DAYS_FAHRENHEIT
                    return "°F·day";
                case "67": // UNITS_YEARS
                    return "yr";
                case "68": // UNITS_MONTHS
                    return "mo";
                case "69": // UNITS_WEEKS
                    return "wk";
                case "70": // UNITS_DAYS
                    return "d";
                case "71": // UNITS_HOURS
                    return "h";
                case "72": // UNITS_MINUTES
                    return "min";
                case "73": // UNITS_SECONDS
                    return "s";
                case "74": // UNITS_METERS_PER_SECOND
                    return "m/s";
                case "75": // UNITS_KILOMETERS_PER_HOUR
                    return "km/h";
                case "76": // UNITS_FEET_PER_SECOND
                    return "ft/s";
                case "77": // UNITS_FEET_PER_MINUTE
                    return "ft/min";
                case "78": // UNITS_MILES_PER_HOUR
                    return "mph";
                case "79": // UNITS_CUBIC_FEET
                    return "ft³";
                case "80": // UNITS_CUBIC_METERS
                    return "m³";
                case "81": // UNITS_IMPERIAL_GALLONS
                    return "gal (imp)";
                case "82": // UNITS_LITERS
                    return "L";
                case "83": // UNITS_US_GALLONS
                    return "gal (US)";
                case "84": // UNITS_CUBIC_FEET_PER_MINUTE
                    return "ft³/min";
                case "85": // UNITS_CUBIC_METERS_PER_SECOND
                    return "m³/s";
                case "86": // UNITS_IMPERIAL_GALLONS_PER_MINUTE
                    return "gal (imp)/min";
                case "87": // UNITS_LITERS_PER_SECOND
                    return "L/s";
                case "88": // UNITS_LITERS_PER_MINUTE
                    return "L/min";
                case "89": // UNITS_US_GALLONS_PER_MINUTE
                    return "gal (US)/min";
                case "90": // UNITS_DEGREES_ANGULAR
                    return "° (angular)";
                case "91": // UNITS_DEGREES_CELSIUS_PER_HOUR
                    return "°C/h";
                case "92": // UNITS_DEGREES_CELSIUS_PER_MINUTE
                    return "°C/min";
                case "93": // UNITS_DEGREES_FAHRENHEIT_PER_HOUR
                    return "°F/h";
                case "94": // UNITS_DEGREES_FAHRENHEIT_PER_MINUTE
                    return "°F/min";
                case "95": // UNITS_NO_UNITS
                    return "";
                case "96": // UNITS_PARTS_PER_MILLION
                    return "ppm";
                case "97": // UNITS_PARTS_PER_BILLION
                    return "ppb";
                case "98": // UNITS_PERCENT
                    return "%";
                case "99": // UNITS_PERCENT_PER_SECOND
                    return "%/s";
                case "100": // UNITS_PER_MINUTE
                    return "/min";
                case "101": // UNITS_PER_SECOND
                    return "/s";
                case "102": // UNITS_PSI_PER_DEGREE_FAHRENHEIT
                    return "psi/°F";
                case "103": // UNITS_RADIANS
                    return "rad";
                case "104": // UNITS_REVOLUTIONS_PER_MINUTE
                    return "rpm";
                case "105": // UNITS_CURRENCY1
                    return "Currency1";
                case "106": // UNITS_CURRENCY2
                    return "Currency2";
                case "107": // UNITS_CURRENCY3
                    return "Currency3";
                case "108": // UNITS_CURRENCY4
                    return "Currency4";
                case "109": // UNITS_CURRENCY5
                    return "Currency5";
                case "110": // UNITS_CURRENCY6
                    return "Currency6";
                case "111": // UNITS_CURRENCY7
                    return "Currency7";
                case "112": // UNITS_CURRENCY8
                    return "Currency8";
                case "113": // UNITS_CURRENCY9
                    return "Currency9";
                case "114": // UNITS_CURRENCY10
                    return "Currency10";
                case "115": // UNITS_SQUARE_INCHES
                    return "in²";
                case "116": // UNITS_SQUARE_CENTIMETERS
                    return "cm²";
                case "117": // UNITS_BTUS_PER_POUND
                    return "BTU/lb";
                case "118": // UNITS_CENTIMETERS
                    return "cm";
                case "119": // UNITS_POUNDS_MASS_PER_SECOND
                    return "lb/s";
                case "120": // UNITS_DELTA_DEGREES_FAHRENHEIT
                    return "Δ°F";
                case "121": // UNITS_DELTA_DEGREES_KELVIN
                    return "ΔK";
                case "122": // UNITS_KILOHMS
                    return "kΩ";
                case "123": // UNITS_MEGOHMS
                    return "MΩ";
                case "124": // UNITS_MILLIVOLTS
                    return "mV";
                case "125": // UNITS_KILOJOULES_PER_KILOGRAM
                    return "kJ/kg";
                case "126": // UNITS_MEGAJOULES
                    return "MJ";
                case "127": // UNITS_JOULES_PER_DEGREE_KELVIN
                    return "J/°K";
                case "128": // UNITS_JOULES_PER_KILOGRAM_DEGREE_KELVIN
                    return "J/(kg·°K)";
                case "129": // UNITS_KILOHERTZ
                    return "kHz";
                case "130": // UNITS_MEGAHERTZ
                    return "MHz";
                case "131": // UNITS_PER_HOUR
                    return "/h";
                case "132": // UNITS_MILLIWATTS
                    return "mW";
                case "133": // UNITS_HECTOPASCALS
                    return "hPa";
                case "134": // UNITS_MILLIBARS
                    return "mbar";
                case "135": // UNITS_CUBIC_METERS_PER_HOUR
                    return "m³/h";
                case "136": // UNITS_LITERS_PER_HOUR
                    return "L/h";
                case "137": // UNITS_KW_HOURS_PER_SQUARE_METER
                    return "kWh/m²";
                case "138": // UNITS_KW_HOURS_PER_SQUARE_FOOT
                    return "kWh/ft²";
                case "139": // UNITS_MEGAJOULES_PER_SQUARE_METER
                    return "MJ/m²";
                case "140": // UNITS_MEGAJOULES_PER_SQUARE_FOOT
                    return "MJ/ft²";
                case "141": // UNITS_WATTS_PER_SQUARE_METER_DEGREE_KELVIN
                    return "W/(m²·°K)";
                case "142": // UNITS_CUBIC_FEET_PER_SECOND
                    return "ft³/s";
                case "143": // UNITS_PERCENT_OBSCURATION_PER_FOOT
                    return "% obs/ft";
                case "144": // UNITS_PERCENT_OBSCURATION_PER_METER
                    return "% obs/m";
                case "145": // UNITS_MILLIOHMS
                    return "mΩ";
                case "146": // UNITS_MEGAWATT_HOURS
                    return "MWh";
                case "147": // UNITS_KILO_BTUS
                    return "kBTU";
                case "148": // UNITS_MEGA_BTUS
                    return "MMBTU";
                case "149": // UNITS_KILOJOULES_PER_KILOGRAM_DRY_AIR
                    return "kJ/kg (dry air)";
                case "150": // UNITS_MEGAJOULES_PER_KILOGRAM_DRY_AIR
                    return "MJ/kg (dry air)";
                case "151": // UNITS_KILOJOULES_PER_DEGREE_KELVIN
                    return "kJ/°K";
                case "152": // UNITS_MEGAJOULES_PER_DEGREE_KELVIN
                    return "MJ/°K";
                case "153": // UNITS_NEWTON
                    return "N";
                case "154": // UNITS_GRAMS_PER_SECOND
                    return "g/s";
                case "155": // UNITS_GRAMS_PER_MINUTE
                    return "g/min";
                case "156": // UNITS_TONS_PER_HOUR
                    return "ton/h";
                case "157": // UNITS_KILO_BTUS_PER_HOUR
                    return "kBTU/h";
                case "158": // UNITS_HUNDREDTHS_SECONDS
                    return "0.01 s";
                case "159": // UNITS_MILLISECONDS
                    return "ms";
                case "160": // UNITS_NEWTON_METERS
                    return "N·m";
                case "161": // UNITS_MILLIMETERS_PER_SECOND
                    return "mm/s";
                case "162": // UNITS_MILLIMETERS_PER_MINUTE
                    return "mm/min";
                case "163": // UNITS_METERS_PER_MINUTE
                    return "m/min";
                case "164": // UNITS_METERS_PER_HOUR
                    return "m/h";
                case "165": // UNITS_CUBIC_METERS_PER_MINUTE
                    return "m³/min";
                case "166": // UNITS_METERS_PER_SECOND_PER_SECOND
                    return "m/s²";
                case "167": // UNITS_AMPERES_PER_METER
                    return "A/m";
                case "168": // UNITS_AMPERES_PER_SQUARE_METER
                    return "A/m²";
                case "169": // UNITS_AMPERE_SQUARE_METERS
                    return "A·m²";
                case "170": // UNITS_FARADS
                    return "F";
                case "171": // UNITS_HENRYS
                    return "H";
                case "172": // UNITS_OHM_METERS
                    return "Ω·m";
                case "173": // UNITS_SIEMENS
                    return "S";
                case "174": // UNITS_SIEMENS_PER_METER
                    return "S/m";
                case "175": // UNITS_TESLAS
                    return "T";
                case "176": // UNITS_VOLTS_PER_DEGREE_KELVIN
                    return "V/°K";
                case "177": // UNITS_VOLTS_PER_METER
                    return "V/m";
                case "178": // UNITS_WEBERS
                    return "Wb";
                case "179": // UNITS_CANDELAS
                    return "cd";
                case "180": // UNITS_CANDELAS_PER_SQUARE_METER
                    return "cd/m²";
                case "181": // UNITS_DEGREES_KELVIN_PER_HOUR
                    return "K/h";
                case "182": // UNITS_DEGREES_KELVIN_PER_MINUTE
                    return "K/min";
                case "183": // UNITS_JOULE_SECONDS
                    return "J·s";
                case "184": // UNITS_RADIANS_PER_SECOND
                    return "rad/s";
                case "185": // UNITS_SQUARE_METERS_PER_NEWTON
                    return "m²/N";
                case "186": // UNITS_KILOGRAMS_PER_CUBIC_METER
                    return "kg/m³";
                case "187": // UNITS_NEWTON_SECONDS
                    return "N·s";
                case "188": // UNITS_NEWTONS_PER_METER
                    return "N/m";
                case "189": // UNITS_WATTS_PER_METER_PER_DEGREE_KELVIN
                    return "W/(m·°K)";
                case "190": // UNITS_MICROSIEMENS
                    return "µS";
                case "191": // UNITS_CUBIC_FEET_PER_HOUR
                    return "ft³/h";
                case "192": // UNITS_US_GALLONS_PER_HOUR
                    return "gal (US)/h";
                case "193": // UNITS_KILOMETERS
                    return "km";
                case "194": // UNITS_MICROMETERS
                    return "µm";
                case "195": // UNITS_GRAMS
                    return "g";
                case "196": // UNITS_MILLIGRAMS
                    return "mg";
                case "197": // UNITS_MILLILITERS
                    return "mL";
                case "198": // UNITS_MILLILITERS_PER_SECOND
                    return "mL/s";
                case "199": // UNITS_DECIBELS
                    return "dB";
                case "200": // UNITS_DECIBELS_MILLIVOLT
                    return "dBmV";
                case "201": // UNITS_DECIBELS_VOLT
                    return "dBV";
                case "202": // UNITS_MILLISIEMENS
                    return "mS";
                case "203": // UNITS_WATT_HOURS_REACTIVE
                    return "var·h";
                case "204": // UNITS_KILOWATT_HOURS_REACTIVE
                    return "kvar·h";
                case "205": // UNITS_MEGAWATT_HOURS_REACTIVE
                    return "Mvar·h";
                case "206": // UNITS_MILLIMETERS_OF_WATER
                    return "mm H₂O";
                case "207": // UNITS_PER_MILLE
                    return "‰";
                case "208": // UNITS_GRAMS_PER_GRAM
                    return "g/g";
                case "209": // UNITS_KILOGRAMS_PER_KILOGRAM
                    return "kg/kg";
                case "210": // UNITS_GRAMS_PER_LITER
                    return "g/L";
                case "211": // UNITS_MILLIGRAMS_PER_GRAM
                    return "mg/g";
                case "212": // UNITS_MILLIGRAMS_PER_KILOGRAM
                    return "mg/kg";
                case "213": // UNITS_GRAMS_PER_MILLILITER
                    return "g/mL";
                case "214": // UNITS_GRAMS_PER_LITER
                    return "g/L";
                case "215": // UNITS_MILLIGRAMS_PER_LITER
                    return "mg/L";
                case "216": // UNITS_MICROGRAMS_PER_LITER
                    return "µg/L";
                case "217": // UNITS_GRAMS_PER_CUBIC_METER
                    return "g/m³";
                case "218": // UNITS_MILLIGRAMS_PER_CUBIC_METER
                    return "mg/m³";
                case "219": // UNITS_MICROGRAMS_PER_CUBIC_METER
                    return "µg/m³";
                case "220": // UNITS_NANOGRAMS_PER_CUBIC_METER
                    return "ng/m³";
                case "221": // UNITS_GRAMS_PER_CUBIC_CENTIMETER
                    return "g/cm³";
                case "222": // UNITS_BECQUERELS
                    return "Bq";
                case "223": // UNITS_KILOBECQUERELS
                    return "kBq";
                case "224": // UNITS_MEGABECQUERELS
                    return "MBq";
                case "225": // UNITS_GRAY
                    return "Gy";
                case "226": // UNITS_MILLIGRAY
                    return "mGy";
                case "227": // UNITS_MICROGRAY
                    return "µGy";
                case "228": // UNITS_SIEVERTS
                    return "Sv";
                case "229": // UNITS_MILLISIEVERTS
                    return "mSv";
                case "230": // UNITS_MICROSIEVERTS
                    return "µSv";
                case "231": // UNITS_MICROSIEVERTS_PER_HOUR
                    return "µSv/h";
                case "232": // UNITS_DECIBELS_A
                    return "dB(A)";
                case "233": // UNITS_NEPHELOMETRIC_TURBIDITY_UNIT
                    return "NTU";
                case "234": // UNITS_PH
                    return "pH";
                case "235": // UNITS_GRAMS_PER_SQUARE_METER
                    return "g/m²";
                case "236": // UNITS_MINUTES_PER_DEGREE_KELVIN
                    return "min/°K";
                case "237": // UNITS_METER_SQUARED_PER_METER
                    return "m²/m";
                case "238": // UNITS_AMPERE_SECONDS
                    return "A·s";
                case "239": // UNITS_VOLT_AMPERE_HOURS
                    return "VA·h";
                case "240": // UNITS_KILOVOLT_AMPERE_HOURS
                    return "kVA·h";
                case "241": // UNITS_MEGAVOLT_AMPERE_HOURS
                    return "MVA·h";
                case "242": // UNITS_VOLT_AMPERE_HOURS_REACTIVE
                    return "var·h";
                case "243": // UNITS_KILOVOLT_AMPERE_HOURS_REACTIVE
                    return "kvar·h";
                case "244": // UNITS_MEGAVOLT_AMPERE_HOURS_REACTIVE
                    return "Mvar·h";
                case "245": // UNITS_VOLT_SQUARE_HOURS
                    return "V²·h";
                case "246": // UNITS_AMPERE_SQUARE_HOURS
                    return "A²·h";
                case "247": // UNITS_JOULE_PER_HOURS
                    return "J/h";
                case "248": // UNITS_CUBIC_FEET_PER_DAY
                    return "ft³/d";
                case "249": // UNITS_CUBIC_METERS_PER_DAY
                    return "m³/d";
                case "250": // UNITS_WATT_HOURS_PER_CUBIC_METER
                    return "Wh/m³";
                case "251": // UNITS_JOULES_PER_CUBIC_METER
                    return "J/m³";
                case "252": // UNITS_MOLE_PERCENT
                    return "mol %";
                case "253": // UNITS_PASCAL_SECONDS
                    return "Pa·s";
                // UNITS_MILLION_CUBIC_FEET_PER_MINUTE
                case "254": // UNITS_MILLION_STANDARD_CUBIC_FEET_PER_MINUTE (same numeric value)
                    return "MMcfm";
                case "47808": // UNITS_STANDARD_CUBIC_FEET_PER_DAY
                    return "SCFD";
                case "47809": // UNITS_MILLION_STANDARD_CUBIC_FEET_PER_DAY
                    return "MMSCFD";
                case "47810": // UNITS_THOUSAND_CUBIC_FEET_PER_DAY
                    return "MCFD";
                case "47811": // UNITS_THOUSAND_STANDARD_CUBIC_FEET_PER_DAY
                    return "MSCFD";
                case "47812": // UINITS_POUNDS_MASS_PER_DAY
                    return "lb/d";
                default:
                    return string.Empty;
            }
            
        }
        public static string GetEventStateDescription(string stateNumber)
        {
            switch (stateNumber)
            {
                case "0": // EVENT_STATE_NORMAL
                    return "Normal";
                case "1": // EVENT_STATE_FAULT
                    return "Fault";
                case "2": // EVENT_STATE_OFFNORMAL
                    return "Offnormal";
                case "3": // EVENT_STATE_HIGH_LIMIT
                    return "High Limit";
                case "4": // EVENT_STATE_LOW_LIMIT
                    return "Low Limit";
                case "5": // EVENT_STATE_LIFE_SAFETY_ALARM
                    return "Life Safety Alarm";
                default:
                    return "Unknown Event State";
            }
        }
        public static string GetReliabilityDescription(string reliabilityIdString)
        {
            if (string.IsNullOrEmpty(reliabilityIdString))
                return "Unknown reliability";

            switch (reliabilityIdString)
            {
                case "0":   // RELIABILITY_NO_FAULT_DETECTED
                    return "No fault detected";
                case "1":   // RELIABILITY_NO_SENSOR
                    return "No sensor";
                case "2":   // RELIABILITY_OVER_RANGE
                    return "Over range";
                case "3":   // RELIABILITY_UNDER_RANGE
                    return "Under range";
                case "4":   // RELIABILITY_OPEN_LOOP
                    return "Open loop";
                case "5":   // RELIABILITY_SHORTED_LOOP
                    return "Shorted loop";
                case "6":   // RELIABILITY_NO_OUTPUT
                    return "No output";
                case "7":   // RELIABILITY_UNRELIABLE_OTHER
                    return "Unreliable (other)";
                case "8":   // RELIABILITY_PROCESS_ERROR
                    return "Process error";
                case "9":   // RELIABILITY_MULTI_STATE_FAULT
                    return "Multi-state fault";
                case "10":  // RELIABILITY_CONFIGURATION_ERROR
                    return "Configuration error";
                case "11":  // RELIABILITY_MEMBER_FAULT
                    return "Member fault";
                case "12":  // RELIABILITY_COMMUNICATION_FAILURE
                    return "Communication failure";
                case "13":  // RELIABILITY_TRIPPED
                    return "Tripped";
                default:
                    return "Unknown reliability";
            }
        }
    }
}
