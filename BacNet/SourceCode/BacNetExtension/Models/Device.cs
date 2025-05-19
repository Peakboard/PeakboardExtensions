using System.IO.BACnet;

namespace BacNetExtension.Models
{
    public class Device
    {
        public BacnetAddress Address { get; set; }
        public uint DeviceId { get; set; }
        public uint MaxAdpu { get; set; }
        public BacnetSegmentations Segmentation { get; set; }
        public ushort VendorId { get; set; }
    }
}
