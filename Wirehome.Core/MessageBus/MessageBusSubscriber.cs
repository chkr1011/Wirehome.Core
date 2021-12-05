using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Wirehome.Core.MessageBus
{
    public sealed class MessageBusSubscriber
    {
        readonly Action<IDictionary<object, object>> _callback;
        readonly ILogger _logger;

        long _processedMessagesCount;
        long _pendingMessagesCount;
        long _faultedMessagesCount;

        public MessageBusSubscriber(string uid, IDictionary<object, object> filter, Action<IDictionary<object, object>> callback, ILogger logger)
        {
            Uid = uid ?? throw new ArgumentNullException(nameof(uid));
            Filter = filter ?? throw new ArgumentNullException(nameof(filter));
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string Uid { get; }

        public IDictionary<object, object> Filter { get; }

        public long ProcessedMessagesCount => Interlocked.Read(ref _processedMessagesCount);

        public long FaultedMessagesCount => Interlocked.Read(ref _faultedMessagesCount);

        public long PendingMessagesCount => Interlocked.Read(ref _pendingMessagesCount);

        public void ProcessMessage(IDictionary<object, object> message)
        {
            if (message is null) throw new ArgumentNullException(nameof(message));

            try
            {
                Interlocked.Increment(ref _pendingMessagesCount);

                _callback.Invoke(message);

                Interlocked.Increment(ref _processedMessagesCount);
            }
            catch (Exception exception)
            {
                if (!(exception is OperationCanceledException))
                {
                    _logger.LogError(exception, $"Error while processing bus message for subscriber '{Uid}'.");
                }

                Interlocked.Increment(ref _faultedMessagesCount);
            }
            finally
            {
                Interlocked.Decrement(ref _pendingMessagesCount);
            }
        }
    }
}