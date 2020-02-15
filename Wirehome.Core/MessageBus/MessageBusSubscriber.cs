using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Wirehome.Core.MessageBus
{
    public class MessageBusSubscriber
    {
        private readonly Queue<MessageBusMessage> _messageQueue = new Queue<MessageBusMessage>();
        private readonly Action<MessageBusMessage> _callback;
        private readonly ILogger _logger;

        private int _processorGate;

        public MessageBusSubscriber(string uid, IDictionary<object, object> filter, Action<MessageBusMessage> callback, ILogger logger)
        {
            Uid = uid ?? throw new ArgumentNullException(nameof(uid));
            Filter = filter ?? throw new ArgumentNullException(nameof(filter));
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public string Uid { get; }

        public IDictionary<object, object> Filter { get; }

        public int ProcessedMessagesCount { get; private set; }

        public int FaultedMessagesCount { get; private set; }

        public int PendingMessagesCount => _messageQueue.Count;

        public void EnqueueMessage(MessageBusMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            lock (_messageQueue)
            {
                _messageQueue.Enqueue(message);
            }
        }

        public bool TryProcessNextMessage()
        {
            var isFirstProcessor = Interlocked.Increment(ref _processorGate) == 1;
            try
            {
                if (!isFirstProcessor)
                {
                    // Ensures that only one out of n threads will process messages for this
                    // instance at a time. The thread will return here and continues with
                    // the next subscriber.
                    return false;
                }

                MessageBusMessage message;
                lock (_messageQueue)
                {
                    if (!_messageQueue.TryDequeue(out message))
                    {
                        return false;
                    }
                }

                _callback.Invoke(message);

                ProcessedMessagesCount++;
                return true;
            }
            catch (Exception exception)
            {
                if (!(exception is OperationCanceledException))
                {
                    _logger.LogError(exception, $"Error while processing bus message for subscriber '{Uid}'.");
                }

                FaultedMessagesCount++;
                return false;
            }
            finally
            {
                Interlocked.Decrement(ref _processorGate);
            }
        }
    }
}