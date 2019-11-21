using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Wirehome.Core.Constants;
using Wirehome.Core.Contracts;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.MessageBus;
using Wirehome.Core.Foundation.Model;
using Wirehome.Core.Notifications;

namespace Wirehome.Core.System
{
    public class SystemService : IService
    {
        private readonly SystemStatusService _systemStatusService;
        private readonly SystemLaunchArguments _systemLaunchArguments;
        private readonly NotificationsService _notificationsService;
        private readonly MessageBusService _messageBusService;

        private readonly ILogger _logger;
        private readonly DateTime _creationTimestamp;

        public SystemService(
            SystemStatusService systemStatusService,
            SystemLaunchArguments systemLaunchArguments,
            SystemCancellationToken systemCancellationToken,
            NotificationsService notificationsService,
            MessageBusService messageBusService,
            ILogger<SystemService> logger)
        {
            _systemStatusService = systemStatusService ?? throw new ArgumentNullException(nameof(systemStatusService));
            _systemLaunchArguments = systemLaunchArguments ?? throw new ArgumentNullException(nameof(systemLaunchArguments));
            _notificationsService = notificationsService ?? throw new ArgumentNullException(nameof(notificationsService));
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _creationTimestamp = DateTime.Now;
        }

        public event EventHandler ServicesInitialized;
        public event EventHandler ConfigurationLoaded;
        public event EventHandler StartupCompleted;
        
        public void Start()
        {
            _systemStatusService.Set("startup.timestamp", _creationTimestamp);
            _systemStatusService.Set("startup.duration", null);

            _systemStatusService.Set("framework.description", RuntimeInformation.FrameworkDescription);

            _systemStatusService.Set("process.architecture", RuntimeInformation.ProcessArchitecture);
            _systemStatusService.Set("process.id", Process.GetCurrentProcess().Id);

            _systemStatusService.Set("system.date_time", () => DateTime.Now);
            _systemStatusService.Set("system.processor_count", Environment.ProcessorCount);
            _systemStatusService.Set("system.working_set", () => Environment.WorkingSet);

            _systemStatusService.Set("up_time", () => DateTime.Now - _creationTimestamp);

            _systemStatusService.Set("arguments", string.Join(" ", _systemLaunchArguments.Values));

            _systemStatusService.Set("wirehome.core.version", WirehomeCoreVersion.Version);

            AddOSInformation();
            AddThreadPoolInformation();
        }

        public void Reboot(int waitTime)
        {
            _logger.LogInformation("Reboot initiated.");

            _notificationsService.PublishFromResource(new PublishFromResourceParameters
            {
                Type = NotificationType.Warning,
                ResourceUid = NotificationResourceUids.RebootInitiated,
                Parameters = new WirehomeDictionary
                {
                    ["wait_time"] = 0 // TODO: Add to event args.
                }
            });

            _messageBusService.Publish(new WirehomeDictionary().WithType("system.reboot_initiated"));

            Task.Run(() =>
            {
                Thread.Sleep(TimeSpan.FromSeconds(waitTime));
                Process.Start("shutdown", " -r now");
            }, CancellationToken.None);
        }

        public void OnServicesInitialized()
        {
            ServicesInitialized?.Invoke(this, EventArgs.Empty);

            _logger.LogInformation("Service startup completed.");
        }

        public void OnConfigurationLoaded()
        {
            ConfigurationLoaded?.Invoke(this, EventArgs.Empty);

            _logger.LogInformation("Configuration loaded.");
        }

        public void OnStartupCompleted()
        {
            _systemStatusService.Set("startup.duration", DateTime.Now - _creationTimestamp);

            PublishBootedNotification();

            StartupCompleted?.Invoke(this, EventArgs.Empty);

            _logger.LogInformation("Startup completed.");
        }

        private void PublishBootedNotification()
        {
            _messageBusService.Publish(new WirehomeDictionary().WithType("system.booted"));

            _notificationsService.PublishFromResource(new PublishFromResourceParameters
            {
                Type = NotificationType.Information,
                ResourceUid = NotificationResourceUids.Booted
            });
        }

        private void AddOSInformation()
        {
            _systemStatusService.Set("os.description", RuntimeInformation.OSDescription);
            _systemStatusService.Set("os.architecture", RuntimeInformation.OSArchitecture);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                _systemStatusService.Set("os.platform", "linux");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                _systemStatusService.Set("os.platform", "windows");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                _systemStatusService.Set("os.platform", "osx");
            }
        }

        private void AddThreadPoolInformation()
        {
            _systemStatusService.Set("thread_pool.max_worker_threads", () =>
            {
                ThreadPool.GetMaxThreads(out var x, out _);
                return x;
            });

            _systemStatusService.Set("thread_pool.max_completion_port_threads", () =>
            {
                ThreadPool.GetMaxThreads(out _, out var x);
                return x;
            });

            _systemStatusService.Set("thread_pool.min_worker_threads", () =>
            {
                ThreadPool.GetMinThreads(out var x, out _);
                return x;
            });

            _systemStatusService.Set("thread_pool.min_completion_port_threads", () =>
            {
                ThreadPool.GetMinThreads(out _, out var x);
                return x;
            });

            _systemStatusService.Set("thread_pool.available_worker_threads", () =>
            {
                ThreadPool.GetAvailableThreads(out var x, out _);
                return x;
            });

            _systemStatusService.Set("thread_pool.available_completion_port_threads", () =>
            {
                ThreadPool.GetAvailableThreads(out _, out var x);
                return x;
            });
        }
    }
}
