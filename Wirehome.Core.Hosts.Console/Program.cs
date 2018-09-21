using System;
using Microsoft.Extensions.Logging;

namespace Wirehome.Core.Hosts.Console
{
    public static class Program
    {
        private static WirehomeController _controller;

        public static void Main(string[] arguments)
        {
            try
            {
                var loggerFactory = new LoggerFactory();
                loggerFactory.AddConsole(LogLevel.Debug);
               
                _controller = new WirehomeController(loggerFactory, arguments);
                _controller.Startup();
            }
            catch (Exception exception)
            {
                global::System.Console.WriteLine(exception.ToString());
            }
            
            global::System.Console.WriteLine("Press <Enter> to exit.");
            global::System.Console.ReadLine();
        }
    }
}
