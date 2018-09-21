using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.Extensions;
using Wirehome.Core.Model;
using Wirehome.Core.Python;
using Wirehome.Core.Python.Proxies;
using Wirehome.Core.System;

namespace Wirehome.Core.MessageBus
{
    public class MessageBusService
    {
        private readonly BlockingCollection<BusMessage> _messages = new BlockingCollection<BusMessage>();
        private readonly Queue<BusMessage> _history = new Queue<BusMessage>();

        private readonly SystemService _systemService;

        private readonly OperationsPerSecondCounter _inboundCounter = new OperationsPerSecondCounter();
        private readonly OperationsPerSecondCounter _outboundCounter = new OperationsPerSecondCounter();

        private readonly Dictionary<string, MessageBusInterceptor> _messageInterceptors = new Dictionary<string, MessageBusInterceptor>();
        private readonly Dictionary<string, MessageBusSubscriber> _subscribers = new Dictionary<string, MessageBusSubscriber>();

        private readonly ILogger _logger;

        public MessageBusService(PythonEngineService pythonEngineService, SystemStatusService systemStatusService, SystemService systemService, ILoggerFactory loggerFactory)
        {
            if (systemStatusService == null) throw new ArgumentNullException(nameof(systemStatusService));
            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            _systemService = systemService;

            _logger = loggerFactory.CreateLogger<MessageBusService>();

            pythonEngineService.RegisterSingletonProxy(new MessageBusPythonProxy(this));

            systemStatusService.Set("message_bus_service.queued_messages", () => _messages.Count);
            systemStatusService.Set("message_bus_service.inbound_rate", () => _inboundCounter.Count);
            systemStatusService.Set("message_bus_service.outbound_rate", () => _outboundCounter.Count);
            systemStatusService.Set("message_bus_service.subscribers_count", () => _subscribers.Count);
        }

        public void Start()
        {
            Task.Factory.StartNew(() => TryDispatchMessages(_systemService.CancellationToken), _systemService.CancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public void Publish(WirehomeDictionary properties)
        {
            if (properties == null) throw new ArgumentNullException(nameof(properties));

            var busMessage = new BusMessage
            {
                Uid = Guid.NewGuid(),
                EnqueuedTimestamp = DateTime.UtcNow,
                Properties = properties
            };

            lock (_messages)
            {
                _messages.Add(busMessage);
                _inboundCounter.Increment();

                _history.Enqueue(busMessage);
                if (_history.Count > 100)
                {
                    _history.Dequeue();
                }
            }

            //_logger.Log(LogLevel.Debug, "Published message '{0}' as '{1}'.", message.ToExtendedString(), busMessage.Uid);
        }

        public List<BusMessage> GetHistory()
        {
            lock (_messages)
            {
                return new List<BusMessage>(_history);
            }
        }

        public string Subscribe(string type, Action<WirehomeDictionary> callback)
        {
            return Subscribe(new WirehomeDictionary().WithType(type), callback);
        }

        public string Subscribe(WirehomeDictionary filter, Action<WirehomeDictionary> callback)
        {
            if (filter == null) throw new ArgumentNullException(nameof(filter));
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            var uid = Guid.NewGuid().ToString("D");
            Subscribe(uid, filter, callback);
            return uid;
        }

        public void Subscribe(string uid, WirehomeDictionary filter, Action<WirehomeDictionary> callback)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));
            if (filter == null) throw new ArgumentNullException(nameof(filter));
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            
            var subscription = new MessageBusSubscriber(uid, filter, callback);
            lock (_subscribers)
            {
                _subscribers.Add(uid, subscription);
            }

            _logger.Log(LogLevel.Debug, $"Registered subscriber '{uid}' with filter '{filter.ToExtendedString()}'.");
        }

        public void Unsubscribe(string subscriptionUid)
        {
            lock (_subscribers)
            {
                _subscribers.Remove(subscriptionUid);
            }

            _logger.Log(LogLevel.Debug, $"Removed subscriber '{subscriptionUid}'.");
        }

        public void RegisterInterceptor(string uid, Func<WirehomeDictionary, WirehomeDictionary> interceptor)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));
            if (interceptor == null) throw new ArgumentNullException(nameof(interceptor));

            lock (_messageInterceptors)
            {
                _messageInterceptors[uid] = new MessageBusInterceptor(uid, interceptor);
            }

            _logger.Log(LogLevel.Information, $"Registered interceptor '{uid}'.");
        }

        private void TryDispatchMessages(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    TryDispatchNextMessage(cancellationToken);
                    _outboundCounter.Increment();
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _logger.Log(LogLevel.Error, exception, "Error while dispatching messages.");
            }
        }

        private void TryDispatchNextMessage(CancellationToken cancellationToken)
        {
            try
            {
                var message = _messages.Take(cancellationToken);
                if (message == null || cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                lock (_messageInterceptors)
                {
                    foreach (var interceptor in _messageInterceptors.Values)
                    {
                        message.Properties = interceptor.Intercept(message.Properties);
                        if (message.Properties == null)
                        {
                            // The message was deleted by the interceptor.
                            _logger.Log(LogLevel.Debug, "Message '{0}' was deleted by interceptor '{1}'.", message.Uid, interceptor.Uid);
                            return;
                        }
                    }
                }

                var affectedSubscribers = new List<MessageBusSubscriber>();
                lock (_subscribers)
                {
                    foreach (var subscription in _subscribers.Values)
                    {
                        if (subscription.IsFilterMatch(message.Properties))
                        {
                            affectedSubscribers.Add(subscription);
                        }
                    }
                }

                foreach (var subscriber in affectedSubscribers)
                {
                    TryNotifySubscriber(subscriber, message.Properties);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _logger.Log(LogLevel.Error, exception, "Error while dispatching message.");
            }
        }

        private void TryNotifySubscriber(MessageBusSubscriber subscriber, WirehomeDictionary message)
        {
            try
            {
                subscriber.Notify(message);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _logger.Log(LogLevel.Error, exception, $"Error while notifying subscriber '{subscriber.Uid}'.");
            }
        }
    }
}
