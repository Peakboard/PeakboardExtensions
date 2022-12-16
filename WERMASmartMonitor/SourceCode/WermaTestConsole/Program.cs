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