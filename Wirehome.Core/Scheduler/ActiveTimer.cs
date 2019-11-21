using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Wirehome.Core.Scheduler
{
    public sealed class ActiveTimer : IDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private readonly Action<TimerTickCallbackParameters> _callback;
        private readonly object _state;
        private readonly ILogger _logger;

        public ActiveTimer(string uid, TimeSpan interval, Action<TimerTickCallbackParameters> callback, object state, ILogger logger)
        {
            Uid = uid ?? throw new ArgumentNullException(nameof(uid));
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
            _state = state;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            Interval = interval;
        }

        public void Start()
        {
            Task.Run(() => RunAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
        }

        public string Uid { get; }

        public TimeSpan Interval { get; }

        public Exception LastException { get; private set; }

        public void Stop()
        {
            _cancellationTokenSource.Cancel(false);
            _logger.Log(LogLevel.Debug, "Stopped timer '{0}'.", Uid);
        }

        public void Dispose()
        {
            _cancellationTokenSource?.Dispose();
        }

        private async Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                while (!cancellationToken.IsCancellationRequested)
                {
                    // TODO: Consider adding a flag "HighPrecision=true|false". Then use Thread.Sleep or await to safe threads.
                    //Thread.Sleep(Interval);
                    await Task.Delay(Interval, cancellationToken).ConfigureAwait(false);

                    // Ensure that the tick is not called when the task was canceled during the sleep time.
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
                _logger.Log(LogLevel.Error, exception, $"Error while executing timer '{Uid}'.");
            }
        }

        private void TryTick(TimeSpan elapsed)
        {
            try
            {
                _callback(new TimerTickCallbackParameters(Uid, (int)elapsed.TotalMilliseconds, _state));
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                LastException = exception;

                _logger.Log(LogLevel.Error, exception, $"Error while executing callback of timer '{Uid}'.");

                Thread.Sleep(TimeSpan.FromSeconds(1)); // Prevent flooding the log.
            }
        }
    }
}
