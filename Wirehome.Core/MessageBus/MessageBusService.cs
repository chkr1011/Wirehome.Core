using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.Model;
using Wirehome.Core.Python;
using Wirehome.Core.Python.Proxies;
using Wirehome.Core.System;

namespace Wirehome.Core.MessageBus
{
    public class MessageBusService
    {
        private readonly BlockingCollection<MessageBusMessage> _messageQueue = new BlockingCollection<MessageBusMessage>();
        private readonly LinkedList<MessageBusMessage> _history = new LinkedList<MessageBusMessage>();
        private readonly ConcurrentDictionary<string, MessageBusSubscriber> _subscribers = new ConcurrentDictionary<string, MessageBusSubscriber>();

        private readonly SystemService _systemService;

        private readonly OperationsPerSecondCounter _inboundCounter;
        private readonly OperationsPerSecondCounter _processingRateCounter;
        
        private readonly ILogger _logger;

        public MessageBusService(
            PythonEngineService pythonEngineService, 
            SystemStatusService systemStatusService, 
            DiagnosticsService diagnosticsService,
            SystemService systemService,
            ILoggerFactory loggerFactory)
        {
            _systemService = systemService ?? throw new ArgumentNullException(nameof(systemService));

            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<MessageBusService>();

            if (pythonEngineService == null) throw new ArgumentNullException(nameof(pythonEngineService));
            pythonEngineService.RegisterSingletonProxy(new MessageBusPythonProxy(this));

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
                () => TryDispatchMessages(_systemService.CancellationToken),
                _systemService.CancellationToken,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);

            for (var i = 0; i < 3; i++)
            {
                Task.Factory.StartNew(
                    () => TryProcessMessages(_systemService.CancellationToken),
                    _systemService.CancellationToken,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default);
            }
        }

        public void Publish(WirehomeDictionary message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var busMessage = new MessageBusMessage
            {
                Uid = Guid.NewGuid(),
                EnqueuedTimestamp = DateTime.UtcNow,
                CarriedMessage = message
            };

            _messageQueue.Add(busMessage);
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

        public string Subscribe(string uid, WirehomeDictionary filter, Action<WirehomeDictionary> callback)
        {
            if (filter == null) throw new ArgumentNullException(nameof(filter));
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            if (string.IsNullOrEmpty(uid))
            {
                uid = Guid.NewGuid().ToString("D");
            }

            var subscriber = new MessageBusSubscriber(uid, filter, callback);
            _subscribers[uid] = subscriber;

            return uid;
        }

        public void Unsubscribe(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            _subscribers.TryRemove(uid, out _);
        }

        private void TryDispatchMessages(CancellationToken cancellationToken)
        {
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
                        _history.AddLast(message);
                        if (_history.Count > 100)
                        {
                            _history.RemoveFirst();
                        }
                    }

                    foreach (var subscriber in _subscribers.Values)
                    {
                        subscriber.EnqueueMessage(message.CarriedMessage);
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception exception)
                {
                    _logger.Log(LogLevel.Error, exception, "Error while dispatching messages.");
                    Thread.Sleep(5000);
                }
            }
        }

        private void TryProcessMessages(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    foreach (var subscriber in _subscribers.Values)
                    {
                        if (subscriber.ProcessNextMessage())
                        {
                            _processingRateCounter.Increment();
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception exception)
                {
                    _logger.Log(LogLevel.Error, exception, "Error while processing messages.");
                    Thread.Sleep(5000);
                }
                finally
                {
                    Thread.Sleep(100);
                }
            }
        }
    }
}
