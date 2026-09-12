using System;

namespace PingMonitor
{
    static class Program
    {
        [STAThread]
        static int Main(string[] args)
        {
            return RuntimeBootstrap.Run(args);
        }
    }
}
