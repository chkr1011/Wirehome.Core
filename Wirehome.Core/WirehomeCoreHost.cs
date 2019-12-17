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
            var loopbackServer = new LoopbackServer();

            var host = WebHost.CreateDefaultBuilder()
                .ConfigureServices(s =>
                {
                    s.AddSingleton(new SystemLaunchArguments(arguments ?? new string[0]));
                    s.AddSingleton(SystemCancellationToken);
                    s.AddSingleton(loopbackServer);
                })
                .UseSockets(o => o.NoDelay = true)
                .UseKestrel(o => o.ListenAnyIP(80))
                .UseServer(loopbackServer)
                .UseStartup<Startup>()
                .Build();

            host.Start();

            //var x = new TestServer();
            //x.SendAsync(c =>
            //{
            //    c.Request.Method = "Get";
            //});
        }

        public static void Stop()
        {
            SystemCancellationToken?.Cancel();
        }
    }
}