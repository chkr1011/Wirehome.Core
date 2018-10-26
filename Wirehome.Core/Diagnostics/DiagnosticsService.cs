using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wirehome.Core.System;

namespace Wirehome.Core.Diagnostics
{
    public class DiagnosticsService
    {
        private readonly ConcurrentBag<OperationsPerSecondCounter> _operationsPerSecondCounters = new ConcurrentBag<OperationsPerSecondCounter>();
        private readonly SystemService _systemService;

        private readonly ILogger _logger;

        public DiagnosticsService(SystemService systemService, ILoggerFactory loggerFactory)
        {
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            _systemService = systemService ?? throw new ArgumentNullException(nameof(systemService));

            _logger = loggerFactory.CreateLogger<DiagnosticsService>();
        }

        public void Start()
        {
            Task.Run(() => ResetOperationsPerSecondCountersAsync(_systemService.CancellationToken), _systemService.CancellationToken).ConfigureAwait(false);
        }

        public OperationsPerSecondCounter CreateOperationsPerSecondCounter(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            var operationsPerSecondCounter = new OperationsPerSecondCounter(uid);
            _operationsPerSecondCounters.Add(operationsPerSecondCounter);

            return operationsPerSecondCounter;
        }

        private async Task ResetOperationsPerSecondCountersAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);

                    foreach (var operationsPerSecondCounter in _operationsPerSecondCounters)
                    {
                        operationsPerSecondCounter.Reset();
                    }
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Error while resetting OperationsPerSecondCounters.");
                }
            }
        }
    }
}
