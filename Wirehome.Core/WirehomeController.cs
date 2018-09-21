using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Areas;
using Wirehome.Core.Automations;
using Wirehome.Core.Components;
using Wirehome.Core.Constants;
using Wirehome.Core.Diagnostics;
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
using Wirehome.Core.Repositories;
using Wirehome.Core.Resources;
using Wirehome.Core.Scheduler;
using Wirehome.Core.ServiceHost;
using Wirehome.Core.Storage;
using Wirehome.Core.System;

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
                _logger.Log(LogLevel.Information, "Starting Wirehome Core (c) Christian Kratky 2011 - 2018");

                var serviceProvider = StartHttpServer();
                _loggerFactory.AddProvider(new LogServiceLoggerProvider(serviceProvider.GetService<LogService>()));

                SetupSystemStatusService(timestamp, serviceProvider);
                SetupHardwareAdapters(serviceProvider); // TODO: From config!

                StartServices(serviceProvider);

                RegisterDefaultResources(serviceProvider);
                RegisterDefaultGlobalVariables(serviceProvider);
                RegisterEvents(serviceProvider);

                PublishBootedNotification(serviceProvider);

                _logger.Log(LogLevel.Information, "Starting Wirehome completed.");
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
            messageBusService.Publish(new WirehomeDictionary
            {
                ["type"] = MessageBusMessageTypes.Booted
            });

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

            serviceCollection.AddSingleton(typeof(HttpServerService));

            serviceCollection.AddSingleton(typeof(LogService));
            serviceCollection.AddSingleton(typeof(SystemService));
            serviceCollection.AddSingleton(typeof(SystemStatusService));
            serviceCollection.AddSingleton(typeof(GlobalVariablesService));

            serviceCollection.AddSingleton(typeof(StorageService));
            serviceCollection.AddSingleton(typeof(ResourcesService));
            serviceCollection.AddSingleton(typeof(FunctionPoolService));

            serviceCollection.AddSingleton(typeof(MessageBusService));
            serviceCollection.AddSingleton(typeof(SchedulerService));
            serviceCollection.AddSingleton(typeof(PythonEngineService));

            serviceCollection.AddSingleton(typeof(MqttService));
            serviceCollection.AddSingleton(typeof(I2CBusService));
            serviceCollection.AddSingleton(typeof(GpioRegistryService));

            serviceCollection.AddSingleton(typeof(NotificationsService));

            serviceCollection.AddSingleton(typeof(AreaRegistryService));

            serviceCollection.AddSingleton(typeof(RepositoryService));

            serviceCollection.AddSingleton(typeof(HistoryService));

            serviceCollection.AddSingleton(typeof(ServiceHostService));
            serviceCollection.AddSingleton(typeof(ComponentRegistryService));
            serviceCollection.AddSingleton(typeof(ComponentInitializerFactory));
            serviceCollection.AddSingleton(typeof(AutomationsRegistryService));
            serviceCollection.AddSingleton(typeof(MacroRegistryService));
        }

        private void StartServices(IServiceProvider serviceProvider)
        {
            _logger.Log(LogLevel.Debug, "Starting services...");

            serviceProvider.GetService<MessageBusService>().Start();
            serviceProvider.GetService<ResourcesService>().Start();
            serviceProvider.GetService<GlobalVariablesService>().Start();

            serviceProvider.GetService<SchedulerService>().Start();

            serviceProvider.GetService<MqttService>().Start();
            serviceProvider.GetService<HttpServerService>().Start();

            serviceProvider.GetService<PythonEngineService>().Start();
            serviceProvider.GetService<FunctionPoolService>().Start();
            serviceProvider.GetService<ServiceHostService>().Start();
            
            serviceProvider.GetService<NotificationsService>().Start();

            serviceProvider.GetService<HistoryService>().Start();

            serviceProvider.GetService<AreaRegistryService>().Start();
            serviceProvider.GetService<ComponentRegistryService>().Start();
            serviceProvider.GetService<AutomationsRegistryService>().Start();
            serviceProvider.GetService<MacroRegistryService>().Start();

            _logger.Log(LogLevel.Debug, "Service startup completed.");
        }

        private void SetupHardwareAdapters(IServiceProvider serviceProvider)
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

        private void SetupSystemStatusService(DateTime startupTimestamp, IServiceProvider serviceProvider)
        {
            var systemStatusService = serviceProvider.GetService<SystemStatusService>();

            systemStatusService.Set("startup.timestamp", startupTimestamp);
            systemStatusService.Set("startup.duration", DateTime.Now - startupTimestamp);

            systemStatusService.Set("os.description", RuntimeInformation.OSDescription);
            systemStatusService.Set("os.architecture", RuntimeInformation.OSArchitecture);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                systemStatusService.Set("os.platform", "linux");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                systemStatusService.Set("os.platform", "windows");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                systemStatusService.Set("os.platform", "osx");
            }

            systemStatusService.Set("framework.description", RuntimeInformation.FrameworkDescription);

            systemStatusService.Set("process.architecture", RuntimeInformation.ProcessArchitecture);
            systemStatusService.Set("process.id", Process.GetCurrentProcess().Id);

            systemStatusService.Set("system.date_time", () => DateTime.Now);

            systemStatusService.Set("up_time", () => DateTime.Now - startupTimestamp);

            systemStatusService.Set("arguments", _arguments);

            systemStatusService.Set("wirehome.core.version", "1.0-alpha1");
            
            _logger.Log(LogLevel.Debug, "System status initialized.");
        }

        private IServiceProvider StartHttpServer()
        {
            _logger.Log(LogLevel.Debug, "Starting HTTP server");

            var webRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WebApp");
            if (!Directory.Exists(webRoot))
            {
                Directory.CreateDirectory(webRoot);
            }

            WebStartup.OnServiceRegistration = RegisterServices;
            var host = WebHost.CreateDefaultBuilder()
                .UseKestrel()
                .UseStartup<WebStartup>()
                .UseUrls("http://*:80")
                .UseContentRoot(webRoot)
                .Build();

            host.Start();

            _logger.Log(LogLevel.Debug, "HTTP server started.");

            return WebStartup.ServiceProvider;
        }
    }
}
