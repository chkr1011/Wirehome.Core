using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Wirehome.Core.Contracts;
using Wirehome.Core.Extensions;
using Wirehome.Core.System;

namespace Wirehome.Core.Diagnostics
{
    public sealed class DiagnosticsService : WirehomeCoreService
    {
        readonly List<OperationsPerSecondCounter> _operationsPerSecondCounters = new();
        readonly SystemCancellationToken _systemCancellationToken;
        
        readonly ILogger _logger;

        public DiagnosticsService(SystemCancellationToken systemCancellationToken, ILogger<DiagnosticsService> logger)
        {
            _systemCancellationToken = systemCancellationToken ?? throw new ArgumentNullException(nameof(systemCancellationToken));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public OperationsPerSecondCounter CreateOperationsPerSecondCounter(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            var operationsPerSecondCounter = new OperationsPerSecondCounter(uid);

            lock (_operationsPerSecondCounters)
            {
                _operationsPerSecondCounters.Add(operationsPerSecondCounter);
            }

            return operationsPerSecondCounter;
        }

        protected override void OnStart()
        {
            ParallelTask.Start(() => ResetOperationsPerSecondCountersAsync(_systemCancellationToken.Token), _systemCancellationToken.Token, _logger, TaskCreationOptions.LongRunning);
        }

        void ResetOperationsPerSecondCountersAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                    
                    lock (_operationsPerSecondCounters)
                    {
                        foreach (var operationsPerSecondCounter in _operationsPerSecondCounters)
                        {
                            operationsPerSecondCounter.Reset();
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Error while resetting operations per second counters.");
                }
            }
        }
    }
}
