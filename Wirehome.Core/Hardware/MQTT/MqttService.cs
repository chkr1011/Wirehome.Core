using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client.Receiving;
using MQTTnet.Server;
using MQTTnet.Server.Status;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wirehome.Core.Contracts;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.Storage;
using Wirehome.Core.System;

namespace Wirehome.Core.Hardware.MQTT
{
    public sealed class MqttService : IService, IDisposable
    {
        readonly BlockingCollection<MqttApplicationMessageReceivedEventArgs> _incomingMessages = new BlockingCollection<MqttApplicationMessageReceivedEventArgs>();
        readonly Dictionary<string, MqttSubscriber> _subscribers = new Dictionary<string, MqttSubscriber>();
        readonly OperationsPerSecondCounter _inboundCounter;
        readonly OperationsPerSecondCounter _outboundCounter;

        readonly SystemCancellationToken _systemCancellationToken;
        readonly StorageService _storageService;

        readonly MqttTopicImportManager _topicImportManager;

        readonly ILogger _logger;


        MqttServiceOptions _options;
        Thread _workerThread;

        IMqttServer _mqttServer;

        public MqttService(
            SystemCancellationToken systemCancellationToken,
            DiagnosticsService diagnosticsService,
            StorageService storageService,
            SystemStatusService systemStatusService,
            ILogger<MqttService> logger)
        {
            _systemCancellationToken = systemCancellationToken ?? throw new ArgumentNullException(nameof(systemCancellationToken));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (diagnosticsService == null) throw new ArgumentNullException(nameof(diagnosticsService));
            _inboundCounter = diagnosticsService.CreateOperationsPerSecondCounter("mqtt.inbound_rate");
            _outboundCounter = diagnosticsService.CreateOperationsPerSecondCounter("mqtt.outbound_rate");

            if (systemStatusService is null) throw new ArgumentNullException(nameof(systemStatusService));
            systemStatusService.Set("mqtt.subscribers_count", () => _subscribers.Count);
            systemStatusService.Set("mqtt.incoming_messages_count", () => _incomingMessages.Count);
            systemStatusService.Set("mqtt.inbound_rate", () => _inboundCounter.Count);
            systemStatusService.Set("mqtt.outbound_rate", () => _outboundCounter.Count);
            systemStatusService.Set("mqtt.connected_clients_count", () => _mqttServer.GetClientStatusAsync().GetAwaiter().GetResult().Count);

            _topicImportManager = new MqttTopicImportManager(this, _logger);
        }

        public bool IsLowLevelMqttLoggingEnabled { get; set; }

        public void Start()
        {
            _storageService.TryReadOrCreate(out _options, DefaultDirectoryNames.Configuration, MqttServiceOptions.Filename);

            var mqttFactory = new MqttFactory();

            IsLowLevelMqttLoggingEnabled = _options.EnableLogging;
            if (IsLowLevelMqttLoggingEnabled)
            {
                _mqttServer = mqttFactory.CreateMqttServer(new LoggerAdapter(_logger));
            }
            else
            {
                _mqttServer = mqttFactory.CreateMqttServer();
            }

            _mqttServer.UseApplicationMessageReceivedHandler(new MqttApplicationMessageReceivedHandlerDelegate(e => OnApplicationMessageReceived(e)));

            var serverOptions = new MqttServerOptionsBuilder()
                .WithDefaultEndpointPort(_options.ServerPort)
                .WithConnectionValidator(ValidateClientConnection)
                .WithPersistentSessions();

            if (_options.PersistRetainedMessages)
            {
                var storage = new MqttServerStorage(_storageService, _systemCancellationToken, _logger);
                storage.Start();
                serverOptions.WithStorage(storage);
            }

            _mqttServer.StartAsync(serverOptions.Build()).GetAwaiter().GetResult();

            _workerThread = new Thread(ProcessIncomingMqttMessages)
            {
                Name = nameof(MqttService),
                IsBackground = true
            };

            _workerThread.Start();
        }

