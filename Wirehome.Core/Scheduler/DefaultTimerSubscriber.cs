using Microsoft.Extensions.Logging;
using System;

namespace Wirehome.Core.Scheduler
{
    public class DefaultTimerSubscriber
    {
        private readonly Action<TimerTickCallbackParameters> _callback;
        private readonly object _state;
        private readonly ILogger _logger;

        public DefaultTimerSubscriber(string uid, Action<TimerTickCallbackParameters> callback, object state, ILogger logger)
        {
            Uid = uid ?? throw new ArgumentNullException(nameof(uid));
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _state = state;
        }

        public string Uid { get; }

        public void TryInvokeCallback(TimeSpan elapsed)
        {
            try
            {
                _callback(new TimerTickCallbackParameters(Uid, (int)elapsed.TotalMilliseconds, _state));
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"Error while invoking callback of default timer subscriber '{Uid}'.");
            }
        }
    }
}