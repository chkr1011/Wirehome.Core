using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Wirehome.Core.Contracts;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.Extensions;
using Wirehome.Core.Storage;
using Wirehome.Core.System;

namespace Wirehome.Core.MessageBus
{
    public sealed class MessageBusService : WirehomeCoreService
    {
        readonly BlockingCollection<MessageBusMessage> _messageQueue = new();
        readonly MessageBusMessageHistory _messageHistory = new();
        readonly ConcurrentDictionary<string, MessageBusSubscriber> _subscribers = new();
        
        readonly SystemCancellationToken _systemCancellationToken;

        readonly OperationsPerSecondCounter _inboundCounter;
        readonly OperationsPerSecondCounter _processingRateCounter;

        readonly MessageBusServiceOptions _options;

        readonly ILogger _logger;

        public MessageBusService(
            StorageService storageService,
            SystemStatusService systemStatusService,
            DiagnosticsService diagnosticsService,
            SystemCancellationToken systemCancellationToken,
            ILogger<MessageBusService> logger)
        {
            _systemCancellationToken = systemCancellationToken ?? throw new ArgumentNullException(nameof(systemCancellationToken));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (storageService == null) throw new ArgumentNullException(nameof(storageService));
            storageService.SafeReadSerializedValue(out _options, DefaultDirectoryNames.Configuration, MessageBusServiceOptions.Filename);

            if (diagnosticsService == null) throw new ArgumentNullException(nameof(diagnosticsService));
            _inboundCounter = diagnosticsService.CreateOperationsPerSecondCounter("message_bus.inbound_rate");
            _processingRateCounter = diagnosticsService.CreateOperationsPerSecondCounter("message_bus.processing_rate");

            if (systemStatusService == null) throw new ArgumentNullException(nameof(systemStatusService));
            systemStatusService.Set("message_bus.queued_messages_count", () => _messageQueue.Count);
            systemStatusService.Set("message_bus.subscribers_count", () => _subscribers.Count);
            systemStatusService.Set("message_bus.inbound_rate", () => _inboundCounter.Count);
            systemStatusService.Set("message_bus.processing_rate", () => _processingRateCounter.Count);
        }

        public void Publish(IDictionary<object, object> message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var busMessage = new MessageBusMessage
            {
                InnerMessage = message
            };

            Publish(busMessage);
        }

        public void Publish(MessageBusMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            message.EnqueuedTimestamp = DateTime.UtcNow;
            _messageQueue.Add(message);

            _inboundCounter.Increment();
        }

        public void EnableHistory(int maxMessagesCount)
        {
            _messageHistory.Enable(maxMessagesCount);
        }

        public void DisableHistory()
        {
            _messageHistory.Disable();
        }

        public void ClearHistory()
        {
            _messageHistory.Clear();
        }

        public List<MessageBusMessage> GetHistory()
        {
            return _messageHistory.GetMessages();
        }

        public List<MessageBusSubscriber> GetSubscribers()
        {
            return _subscribers.Values.ToList();
        }

        public string Subscribe(string uid, IDictionary<object, object> filter, Action<IDictionary<object, object>> callback)
        {
            if (filter == null) throw new ArgumentNullException(nameof(filter));
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            if (string.IsNullOrEmpty(uid))
            {
                uid = Guid.NewGuid().ToString("D");
            }

            _subscribers[uid] = new MessageBusSubscriber(uid, filter, callback, _logger);

            return uid;
        }

        public void Unsubscribe(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            _subscribers.Remove(uid, out _);
        }

        protected override void OnStart()
        {
            var cancellationToken = _systemCancellationToken.Token;
            ParallelTask.StartLongRunning(() => DispatchMessageBusMessages(cancellationToken), cancellationToken, _logger);
        }

        void DispatchMessageBusMessages(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var message = _messageQueue.Take(cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    if (message == null)
                    {
                        continue;
                    }

                    _messageHistory.Add(message);

                    foreach (var subscriber in _subscribers.Values)
                    {
                        if (MessageBusFilterComparer.IsMatch(message.InnerMessage, subscriber.Filter))
                        {
                            subscriber.ProcessMessage(message.InnerMessage);
                            
                            _processingRateCounter.Increment();
                        }
                    }
                }
                catch (ThreadAbortException)
                {
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Error while dispatching messages.");
                    Thread.Sleep(TimeSpan.FromSeconds(1));
                }
            }
        }
    }
}
