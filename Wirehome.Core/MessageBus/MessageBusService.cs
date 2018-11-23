using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Contracts;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.Model;
using Wirehome.Core.Storage;
using Wirehome.Core.System;

namespace Wirehome.Core.MessageBus
{
    public class MessageBusService : IService
    {
        private readonly BlockingCollection<MessageBusMessage> _messageQueue = new BlockingCollection<MessageBusMessage>();
        private readonly LinkedList<MessageBusMessage> _history = new LinkedList<MessageBusMessage>();
        private readonly ConcurrentDictionary<string, MessageBusSubscriber> _subscribers = new ConcurrentDictionary<string, MessageBusSubscriber>();
        private readonly ConcurrentDictionary<string, MessageBusResponseSubscriber> _responseSubscribers = new ConcurrentDictionary<string, MessageBusResponseSubscriber>();

        private readonly SystemCancellationToken _systemCancellationToken;

        private readonly OperationsPerSecondCounter _inboundCounter;
        private readonly OperationsPerSecondCounter _processingRateCounter;

        private readonly MessageBusServiceOptions _options;

        private readonly ILogger _logger;

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
            storageService.TryReadOrCreate(out _options, MessageBusServiceOptions.Filename);

            if (diagnosticsService == null) throw new ArgumentNullException(nameof(diagnosticsService));
            _inboundCounter = diagnosticsService.CreateOperationsPerSecondCounter("message_bus.inbound_rate");
            _processingRateCounter = diagnosticsService.CreateOperationsPerSecondCounter("message_bus.processing_rate");

            if (systemStatusService == null) throw new ArgumentNullException(nameof(systemStatusService));
            systemStatusService.Set("message_bus.queued_messages_count", () => _messageQueue.Count);
            systemStatusService.Set("message_bus.subscribers_count", () => _subscribers.Count);
            systemStatusService.Set("message_bus.inbound_rate", () => _inboundCounter.Count);
            systemStatusService.Set("message_bus.processing_rate", () => _processingRateCounter.Count);
        }

        public void Start()
        {
            Task.Factory.StartNew(
                () => DispatchMessageBusMessages(_systemCancellationToken.Token),
                _systemCancellationToken.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

            for (var i = 0; i < _options.MessageProcessorsCount; i++)
            {
                Task.Factory.StartNew(
                    () => ProcessMessages(_systemCancellationToken.Token),
                    _systemCancellationToken.Token,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default);
            }
        }

        public async Task<WirehomeDictionary> PublishRequestAsync(WirehomeDictionary message, TimeSpan timeout)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var request = new MessageBusMessage
            {
                Message = message
            };

            string requestCorrelationUid = null;
            try
            {
                var responseSubscriber = new MessageBusResponseSubscriber();

                if (message.TryGetValue(MessageBusMessagePropertyName.CorrelationUid, out var buffer))
                {
                    requestCorrelationUid = Convert.ToString(buffer, CultureInfo.InvariantCulture);
                }
                else
                {
                    requestCorrelationUid = Guid.NewGuid().ToString("D");
                }

                _responseSubscribers.TryAdd(requestCorrelationUid, responseSubscriber);

                Publish(request);

                using (var timeoutCts = new CancellationTokenSource(timeout))
                {
                    var responseMessage = await Task.Run(() => responseSubscriber.Task, timeoutCts.Token).ConfigureAwait(false);
                    return responseMessage.Message;
                }
            }
            finally
            {
                if (requestCorrelationUid != null)
                {
                    _responseSubscribers.TryRemove(requestCorrelationUid, out _);
                }
            }
        }

        public void PublishResponse(WirehomeDictionary request, WirehomeDictionary responseMessage)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (responseMessage == null) throw new ArgumentNullException(nameof(responseMessage));

            responseMessage[MessageBusMessagePropertyName.CorrelationUid] =
                request[MessageBusMessagePropertyName.CorrelationUid];

            var response = new MessageBusMessage { Message = responseMessage };
            Publish(response);
        }

        public void Publish(WirehomeDictionary message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var busMessage = new MessageBusMessage
            {
                Message = message
            };

            Publish(busMessage);
        }

        public void Publish(MessageBusMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            message.EnqueuedTimestamp = DateTime.Now;
            _messageQueue.Add(message);

            _inboundCounter.Increment();
        }

        public void ClearHistory()
        {
            lock (_history)
            {
                _history.Clear();
            }
        }

        public List<MessageBusMessage> GetHistory()
        {
            lock (_history)
            {
                return new List<MessageBusMessage>(_history);
            }
        }

        public List<MessageBusSubscriber> GetSubscribers()
        {
            return new List<MessageBusSubscriber>(_subscribers.Values);
        }

        public string Subscribe(string uid, WirehomeDictionary filter, Action<MessageBusMessage> callback)
        {
            if (filter == null) throw new ArgumentNullException(nameof(filter));
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            if (string.IsNullOrEmpty(uid))
            {
                uid = Guid.NewGuid().ToString("D");
            }

            var subscriber = new MessageBusSubscriber(uid, filter, callback, _logger);
            _subscribers[uid] = subscriber;

            return uid;
        }

        public void Unsubscribe(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            _subscribers.TryRemove(uid, out _);
        }

        private void DispatchMessageBusMessages(CancellationToken cancellationToken)
        {
            Thread.CurrentThread.Name = nameof(DispatchMessageBusMessages);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var message = _messageQueue.Take(cancellationToken);
                    if (message == null || cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    lock (_history)
                    {
                        _history.AddFirst(message);
                        if (_history.Count > _options.HistoryItemsCount)
                        {
                            _history.RemoveLast();
                        }
                    }

                    if (message.Message.TryGetValue(MessageBusMessagePropertyName.CorrelationUid, out var correlationUid))
                    {
                        var responseCorrelationUid = Convert.ToString(correlationUid, CultureInfo.InvariantCulture);

                        foreach (var responseSubscriber in _responseSubscribers)
                        {
                            if (responseSubscriber.Key.Equals(responseCorrelationUid, StringComparison.Ordinal))
                            {
                                responseSubscriber.Value.SetResponse(message);
                                break;
                            }
                        }
                    }
                    else
                    {
                        foreach (var subscriber in _subscribers.Values)
                        {
                            subscriber.EnqueueMessage(message);
                        }
                    }
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

        private void ProcessMessages(CancellationToken cancellationToken)
        {
            try
            {
                Thread.CurrentThread.Name = nameof(ProcessMessages);

                while (!cancellationToken.IsCancellationRequested)
                {
                    foreach (var subscriber in _subscribers.Values)
                    {
                        if (subscriber.TryProcessNextMessage())
                        {
                            _processingRateCounter.Increment();
                        }
                    }

                    Thread.Sleep(10);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Message processor faulted.");
            }
        }
    }
}
