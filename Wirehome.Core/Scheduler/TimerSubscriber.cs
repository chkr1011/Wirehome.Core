using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;

namespace Wirehome.Core.Scheduler
{
    public class TimerSubscriber
    {
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private readonly Action<TimerTickCallbackParameters> _callback;
        private readonly object _state;
        private readonly ILogger _logger;

        public TimerSubscriber(string uid, Action<TimerTickCallbackParameters> callback, object state, ILogger logger)
        {
            Uid = uid ?? throw new ArgumentNullException(nameof(uid));
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _state = state;
        }

        public string Uid { get; }

        public void TryInvokeCallback()
        {
            try
            {
                _stopwatch.Stop();
                var elapsed = _stopwatch.Elapsed;

                _callback(new TimerTickCallbackParameters(Uid, (int)elapsed.TotalMilliseconds, _state));
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"Error while invoking callback of default timer subscriber '{Uid}'.");
            }
            finally
            {
                _stopwatch.Restart();
            }
        }
    }
}