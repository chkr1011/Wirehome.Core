using System;

namespace Wirehome.Core.Hosts.Console
{
    public static class Program
    {
        public static void Main(string[] arguments)
        {
            var logo = $@"
      __        ___          _                            ____               
      \ \      / (_)_ __ ___| |__   ___  _ __ ___   ___  / ___|___  _ __ ___ 
       \ \ /\ / /| | '__/ _ \ '_ \ / _ \| '_ ` _ \ / _ \| |   / _ \| '__/ _ \
        \ V  V / | | | |  __/ | | | (_) | | | | | |  __/| |__| (_) | | |  __/
         \_/\_/  |_|_|  \___|_| |_|\___/|_| |_| |_|\___(_)____\___/|_|  \___|

      {WirehomeCoreVersion.Version}

      (c) Christian Kratky 2011 - 2020
      https://github.com/chkr1011/Wirehome.Core
                                                                        
";
            try
            {
                global::System.Console.WriteLine(logo);

                WirehomeCoreHost.Start(arguments);

                global::System.Console.WriteLine("Press <Enter> to exit.");
                global::System.Console.ReadLine();

                WirehomeCoreHost.Stop();
            }
            catch (Exception exception)
            {
                global::System.Console.WriteLine(exception);
            }
        }
    }
}
