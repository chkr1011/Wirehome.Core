using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Server;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.Python;
using Wirehome.Core.Python.Proxies;
using Wirehome.Core.Storage;
using Wirehome.Core.System;

namespace Wirehome.Core.Hardware.MQTT
{
    public partial class MqttService
    {
        private readonly BlockingCollection<MqttApplicationMessageReceivedEventArgs> _messages = new BlockingCollection<MqttApplicationMessageReceivedEventArgs>();
        private readonly Dictionary<string, IManagedMqttClient> _importClients = new Dictionary<string, IManagedMqttClient>();
        private readonly Dictionary<string, MqttServiceSubscriber> _subscribers = new Dictionary<string, MqttServiceSubscriber>();
        private readonly OperationsPerSecondCounter _inboundCounter = new OperationsPerSecondCounter();
        private readonly OperationsPerSecondCounter _outboundCounter = new OperationsPerSecondCounter();

        private readonly SystemService _systemService;
        private readonly StorageService _storageService;

        private readonly IMqttServer _mqttServer;
        private readonly ILogger _logger;

        public MqttService(
            PythonEngineService pythonEngineService, 
            SystemService systemService, 
            StorageService storageService, 
            SystemStatusService systemStatusService,
            ILoggerFactory loggerFactory)
        {
            _systemService = systemService ?? throw new ArgumentNullException(nameof(systemService));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));

            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<MqttService>();

            _mqttServer = new MqttFactory().CreateMqttServer(new LoggerAdapter(_logger));
            _mqttServer.ApplicationMessageReceived += OnApplicationMessageReceived;

            if (pythonEngineService == null) throw new ArgumentNullException(nameof(pythonEngineService));
            pythonEngineService.RegisterSingletonProxy(new MqttPythonProxy(this));

            systemStatusService.Set("mqtt_service.subscribers_count", () => _subscribers.Count);
            systemStatusService.Set("mqtt_service.inbound_rate", () => _inboundCounter.Count);
            systemStatusService.Set("mqtt_service.outbound_rate", () => _outboundCounter.Count);
        }

        public void Start()
        {
            _logger.Log(LogLevel.Debug, "Starting...");

            var options = new MqttServerOptionsBuilder()
                .WithStorage(new MqttServerStorage(_storageService))
                .Build();

            _mqttServer.StartAsync(options).GetAwaiter().GetResult();
            Task.Factory.StartNew(() => TryProcessMessages(_systemService.CancellationToken), _systemService.CancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            
            _logger.Log(LogLevel.Debug, "Started.");
        }

        public void EnableTopicImport(string uid, MqttImportTopicParameters parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            var options = new ManagedMqttClientOptionsBuilder();
            options.WithClientOptions(o => o.WithTcpServer(parameters.Server, parameters.Port));

            var client = new MqttFactory().CreateManagedMqttClient(new LoggerAdapter(_logger));
            client.SubscribeAsync(parameters.Topic, parameters.QualityOfServiceLevel).GetAwaiter().GetResult();
            client.ApplicationMessageReceived += OnImportedApplicationMessageReceived;
            client.StartAsync(options.Build()).GetAwaiter().GetResult();

            lock (_importClients)
            {
                _importClients[uid] = client;
            }

            _logger.Log(LogLevel.Information, "Started import client '{0}' for topic '{1}' from server '{2}'.", uid, parameters.Topic, parameters.Server);
        }

        public void DisableTopicImport(string uid)
        {
            lock (_importClients)
            {
                if (_importClients.TryGetValue(uid, out var client))
                {
                    client.StopAsync().GetAwaiter().GetResult();
                    client.Dispose();

                    _logger.Log(LogLevel.Information, "Stopped import client '{0}'.");
                }
            }
        }

        public List<string> GetSubscriptions()
        {
            lock (_subscribers)
            {
                return _subscribers.Select(s => s.Key).ToList();
            }
        }

        public void DeleteRetainedMessages()
        {
            _mqttServer.ClearRetainedMessagesAsync().GetAwaiter().GetResult();
        }

        public void Publish(MqttPublishParameters parameters)
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(parameters.Topic)
                .WithPayload(parameters.Payload)
                .WithQualityOfServiceLevel(parameters.QualityOfServiceLevel)
                .WithRetainFlag(parameters.Retain)
                .Build();

            _mqttServer.PublishAsync(message).GetAwaiter().GetResult();
            _outboundCounter.Increment();

            _logger.Log(LogLevel.Trace, $"Published MQTT topic '{parameters.Topic}.");
        }

        public void Subscribe(string uid, string topicFilter, Action<MqttApplicationMessageReceivedEventArgs> callback)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));
            if (topicFilter == null) throw new ArgumentNullException(nameof(topicFilter));
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            lock (_subscribers)
            {
                _subscribers[uid] = new MqttServiceSubscriber(uid, topicFilter, callback);
            }
        }

        public void Unsubscribe(string uid)
        {
            lock (_subscribers)
            {
                _subscribers.Remove(uid);
            }
        }

        private void TryProcessMessages(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    TryProcessNextMessage(cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _logger.Log(LogLevel.Error, exception, "Error while processing MQTT messages.");
            }
        }

        private void TryProcessNextMessage(CancellationToken cancellationToken)
        {
            MqttApplicationMessageReceivedEventArgs message = null;
            try
            {
                message = _messages.Take(cancellationToken);
                if (message == null || cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                var affectedSubscribers = new List<MqttServiceSubscriber>();
                lock (_subscribers)
                {
                    foreach (var subscription in _subscribers.Values)
                    {
                        if (subscription.IsFilterMatch(message.ApplicationMessage))
                        {
                            affectedSubscribers.Add(subscription);
                        }
                    }
                }

                foreach (var subscriber in affectedSubscribers)
                {
                    TryNotifySubscriber(subscriber, message);
                }

                _outboundCounter.Increment();
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _logger.Log(LogLevel.Error, exception, $"Error while processing MQTT message with topic '{message?.ApplicationMessage?.Topic}'.");
            }
        }

        private void TryNotifySubscriber(MqttServiceSubscriber subscriber, MqttApplicationMessageReceivedEventArgs message)
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

        private void OnApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            _messages.Add(eventArgs);
        }

        private void OnImportedApplicationMessageReceived(object sender, MqttApplicationMessageReceivedEventArgs e)
        {
            _mqttServer.PublishAsync(e.ApplicationMessage).GetAwaiter().GetResult();
        }
    }
}
