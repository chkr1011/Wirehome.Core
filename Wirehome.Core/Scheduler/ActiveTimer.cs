using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Wirehome.Core.Extensions;

namespace Wirehome.Core.Scheduler
{
    public sealed class ActiveTimer : IDisposable
    {
        readonly CancellationTokenSource _ownCancellationTokenSource = new CancellationTokenSource();
        
        readonly Action<TimerTickCallbackParameters> _callback;
        readonly object _state;
        readonly ILogger _logger;

        CancellationTokenSource _cancellationTokenSource;

        public ActiveTimer(string uid, TimeSpan interval, Action<TimerTickCallbackParameters> callback, object state, ILogger logger)
        {
            Uid = uid ?? throw new ArgumentNullException(nameof(uid));
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
            _state = state;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            Interval = interval;
        }

        public void Start(CancellationToken cancellationToken)
        {
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _ownCancellationTokenSource.Token);
            Task.Run(() => RunAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token).Forget(_logger);
        }

        public string Uid { get; }

        public TimeSpan Interval { get; }

        public Exception LastException { get; private set; }

        public TimeSpan LastDuration { get; private set; }

        public void Stop()
        {
            _ownCancellationTokenSource.Cancel(false);

            _logger.LogTrace($"Stopped timer '{Uid}'.");
        }

        public void Dispose()
        {
            _ownCancellationTokenSource?.Dispose();
            _cancellationTokenSource?.Dispose();
        }

        async Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                while (!cancellationToken.IsCancellationRequested)
                {
                    if (Interval < TimeSpan.FromSeconds(1))
                    {
                        Thread.Sleep(Interval);
                    }
                    else
                    {
                        await Task.Delay(Interval, cancellationToken).ConfigureAwait(false);
                    }

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

        void TryTick(TimeSpan elapsed)
        {
            var stopwatch = Stopwatch.StartNew();
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

                _logger.LogError(exception, $"Error while executing callback of timer '{Uid}'.");

                Thread.Sleep(TimeSpan.FromSeconds(1)); // Prevent flooding the log.
            }
            finally
            {
                LastDuration = stopwatch.Elapsed;
            }
        }
    }
}
