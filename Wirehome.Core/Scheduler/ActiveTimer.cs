using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Wirehome.Core.Scheduler
{
    public class ActiveTimer
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private readonly string _uid;
        private readonly TimeSpan _interval;
        private readonly Action<string, TimeSpan> _callback;
        private readonly ILogger _logger;

        public ActiveTimer(string uid, TimeSpan interval, Action<string, TimeSpan> callback, ILoggerFactory loggerFactory)
        {
            _uid = uid ?? throw new ArgumentNullException(nameof(uid));
            _interval = interval;
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));

            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<ActiveTimer>();
        }

        public void Start()
        {
            Task.Factory.StartNew(() => Run(_cancellationTokenSource.Token), _cancellationTokenSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private void Run(CancellationToken cancellationToken)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                while (!cancellationToken.IsCancellationRequested)
                {
                    var elapsed = stopwatch.Elapsed;
                    TryTick(elapsed);
                    stopwatch.Restart();

                    Thread.Sleep(_interval);
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
                _callback(_uid, elapsed);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _logger.Log(LogLevel.Error, exception, "Error while executing timer callback.");
            }
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel(false);
            _logger.Log(LogLevel.Debug, "Stopped timer '{0}'.", _uid);
        }
    }
}
