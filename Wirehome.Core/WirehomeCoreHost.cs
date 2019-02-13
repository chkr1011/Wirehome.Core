using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Wirehome.Core.HTTP;
using Wirehome.Core.System;

namespace Wirehome.Core
{
    public class WirehomeCoreHost
    {
        private static readonly SystemCancellationToken _systemCancellationToken = new SystemCancellationToken();

        public static void Start(string[] arguments)
        {
            IWebHost host = WebHost.CreateDefaultBuilder()
                .ConfigureServices(s =>
                {
                    s.AddSingleton(new SystemLaunchArguments(arguments ?? new string[0]));
                    s.AddSingleton(_systemCancellationToken);
                })
                .UseKestrel(kestrelOptions =>
                {
                    kestrelOptions.ListenAnyIP(80,
                        listenOptions =>
                        {
                            listenOptions.NoDelay = true;
                        });
                })
                .UseStartup<WebStartup>()
                .Build();

            host.Start();
        }

        public static void Stop()
        {
            _systemCancellationToken?.Cancel();
        }
    }
}