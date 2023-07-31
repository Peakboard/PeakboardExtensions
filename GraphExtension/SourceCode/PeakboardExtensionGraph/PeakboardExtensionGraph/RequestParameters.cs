namespace PeakboardExtensionGraph
{
    public class RequestParameters
    {
        public string Select { get; set; }
        public int Top = 0;
        public string OrderBy { get; set; }
        public int Skip = 0;
        public string Filter { get; set; }
        public bool ConsistencyLevelEventual { get; set; } // header needed for some filter options

        public override string ToString() =>
            $"select={Select} orderBy={OrderBy} filter={Filter} top={Top} skip={Skip} consistency={ConsistencyLevelEventual}";
    }
}