using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Automations;
using Wirehome.Core.Cloud;
using Wirehome.Core.Components;
using Wirehome.Core.Contracts;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.Discovery;
using Wirehome.Core.Extensions;
using Wirehome.Core.FunctionPool;
using Wirehome.Core.GlobalVariables;
using Wirehome.Core.Hardware.GPIO;
using Wirehome.Core.Hardware.I2C;
using Wirehome.Core.Hardware.MQTT;
using Wirehome.Core.History;
using Wirehome.Core.HTTP;
using Wirehome.Core.Macros;
using Wirehome.Core.MessageBus;
using Wirehome.Core.Notifications;
using Wirehome.Core.Python;
using Wirehome.Core.Resources;
using Wirehome.Core.Scheduler;
using Wirehome.Core.ServiceHost;
using Wirehome.Core.Storage;
using Wirehome.Core.System;
using Wirehome.Core.System.StartupScripts;

namespace Wirehome.Core
{
    public class WirehomeController
    {
        private readonly SystemCancellationToken _systemCancellationToken = new SystemCancellationToken();
        private readonly SystemLaunchArguments _systemLaunchArguments;
        private readonly ILoggerFactory _loggerFactory;
        private ILogger _logger;

        public WirehomeController(ILoggerFactory loggerFactory, string[] arguments)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _systemLaunchArguments = new SystemLaunchArguments(arguments ?? new string[0]);
        }

        public void Start()
        {
            try
            {
                _logger = _loggerFactory.CreateLogger<WirehomeController>();
                _logger.LogInformation("Starting Wirehome.Core (c) Christian Kratky 2011 - 2018");

                var serviceProvider = StartHttpServer();

                _logger.LogDebug("Starting services...");

                serviceProvider.GetRequiredService<StorageService>().Start();
                serviceProvider.GetRequiredService<DiagnosticsService>().Start();
                serviceProvider.GetRequiredService<MessageBusService>().Start();

                serviceProvider.GetRequiredService<ResourceService>().Start();
                serviceProvider.GetRequiredService<GlobalVariablesService>().Start();
                serviceProvider.GetRequiredService<CloudService>().Start();

                serviceProvider.GetRequiredService<SchedulerService>().Start();

                // Start hardware related services.
                serviceProvider.GetRequiredService<GpioRegistryService>().Start();
                serviceProvider.GetRequiredService<I2CBusService>().Start();
                serviceProvider.GetRequiredService<MqttService>().Start();
                serviceProvider.GetRequiredService<HttpServerService>().Start();
                serviceProvider.GetRequiredService<DiscoveryService>().Start();

                serviceProvider.GetRequiredService<PythonEngineService>().Start();

                var startupScriptsService = serviceProvider.GetRequiredService<StartupScriptsService>();
                startupScriptsService.Start();

                serviceProvider.GetRequiredService<FunctionPoolService>().Start();
                serviceProvider.GetRequiredService<ServiceHostService>().Start();

                serviceProvider.GetRequiredService<NotificationsService>().Start();

                serviceProvider.GetRequiredService<HistoryService>().Start();

                startupScriptsService.OnServicesInitialized();

                // Start data related services.
                serviceProvider.GetRequiredService<ComponentGroupRegistryService>().Start();
                serviceProvider.GetRequiredService<ComponentRegistryService>().Start();
                serviceProvider.GetRequiredService<AutomationRegistryService>().Start();
                serviceProvider.GetRequiredService<MacroRegistryService>().Start();

                _logger.LogDebug("Service startup completed.");

                startupScriptsService.OnConfigurationLoaded();

                var systemService = serviceProvider.GetRequiredService<SystemService>();
                systemService.Start();

                startupScriptsService.OnStartupCompleted();
            }
            catch (Exception exception)
            {
                _logger.LogCritical(exception, "Startup failed.");
                throw;
            }
        }

        public void Stop()
        {
            _systemCancellationToken?.Cancel();
        }

        private IServiceProvider StartHttpServer()
        {
            _logger.LogDebug("Starting HTTP server");

            // TODO: Consider writing custom WebHostBuilder to fix this hack.
            WebStartup.OnServiceRegistration = RegisterServices;

            var host = WebHost.CreateDefaultBuilder()
                .UseKestrel(kestrelOptions =>
                {
                    kestrelOptions.ListenAnyIP(80, 
                        listenOptions =>
                        {
                            listenOptions.NoDelay = true;
                        });
                })
                .UseStartup<WebStartup>()
                .UseUrls("http://*:80")
                .Build();

            host.Start();

            _logger.LogDebug("HTTP server started.");

            return WebStartup.ServiceProvider;
        }

        private void RegisterServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton(_loggerFactory);
            serviceCollection.AddSingleton(_systemLaunchArguments);
            serviceCollection.AddSingleton(_systemCancellationToken);

            foreach (var singletonService in Reflection.GetClassesImplementingInterface<IService>())
            {
                serviceCollection.AddSingleton(singletonService);
            }

            foreach (var pythonProxy in Reflection.GetClassesImplementingInterface<IInjectedPythonProxy>())
            {
                serviceCollection.AddSingleton(typeof(IPythonProxy), pythonProxy);
            }
        }
    }
}