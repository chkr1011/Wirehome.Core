using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet;
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
        private readonly BlockingCollection<MqttApplicationMessageReceivedEventArgs> _incomingMessages = new BlockingCollection<MqttApplicationMessageReceivedEventArgs>();
        private readonly Dictionary<string, MqttTopicImporter> _importers = new Dictionary<string, MqttTopicImporter>();
        private readonly Dictionary<string, MqttServiceSubscriber> _subscribers = new Dictionary<string, MqttServiceSubscriber>();
        private readonly OperationsPerSecondCounter _inboundCounter;
        private readonly OperationsPerSecondCounter _outboundCounter;

        private readonly SystemService _systemService;
        private readonly StorageService _storageService;

        private readonly IMqttServer _mqttServer;
        private readonly ILogger _logger;

        public MqttService(
            PythonEngineService pythonEngineService,
            SystemService systemService,
            DiagnosticsService diagnosticsService,
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

            if (diagnosticsService == null) throw new ArgumentNullException(nameof(diagnosticsService));
            _inboundCounter = diagnosticsService.CreateOperationsPerSecondCounter("mqtt.inbound_rate");
            _outboundCounter = diagnosticsService.CreateOperationsPerSecondCounter("mqtt.outbound_rate");

            systemStatusService.Set("mqtt.subscribers_count", () => _subscribers.Count);
            systemStatusService.Set("mqtt.inbound_rate", () => _inboundCounter.Count);
            systemStatusService.Set("mqtt.outbound_rate", () => _outboundCounter.Count);
        }

        public void Start()
        {
            if (!_storageService.TryRead(out MqttServiceSettings settings, "MqttServiceSettings.json"))
            {
                settings = new MqttServiceSettings();
            }

            var options = new MqttServerOptionsBuilder()
                .WithStorage(new MqttServerStorage(_storageService))
                .WithDefaultEndpointPort(settings.ServerPort)
                .WithPersistentSessions()
                .Build();

            _mqttServer.StartAsync(options).GetAwaiter().GetResult();
            Task.Factory.StartNew(() => TryProcessIncomingMessages(_systemService.CancellationToken), _systemService.CancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            _logger.Log(LogLevel.Debug, "Started.");
        }

        public string StartTopicImport(string uid, MqttImportTopicParameters parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            if (string.IsNullOrEmpty(uid))
            {
                uid = Guid.NewGuid().ToString("D");
            }

            var importer = new MqttTopicImporter(parameters, this, _logger);
            importer.Start();

            lock (_importers)
            {
                if (_importers.TryGetValue(uid, out var existingImporter))
                {
                    existingImporter.Stop();
                }

                _importers[uid] = importer;
            }

            _logger.Log(LogLevel.Information, "Started importer '{0}' for topic '{1}' from server '{2}'.", uid, parameters.Topic, parameters.Server);
            return uid;
        }

        public void StopTopicImport(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            lock (_importers)
            {
                if (_importers.TryGetValue(uid, out var importer))
                {
                    importer.Stop();
                    _logger.Log(LogLevel.Information, "Stopped importer '{0}'.");
                }

                _importers.Remove(uid);
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

        private void TryProcessIncomingMessages(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                MqttApplicationMessageReceivedEventArgs message = null;
                try
                {
                    message = _incomingMessages.Take(cancellationToken);
                    if (message == null || cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    var affectedSubscribers = new List<MqttServiceSubscriber>();
                    lock (_subscribers)
                    {
                        foreach (var subscriber in _subscribers.Values)
                        {
                            if (subscriber.IsFilterMatch(message.ApplicationMessage.Topic))
                            {
                                affectedSubscribers.Add(subscriber);
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
            _inboundCounter.Increment();
            _incomingMessages.Add(eventArgs);
        }
    }
}
