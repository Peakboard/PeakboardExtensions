namespace AVMFritz
{
    public class FritzThermostat
    {
        public string Id { get; set; }
        public bool Present { get; set; }
        public string Name { get; set; }
        public int Battery { get; set; }
        public double TempCurrent { get; set; }
        public double TempTarget { get; set; }
    }
}