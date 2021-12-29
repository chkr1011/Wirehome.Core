using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Adapter;
using MQTTnet.AspNetCore;
using MQTTnet.Client.Receiving;
using MQTTnet.Diagnostics.Logger;
using MQTTnet.Implementations;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MQTTnet.Server.Status;
using Wirehome.Core.Contracts;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.Extensions;
using Wirehome.Core.Storage;
using Wirehome.Core.System;

namespace Wirehome.Core.Hardware.MQTT
{
    public sealed class MqttService : WirehomeCoreService
    {
        readonly OperationsPerSecondCounter _inboundCounter;

        readonly BlockingCollection<MqttApplicationMessageReceivedEventArgs> _incomingMessages = new BlockingCollection<MqttApplicationMessageReceivedEventArgs>();

        readonly ILogger _logger;
        readonly OperationsPerSecondCounter _outboundCounter;
        readonly StorageService _storageService;
        readonly Dictionary<string, MqttSubscriber> _subscribers = new Dictionary<string, MqttSubscriber>();

        readonly SystemCancellationToken _systemCancellationToken;
        readonly MqttTopicImportManager _topicImportManager;
        IMqttServer _mqttServer;

        MqttServiceOptions _options;

        MqttWebSocketServerAdapter _webSocketServerAdapter;

        public MqttService(SystemCancellationToken systemCancellationToken,
            DiagnosticsService diagnosticsService,
            StorageService storageService,
            SystemStatusService systemStatusService,
            ILogger<MqttService> logger)
        {
            _systemCancellationToken = systemCancellationToken ?? throw new ArgumentNullException(nameof(systemCancellationToken));
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            if (diagnosticsService == null)
            {
                throw new ArgumentNullException(nameof(diagnosticsService));
            }

            _inboundCounter = diagnosticsService.CreateOperationsPerSecondCounter("mqtt.inbound_rate");
            _outboundCounter = diagnosticsService.CreateOperationsPerSecondCounter("mqtt.outbound_rate");

            if (systemStatusService is null)
            {
                throw new ArgumentNullException(nameof(systemStatusService));
            }

            systemStatusService.Set("mqtt.subscribers_count", () => _subscribers.Count);
            systemStatusService.Set("mqtt.incoming_messages_count", () => _incomingMessages.Count);
            systemStatusService.Set("mqtt.inbound_rate", () => _inboundCounter.Count);
            systemStatusService.Set("mqtt.outbound_rate", () => _outboundCounter.Count);
            systemStatusService.Set("mqtt.connected_clients_count", () => _mqttServer.GetClientStatusAsync().GetAwaiter().GetResult().Count);

            _topicImportManager = new MqttTopicImportManager(this, _logger);
        }

        public bool IsLowLevelMqttLoggingEnabled { get; set; }

        public Task DeleteRetainedMessagesAsync()
        {
            return _mqttServer.ClearRetainedApplicationMessagesAsync();
        }

        public Task<IList<IMqttClientStatus>> GetClientsAsync()
        {
            return _mqttServer.GetClientStatusAsync();
        }

        public Task<IList<MqttApplicationMessage>> GetRetainedMessagesAsync()
        {
            return _mqttServer.GetRetainedApplicationMessagesAsync();
        }

        public Task<IList<IMqttSessionStatus>> GetSessionsAsync()
        {
            return _mqttServer.GetSessionStatusAsync();
        }

        public List<MqttSubscriber> GetSubscribers()
        {
            lock (_subscribers)
            {
                return _subscribers.Values.ToList();
            }
        }

        public List<string> GetTopicImportUids()
        {
            return _topicImportManager.GetTopicImportUids();
        }

        public void Publish(MqttPublishParameters parameters)
        {
            if (parameters is null)
            {
                throw new ArgumentNullException(nameof(parameters));
            }

            var message = new MqttApplicationMessageBuilder().WithTopic(parameters.Topic).WithPayload(parameters.Payload)
                .WithQualityOfServiceLevel(parameters.QualityOfServiceLevel).WithRetainFlag(parameters.Retain).Build();

            _mqttServer.PublishAsync(message).GetAwaiter().GetResult();
            _outboundCounter.Increment();

            _logger.Log(LogLevel.Trace, $"Published MQTT topic '{parameters.Topic}'.");
        }

        public Task RunWebSocketConnectionAsync(WebSocket webSocket, HttpContext context)
        {
            return _webSocketServerAdapter.RunWebSocketConnectionAsync(webSocket, context);
        }

        public Task<string> StartTopicImport(string uid, MqttImportTopicParameters parameters)
        {
            return _topicImportManager.StartTopicImport(uid, parameters);
        }

        public Task StopTopicImport(string uid)
        {
            return _topicImportManager.StopTopicImport(uid);
        }

        public string Subscribe(string uid, string topicFilter, Action<MqttApplicationMessageReceivedEventArgs> callback)
        {
            if (topicFilter == null)
            {
                throw new ArgumentNullException(nameof(topicFilter));
            }

            if (callback == null)
            {
                throw new ArgumentNullException(nameof(callback));
            }

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
                _incomingMessages.Add(new MqttApplicationMessageReceivedEventArgs(null, retainedMessage, new MqttPublishPacket(), (args, token) => Task.CompletedTask));
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

        protected override void OnStart()
        {
            _storageService.SafeReadSerializedValue(out _options, DefaultDirectoryNames.Configuration, MqttServiceOptions.Filename);

            IsLowLevelMqttLoggingEnabled = _options.EnableLogging;

            var mqttFactory = new MqttFactory();

            IMqttNetLogger mqttNetLogger;
            if (IsLowLevelMqttLoggingEnabled)
            {
                mqttNetLogger = new LoggerAdapter(_logger);
            }
            else
            {
                mqttNetLogger = new MqttNetNullLogger();
            }

            _webSocketServerAdapter = new MqttWebSocketServerAdapter(mqttNetLogger);

            var serverAdapters = new List<IMqttServerAdapter>
            {
                new MqttTcpServerAdapter(mqttNetLogger),
                _webSocketServerAdapter
            };

            _mqttServer = mqttFactory.CreateMqttServer(serverAdapters, mqttNetLogger);
            _mqttServer.UseApplicationMessageReceivedHandler(new MqttApplicationMessageReceivedHandlerDelegate(e => OnApplicationMessageReceived(e)));

            var serverOptions = new MqttServerOptionsBuilder().WithDefaultEndpointPort(_options.ServerPort).WithConnectionValidator(ValidateClientConnection)
                .WithPersistentSessions();

            if (_options.PersistRetainedMessages)
            {
                var storage = new MqttServerStorage(_storageService, _systemCancellationToken, _logger);
                storage.Start();
                serverOptions.WithStorage(storage);
            }

            _mqttServer.StartAsync(serverOptions.Build()).GetAwaiter().GetResult();

            _systemCancellationToken.Token.Register(() => { _mqttServer.StopAsync().GetAwaiter().GetResult(); });

            ParallelTask.StartLongRunning(ProcessIncomingMqttMessages, _systemCancellationToken.Token, _logger);
        }

        void OnApplicationMessageReceived(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            _inboundCounter.Increment();
            _incomingMessages.Add(eventArgs);
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

        Task ValidateClientConnection(MqttConnectionValidatorContext context)
        {
            context.ReasonCode = MqttConnectReasonCode.Success;

            if (_options.BlockedClients == null)
            {
                return Task.CompletedTask;
            }

            if (_options.BlockedClients.Contains(context.ClientId ?? string.Empty))
            {
                context.ReasonCode = MqttConnectReasonCode.Banned;
            }

            return Task.CompletedTask;
        }
    }
}