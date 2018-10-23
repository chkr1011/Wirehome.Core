using System;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Automations;
using Wirehome.Core.Components;
using Wirehome.Core.Constants;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.Diagnostics.Log;
using Wirehome.Core.FunctionPool;
using Wirehome.Core.GlobalVariables;
using Wirehome.Core.Hardware.GPIO;
using Wirehome.Core.Hardware.GPIO.Adapters;
using Wirehome.Core.Hardware.I2C;
using Wirehome.Core.Hardware.I2C.Adapters;
using Wirehome.Core.Hardware.MQTT;
using Wirehome.Core.History;
using Wirehome.Core.HTTP;
using Wirehome.Core.Macros;
using Wirehome.Core.MessageBus;
using Wirehome.Core.Model;
using Wirehome.Core.Notifications;
using Wirehome.Core.Python;
using Wirehome.Core.Repository;
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
        private readonly ILoggerFactory _loggerFactory;
        private readonly string[] _arguments;
        private ILogger _logger;

        public WirehomeController(ILoggerFactory loggerFactory, string[] arguments)
        {
            _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            _arguments = arguments;
        }

        public void Startup()
        {
            try
            {
                var timestamp = DateTime.Now;

                _logger = _loggerFactory.CreateLogger<WirehomeController>();
                _logger.Log(LogLevel.Information, "Starting Wirehome.Core (c) Christian Kratky 2011 - 2018");

                var storageService = new StorageService(_loggerFactory);
                storageService.Start();
                
                var serviceProvider = StartHttpServer(storageService);
                _loggerFactory.AddProvider(new LogServiceLoggerProvider(serviceProvider.GetService<LogService>()));

                SetupHardwareAdapters(serviceProvider); // TODO: From config!

                StartServices(serviceProvider);

                RegisterDefaultResources(serviceProvider);
                RegisterDefaultGlobalVariables(serviceProvider);
                RegisterEvents(serviceProvider);

                PublishBootedNotification(serviceProvider);

                _logger.Log(LogLevel.Information, "Startup completed.");

                serviceProvider.GetService<SystemService>().Start(timestamp, string.Join(" ", _arguments));
                serviceProvider.GetRequiredService<StartupScriptsService>().OnStartupCompleted();
            }
            catch (Exception exception)
            {
                _logger.Log(LogLevel.Critical, exception, "Startup failed.");
            }
        }

        private static void RegisterEvents(IServiceProvider serviceProvider)
        {
            var systemService = serviceProvider.GetService<SystemService>();
            var notificationsService = serviceProvider.GetService<NotificationsService>();

            systemService.RebootInitiated += (s, e) =>
            {
                notificationsService.PublishFromResource(new PublishFromResourceParameters
                {
                    Type = NotificationType.Warning,
                    ResourceUid = NotificationResourceUids.RebootInitiated,
                    Parameters = new WirehomeDictionary
                    {
                        ["wait_time"] = 0 // TODO: Add to event args.
                    }
                });
            };
        }

        private static void PublishBootedNotification(IServiceProvider serviceProvider)
        {
            var messageBusService = serviceProvider.GetService<MessageBusService>();
            messageBusService.Publish(new WirehomeDictionary().WithType(MessageBusMessageTypes.Booted));
    
            var notificationService = serviceProvider.GetService<NotificationsService>();
            notificationService.PublishFromResource(new PublishFromResourceParameters()
            {
                Type = NotificationType.Information,
                ResourceUid = NotificationResourceUids.Booted
            });
        }

        private void RegisterDefaultResources(IServiceProvider serviceProvider)
        {
            var resourcesService = serviceProvider.GetService<ResourcesService>();

            // Notifications
            resourcesService.RegisterString(NotificationResourceUids.Booted, "en", "System has booted.");
            resourcesService.RegisterString(NotificationResourceUids.Booted, "de", "Das System wurde gestartet.");

            resourcesService.RegisterString(NotificationResourceUids.RebootInitiated, "en", "Reboot initiated.");
            resourcesService.RegisterString(NotificationResourceUids.RebootInitiated, "de", "Neustart eingeleitet.");

            _logger.Log(LogLevel.Debug, "Registered default resources.");
        }

        private void RegisterDefaultGlobalVariables(IServiceProvider serviceProvider)
        {
            var globalVariablesService = serviceProvider.GetService<GlobalVariablesService>();

            globalVariablesService.RegisterValue(GlobalVariableUids.LanguageCode, "en");

            _logger.Log(LogLevel.Debug, "Registered default global variables.");
        }

        private void RegisterServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton(_loggerFactory);

            serviceCollection.AddSingleton<HttpServerService>();

            serviceCollection.AddSingleton<LogService>();
            serviceCollection.AddSingleton<SystemService>();
            serviceCollection.AddSingleton<DiagnosticsService>();
            serviceCollection.AddSingleton<StartupScriptsService>();
            serviceCollection.AddSingleton<SystemStatusService>();
            serviceCollection.AddSingleton<GlobalVariablesService>();

            serviceCollection.AddSingleton<ResourcesService>();
            serviceCollection.AddSingleton<FunctionPoolService>();

            serviceCollection.AddSingleton<MessageBusService>();
            serviceCollection.AddSingleton<SchedulerService>();
            serviceCollection.AddSingleton<PythonEngineService>();

            serviceCollection.AddSingleton<MqttService>();
            serviceCollection.AddSingleton<I2CBusService>();
            serviceCollection.AddSingleton<GpioRegistryService>();

            serviceCollection.AddSingleton<NotificationsService>();

            serviceCollection.AddSingleton<ComponentGroupRegistryService>();

            serviceCollection.AddSingleton<RepositoryService>();

            serviceCollection.AddSingleton<HistoryService>();

            serviceCollection.AddSingleton<ServiceHostService>();
            serviceCollection.AddSingleton<ComponentRegistryService>();
            serviceCollection.AddSingleton<ComponentInitializerFactory>();
            serviceCollection.AddSingleton<AutomationsRegistryService>();
            serviceCollection.AddSingleton<MacroRegistryService>();
        }

        private void StartServices(IServiceProvider serviceProvider)
        {
            _logger.Log(LogLevel.Debug, "Starting services...");

            serviceProvider.GetService<DiagnosticsService>().Start();
            serviceProvider.GetService<MessageBusService>().Start();

            serviceProvider.GetService<ResourcesService>().Start();
            serviceProvider.GetService<GlobalVariablesService>().Start();

            serviceProvider.GetService<SchedulerService>().Start();

            serviceProvider.GetService<MqttService>().Start();
            serviceProvider.GetService<HttpServerService>().Start();

            serviceProvider.GetService<PythonEngineService>().Start();

            var startupScriptsService = serviceProvider.GetService<StartupScriptsService>();
            startupScriptsService.Start();

            serviceProvider.GetService<FunctionPoolService>().Start();
            serviceProvider.GetService<ServiceHostService>().Start();
            
            serviceProvider.GetService<NotificationsService>().Start();

            serviceProvider.GetService<HistoryService>().Start();

            startupScriptsService.OnServicesInitialized();

            // Start data related services.
            serviceProvider.GetService<ComponentGroupRegistryService>().Start();
            serviceProvider.GetService<ComponentRegistryService>().Start();
            serviceProvider.GetService<AutomationsRegistryService>().Start();
            serviceProvider.GetService<MacroRegistryService>().Start();

            startupScriptsService.OnConfigurationLoaded();

            _logger.Log(LogLevel.Debug, "Service startup completed.");
        }

        private static void SetupHardwareAdapters(IServiceProvider serviceProvider)
        {
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();

            var i2CService = serviceProvider.GetService<I2CBusService>();
            var gpioService = serviceProvider.GetService<GpioRegistryService>();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                var i2CAdapter = new LinuxI2CBusAdapter(1, loggerFactory);
                i2CAdapter.Enable();
                i2CService.RegisterAdapter(string.Empty, i2CAdapter);

                var gpioAdapter = new LinuxGpioAdapter(loggerFactory);
                gpioAdapter.Enable();
                gpioService.RegisterAdapter(string.Empty, gpioAdapter);
            }
            else
            {
                var i2CAdapter = new TestI2CBusAdapter(loggerFactory);
                i2CService.RegisterAdapter(string.Empty, i2CAdapter);

                var gpioAdapter = new TestGpioAdapter(loggerFactory);
                gpioService.RegisterAdapter(string.Empty, gpioAdapter);
            }
        }

        private IServiceProvider StartHttpServer(StorageService storageService)
        {
            _logger.Log(LogLevel.Debug, "Starting HTTP server");

            WebStartup.OnServiceRegistration = RegisterServices;
            WebStartup.StorageService = storageService;

            var host = WebHost.CreateDefaultBuilder()
                .UseKestrel()
                .UseStartup<WebStartup>()
                .UseUrls("http://*:80")
                .Build();

            host.Start();

            _logger.Log(LogLevel.Debug, "HTTP server started.");

            return WebStartup.ServiceProvider;
        }
    }
}
