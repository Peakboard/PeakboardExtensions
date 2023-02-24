using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace WermaTestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            // WERMA want to write some temp data here. Seams they have a bug which does not recognize the folder is missing
            if (!Directory.Exists(@"C:\ProgramData\WERMA"))
            {
                Directory.CreateDirectory(@"C:\ProgramData\WERMA");
                if (!Directory.Exists(@"C:\ProgramData\WERMA\WERMA-WIN-3.0"))
                {
                    Directory.CreateDirectory(@"C:\ProgramData\WERMA\WERMA-WIN-3.0");
                }
            }

            var host = "192.168.20.70\\WERMAWIN";
            var command = $@"{AppDomain.CurrentDomain.BaseDirectory}\WermaUtilities\WIN-CLI.exe /server {host.Split('\\').First()} /switchcontrol ""macid:009E40"" 1 {args[0]}";

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/K " + command;

            Process process = new Process();
            process.StartInfo = startInfo;

            process.Start();
        }
    }
}