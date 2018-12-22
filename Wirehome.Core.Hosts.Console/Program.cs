using System;

namespace Wirehome.Core.Hosts.Console
{
    public static class Program
    {
        private static WirehomeController _controller;

        public static void Main(string[] arguments)
        {
            var logo = $@"
      __        ___          _                            ____               
      \ \      / (_)_ __ ___| |__   ___  _ __ ___   ___  / ___|___  _ __ ___ 
       \ \ /\ / /| | '__/ _ \ '_ \ / _ \| '_ ` _ \ / _ \| |   / _ \| '__/ _ \
        \ V  V / | | | |  __/ | | | (_) | | | | | |  __/| |__| (_) | | |  __/
         \_/\_/  |_|_|  \___|_| |_|\___/|_| |_| |_|\___(_)____\___/|_|  \___|

      {WirehomeCoreVersion.Version}

      (c) Christian Kratky 2011 - 2018
      https://github.com/chkr1011/Wirehome.Core
                                                                        
";

            try
            {
                global::System.Console.WriteLine(logo);
                
                _controller = new WirehomeController(arguments);
                _controller.Start();

                global::System.Console.WriteLine("Press <Enter> to exit.");
                global::System.Console.ReadLine();

                _controller?.Stop();
            }
            catch (Exception exception)
            {
                global::System.Console.WriteLine("ERROR: " + exception);
            }
        }
    }
}
