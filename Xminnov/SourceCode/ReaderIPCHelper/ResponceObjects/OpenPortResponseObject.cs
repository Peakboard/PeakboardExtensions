
namespace ReaderIPCHelper.ResponceObjects
{
    public class OpenPortResponseObject
    {
        public int FCmdRet { get; set; }
        public int TcpPort { get; set; }
        public string IpAddress { get; set; }
        public byte ComAddr { get; set; }
        public int FrmHandle { get; set; }
    }
}
