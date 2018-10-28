using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Diagnostics;

namespace Wirehome.Core.System
{
    public class SystemService
    {
        private readonly SystemStatusService _systemStatusService;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private readonly ILogger _logger;

        public SystemService(SystemStatusService systemStatusService, ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            _systemStatusService = systemStatusService ?? throw new ArgumentNullException(nameof(systemStatusService));
            _logger = loggerFactory.CreateLogger<SystemService>();
        }

        public event EventHandler RebootInitiated;

        public CancellationToken CancellationToken => _cancellationTokenSource.Token;

        public void Start(DateTime startupTimestamp, string _arguments)
        {
            _systemStatusService.Set("startup.timestamp", startupTimestamp);
            _systemStatusService.Set("startup.duration", DateTime.Now - startupTimestamp);
            
            _systemStatusService.Set("framework.description", RuntimeInformation.FrameworkDescription);

            _systemStatusService.Set("process.architecture", RuntimeInformation.ProcessArchitecture);
            _systemStatusService.Set("process.id", Process.GetCurrentProcess().Id);

            _systemStatusService.Set("system.date_time", () => DateTime.Now);
            _systemStatusService.Set("system.processor_count", Environment.ProcessorCount);
            _systemStatusService.Set("system.working_set", () => Environment.WorkingSet);

            _systemStatusService.Set("up_time", () => DateTime.Now - startupTimestamp);

            _systemStatusService.Set("arguments", _arguments);

            _systemStatusService.Set("wirehome.core.version", WirehomeVersion.Version);

            AddOSInformation();
            AddThreadPoolInformation();
        }

        public void Reboot(int waitTime)
        {
            _logger.Log(LogLevel.Information, "Reboot initiated.");
            RebootInitiated?.Invoke(this, EventArgs.Empty);

            _cancellationTokenSource.Cancel(false);

            Task.Run(() =>
            {
                Thread.Sleep(TimeSpan.FromSeconds(waitTime));
                Process.Start("shutdown", " -r now");
            }, CancellationToken);
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
