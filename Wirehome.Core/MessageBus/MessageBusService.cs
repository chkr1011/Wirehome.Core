using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Contracts;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.Extensions;
using Wirehome.Core.Hardware.MQTT;
using Wirehome.Core.Storage;
using Wirehome.Core.System;

namespace Wirehome.Core.MessageBus;

public sealed class MessageBusService : WirehomeCoreService
{
    readonly OperationsPerSecondCounter _inboundCounter;

    readonly ILogger _logger;
    readonly MessageBusMessageHistory _messageHistory = new();
    readonly BlockingCollection<MessageBusMessage> _messageQueue = new();
    
    readonly OperationsPerSecondCounter _processingRateCounter;
    readonly ConcurrentDictionary<string, MessageBusSubscriber> _subscribers = new();

    readonly SystemCancellationToken _systemCancellationToken;

    public MessageBusService(StorageService storageService,
        SystemStatusService systemStatusService,
        DiagnosticsService diagnosticsService,
        SystemCancellationToken systemCancellationToken,
        MqttService mqttService,
        ILogger<MessageBusService> logger)
    {
        Sender = new MessageBusSender(mqttService);

        _systemCancellationToken = systemCancellationToken ?? throw new ArgumentNullException(nameof(systemCancellationToken));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (storageService == null)
        {
            throw new ArgumentNullException(nameof(storageService));
        }
        
        if (diagnosticsService == null)
        {
            throw new ArgumentNullException(nameof(diagnosticsService));
        }

        _inboundCounter = diagnosticsService.CreateOperationsPerSecondCounter("message_bus.inbound_rate");
        _processingRateCounter = diagnosticsService.CreateOperationsPerSecondCounter("message_bus.processing_rate");

        if (systemStatusService == null)
        {
            throw new ArgumentNullException(nameof(systemStatusService));
        }

        systemStatusService.Set("message_bus.queued_messages_count", () => _messageQueue.Count);
        systemStatusService.Set("message_bus.subscribers_count", () => _subscribers.Count);
        systemStatusService.Set("message_bus.inbound_rate", () => _inboundCounter.Count);
        systemStatusService.Set("message_bus.processing_rate", () => _processingRateCounter.Count);
    }

    public MessageBusSender Sender { get; }

    public void ClearHistory()
    {
        _messageHistory.Clear();
    }

    public void DisableHistory()
    {
        _messageHistory.Disable();
    }

    public void EnableHistory(int maxMessagesCount)
    {
        _messageHistory.Enable(maxMessagesCount);
    }

    public List<MessageBusMessage> GetHistory()
    {
        return _messageHistory.GetMessages();
    }

    public List<MessageBusSubscriber> GetSubscribers()
    {
        return _subscribers.Values.ToList();
    }

    public void Publish(IDictionary<object, object> message)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        var busMessage = new MessageBusMessage(message)
        {
            EnqueuedTimestamp = DateTime.UtcNow
        };

        Sender.TrySend(busMessage);

        _messageQueue.Add(busMessage);

        _inboundCounter.Increment();
    }

    public string Subscribe(string uid, IDictionary<object, object> filter, Action<IDictionary<object, object>> callback)
    {
        if (filter == null)
        {
            throw new ArgumentNullException(nameof(filter));
        }

        if (callback == null)
        {
            throw new ArgumentNullException(nameof(callback));
        }

        if (string.IsNullOrEmpty(uid))
        {
            uid = Guid.NewGuid().ToString("D");
        }

        _subscribers[uid] = new MessageBusSubscriber(uid, filter, callback, _logger);

        return uid;
    }

    public void Unsubscribe(string uid)
    {
        if (uid == null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        _subscribers.Remove(uid, out _);
    }

    protected override void OnStart()
    {
        var cancellationToken = _systemCancellationToken.Token;
        ParallelTask.StartLongRunning(() => ProcessMessagesLoop(cancellationToken), cancellationToken, _logger);
    }

    void ProcessMessagesLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var messageBusMessage = _messageQueue.Take(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                if (messageBusMessage == null)
                {
                    continue;
                }

                _messageHistory.Add(messageBusMessage);

                // Convert the inner message for optimized checking.
                var compareMessage = new Dictionary<string, string>(messageBusMessage.InnerMessage.Count);
                foreach (var innerMessageItem in messageBusMessage.InnerMessage)
                {
                    var key = Convert.ToString(innerMessageItem.Key, CultureInfo.InvariantCulture) ?? string.Empty;
                    var value = Convert.ToString(innerMessageItem.Value, CultureInfo.InvariantCulture) ?? string.Empty;

                    compareMessage[key] = value;
                }

                // Check for affected subscribers.
                foreach (var subscriberItem in _subscribers)
                {
                    var subscriber = subscriberItem.Value;

                    if (MessageBusFilterComparer.IsMatch(compareMessage, subscriber.Filter))
                    {
                        subscriber.ProcessMessage(messageBusMessage.InnerMessage);

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