        public List<string> GetTopicImportUids()
        {
            return _topicImportManager.GetTopicImportUids();
        }

        public Task<string> StartTopicImport(string uid, MqttImportTopicParameters parameters)
        {
            return _topicImportManager.StartTopicImport(uid, parameters);
        }

        public Task StopTopicImport(string uid)
        {
            return _topicImportManager.StopTopicImport(uid);
        }

        public List<MqttSubscriber> GetSubscribers()
        {
            lock (_subscribers)
            {
                return _subscribers.Values.ToList();
            }
        }

        public Task<IList<MqttApplicationMessage>> GetRetainedMessagesAsync()
        {
            return _mqttServer.GetRetainedApplicationMessagesAsync();
        }

        public Task DeleteRetainedMessagesAsync()
        {
            return _mqttServer.ClearRetainedApplicationMessagesAsync();
        }

        public void Publish(MqttPublishParameters parameters)
        {
            if (parameters is null) throw new ArgumentNullException(nameof(parameters));

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(parameters.Topic)
                .WithPayload(parameters.Payload)
                .WithQualityOfServiceLevel(parameters.QualityOfServiceLevel)
                .WithRetainFlag(parameters.Retain)
                .Build();

            _mqttServer.PublishAsync(message).GetAwaiter().GetResult();
            _outboundCounter.Increment();

            _logger.Log(LogLevel.Trace, $"Published MQTT topic '{parameters.Topic}'.");
        }

        public string Subscribe(string uid, string topicFilter, Action<MqttApplicationMessageReceivedEventArgs> callback)
        {
            if (topicFilter == null) throw new ArgumentNullException(nameof(topicFilter));
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            if (string.IsNullOrEmpty(uid))
            {
                uid = Guid.NewGuid().ToString("D");
            }

            lock (_subscribers)
            {
                _subscribers[uid] = new MqttSubscriber(uid, topicFilter, callback);
            }

            // Enqueue all retained messages to match the expected MQTT behavior.
            // Here we have no client per subscription. So we need to adopt some
            // features here manually.
            var retainedMessages = _mqttServer.GetRetainedApplicationMessagesAsync().GetAwaiter().GetResult();
            foreach (var retainedMessage in retainedMessages)
            {
                _incomingMessages.Add(new MqttApplicationMessageReceivedEventArgs(null, retainedMessage));
            }

            return uid;
        }

        public void Unsubscribe(string uid)
        {
            lock (_subscribers)
            {
                _subscribers.Remove(uid);
            }
        }

        public Task<IList<IMqttClientStatus>> GetClientsAsync()
        {
            return _mqttServer.GetClientStatusAsync();
        }

        public Task<IList<IMqttSessionStatus>> GetSessionsAsync()
        {
            return _mqttServer.GetSessionStatusAsync();
        }

        public void Dispose()
        {
            _incomingMessages.Dispose();
            _topicImportManager.Dispose();
        }

        void ProcessIncomingMqttMessages()
        {
            var cancellationToken = _systemCancellationToken.Token;

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

                    var affectedSubscribers = new List<MqttSubscriber>();
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
                catch (ThreadAbortException)
                {
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

        void TryNotifySubscriber(MqttSubscriber subscriber, MqttApplicationMessageReceivedEventArgs message)
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

        void OnApplicationMessageReceived(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            _inboundCounter.Increment();
            _incomingMessages.Add(eventArgs);
        }

        void ValidateClientConnection(MqttConnectionValidatorContext context)
        {
            context.ReasonCode = MQTTnet.Protocol.MqttConnectReasonCode.Success;

            if (_options.BlockedClients == null)
            {
                return;
            }

            if (_options.BlockedClients.Contains(context.ClientId ?? string.Empty))
            {
                context.ReasonCode = MQTTnet.Protocol.MqttConnectReasonCode.Banned;
            }
        }
    }
}
