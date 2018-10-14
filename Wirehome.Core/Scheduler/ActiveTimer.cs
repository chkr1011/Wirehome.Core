using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Wirehome.Core.Scheduler
{
    public sealed class ActiveTimer : IDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private readonly Action<string, TimeSpan> _callback;
        private readonly ILogger _logger;

        public ActiveTimer(string uid, TimeSpan interval, Action<string, TimeSpan> callback, ILoggerFactory loggerFactory)
        {
            Uid = uid ?? throw new ArgumentNullException(nameof(uid));
            Interval = interval;
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));

            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<ActiveTimer>();
        }

        public void Start()
        {
            Task.Factory.StartNew(() => Run(_cancellationTokenSource.Token), _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public string Uid { get; }

        public TimeSpan Interval { get; }

        public void Stop()
        {
            _cancellationTokenSource.Cancel(false);
            _logger.Log(LogLevel.Debug, "Stopped timer '{0}'.", Uid);
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
        }

        private void Run(CancellationToken cancellationToken)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                while (!cancellationToken.IsCancellationRequested)
                {
                    // TODO: Consider adding a flag "HighPrecision=true|false". Then use Thread.Sleep or await to safe threads.
                    Thread.Sleep(Interval);

                    // Ensure that the tick is not called when the task was cancelled during the sleep time.
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    var elapsed = stopwatch.Elapsed;
                    stopwatch.Restart();
                    TryTick(elapsed);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _logger.Log(LogLevel.Error, exception, "Error while executing timer.");
            }
        }

        private void TryTick(TimeSpan elapsed)
        {
            try
            {
                _callback(Uid, elapsed);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _logger.Log(LogLevel.Error, exception, "Error while executing timer callback.");
                Thread.Sleep(5000); // Prevent flooding the log.
            }
        }
    }
}
