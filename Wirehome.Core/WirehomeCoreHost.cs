using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Wirehome.Core.HTTP;
using Wirehome.Core.System;

namespace Wirehome.Core
{
    public class WirehomeCoreHost
    {
        private static readonly SystemCancellationToken SystemCancellationToken = new SystemCancellationToken();

        public static void Start(string[] arguments)
        {
            var host = WebHost.CreateDefaultBuilder()
                .ConfigureServices(s =>
                {
                    s.AddSingleton(new SystemLaunchArguments(arguments ?? new string[0]));
                    s.AddSingleton(SystemCancellationToken);
                })
                .UseSockets(o => o.NoDelay = true)
                .UseKestrel(o => o.ListenAnyIP(80))
                .UseStartup<Startup>()
                .Build();

            host.Start();
        }

        public static void Stop()
        {
            SystemCancellationToken?.Cancel();
        }
    }
}