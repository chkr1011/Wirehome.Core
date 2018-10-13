using System;
using System.Collections.Concurrent;
using System.Threading;
using Wirehome.Core.Model;

namespace Wirehome.Core.MessageBus
{
    public class MessageBusSubscriber
    {
        private readonly ConcurrentQueue<WirehomeDictionary> _messageQueue = new ConcurrentQueue<WirehomeDictionary>();
        private readonly Action<WirehomeDictionary> _callback;

        private int _processorGate;

        public MessageBusSubscriber(string uid, WirehomeDictionary filter, Action<WirehomeDictionary> callback)
        {
            Uid = uid ?? throw new ArgumentNullException(nameof(uid));
            Filter = filter ?? throw new ArgumentNullException(nameof(filter));
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        public string Uid { get; }

        public WirehomeDictionary Filter { get; }

        public int ProcessedMessagesCount { get; private set; }

        public int PendingMessagesCount => _messageQueue.Count;

        public void EnqueueMessage(WirehomeDictionary message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            if (!MessageBusFilterComparer.IsMatch(message, Filter))
            {
                return;
            }

            _messageQueue.Enqueue(message);
        }

        public bool ProcessNextMessage()
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

                if (!_messageQueue.TryDequeue(out var message))
                {
                    return false;
                }

                _callback.Invoke(message);
                ProcessedMessagesCount++;

                return true;
            }
            finally
            {
                Interlocked.Decrement(ref _processorGate);
            }
        }
    }
}