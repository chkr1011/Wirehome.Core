using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wirehome.Core.HTTP;
using Wirehome.Core.System;

namespace Wirehome.Core
{
    public static class WirehomeCoreHost
    {
        static readonly SystemCancellationToken SystemCancellationToken = new SystemCancellationToken();

        static IHost _host;

        public static Task Start(string[] arguments)
        {
            _host = Host.CreateDefaultBuilder(arguments)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseKestrel(o => o.ListenAnyIP(80));
                    webBuilder.ConfigureServices(s =>
                    {
                        s.AddSingleton(new SystemLaunchArguments(arguments));
                        s.AddSingleton(SystemCancellationToken);
                    });
                }).Build();

            return _host.StartAsync();
        }

        public static async Task Stop()
        {
            SystemCancellationToken?.Cancel();

            await _host.StopAsync(TimeSpan.FromSeconds(10)).ConfigureAwait(false);
            _host.Dispose();
        }
    }
}