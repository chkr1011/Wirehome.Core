using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wirehome.Core.HTTP;
using Wirehome.Core.System;

namespace Wirehome.Core;

public static class WirehomeCoreHost
{
    static readonly SystemCancellationToken SystemCancellationToken = new();

    public static async Task Run(string[] arguments)
    {
        using (var host = Host.CreateDefaultBuilder(arguments).ConfigureWebHostDefaults(webBuilder =>
               {
                   webBuilder.UseStartup<Startup>();
                   webBuilder.UseKestrel(o => o.ListenAnyIP(80));
                   webBuilder.ConfigureServices(s =>
                   {
                       s.AddSingleton(new SystemLaunchArguments(arguments));
                       s.AddSingleton(SystemCancellationToken);
                   });
               }).Build())
        {
            await host.RunAsync(SystemCancellationToken.Token).ConfigureAwait(false);

            SystemCancellationToken?.Cancel();
        }
    }
}