using Microsoft.Extensions.Logging;
using System;

namespace Wirehome.Core.Scheduler
{
    public class ActiveCountdown
    {
        readonly ILogger _logger;
        readonly Action<CountdownElapsedParameters> _callback;
        readonly object _state;

        public ActiveCountdown(string uid, Action<CountdownElapsedParameters> callback, object state, ILogger logger)
        {
            Uid = uid ?? throw new ArgumentNullException(nameof(uid));
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _state = state;
        }

        public string Uid { get; }

        public TimeSpan TimeLeft { get; set; }
        
        public void TryInvokeCallback()
        {
            try
            {
                _callback(new CountdownElapsedParameters(Uid, _state));
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"Error while executing callback of countdown '{Uid}'.");
            }
        }
    }
}