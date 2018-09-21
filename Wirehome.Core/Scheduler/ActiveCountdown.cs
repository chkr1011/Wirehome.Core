using System;
using Microsoft.Extensions.Logging;

namespace Wirehome.Core.Scheduler
{
    public class ActiveCountdown
    {
        private readonly ILogger _logger;
        private readonly Action<string> _callback;

        public ActiveCountdown(string uid, Action<string> callback, ILoggerFactory loggerFactory)
        {
            Uid = uid ?? throw new ArgumentNullException(nameof(uid));
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));

            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<ActiveCountdown>();
        }

        public string Uid { get; }

        public TimeSpan TimeLeft { get; set; }
        
        public void TryInvokeCallback()
        {
            try
            {
                _callback(Uid);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while executing countdown callback.");
            }
        }
    }
}