using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace Wirehome.Core.System
{
    public class SystemService
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private readonly ILogger _logger;

        public SystemService(ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<SystemService>();
        }

        public event EventHandler RebootInitiated;

        public CancellationToken CancellationToken => _cancellationTokenSource.Token;
        
        public void Reboot(int waitTime)
        {
            _logger.Log(LogLevel.Information, "Reboot initiated.");
            RebootInitiated?.Invoke(this, EventArgs.Empty);

            _cancellationTokenSource.Cancel(false);
            Thread.Sleep(TimeSpan.FromSeconds(waitTime));

            Process.Start("sudo shutdown -r now");
        }
    }
}
