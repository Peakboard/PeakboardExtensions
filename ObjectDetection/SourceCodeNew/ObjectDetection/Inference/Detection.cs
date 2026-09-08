namespace PeakboardExtensionObjectDetection.Inference
{
    public class Detection
    {
        public string ClassName { get; set; }
        public int ClassId { get; set; }
        public float Confidence { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }

        public override string ToString()
        {
            return $"{ClassName} ({Confidence:P0}) [{X:F0},{Y:F0},{Width:F0},{Height:F0}]";
        }
    }
}
