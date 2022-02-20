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
using MQTTnet.Implementations;
using MQTTnet.Protocol;
using MQTTnet.Server;
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

        readonly BlockingCollection<IncomingMqttMessage> _incomingMessages = new();

        readonly ILogger _logger;
        readonly OperationsPerSecondCounter _outboundCounter;
        readonly StorageService _storageService;
        readonly Dictionary<string, MqttSubscriber> _subscribers = new();

        readonly SystemCancellationToken _systemCancellationToken;
        readonly MqttTopicImportManager _topicImportManager;
        MqttServer _mqttServer;

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
            systemStatusService.Set("mqtt.connected_clients_count", () => _mqttServer.GetClientsAsync().GetAwaiter().GetResult().Count);

            _topicImportManager = new MqttTopicImportManager(this, _logger);
        }

        public bool IsLowLevelMqttLoggingEnabled { get; set; }

        public Task DeleteRetainedMessagesAsync()
        {
            return _mqttServer.DeleteRetainedMessagesAsync();
        }

        public Task<IList<MqttClientStatus>> GetClientsAsync()
        {
            return _mqttServer.GetClientsAsync();
        }

        public Task<IList<MqttApplicationMessage>> GetRetainedMessagesAsync()
        {
            return _mqttServer.GetRetainedMessagesAsync();
        }

        public Task<IList<MqttSessionStatus>> GetSessionsAsync()
        {
            return _mqttServer.GetSessionsAsync();
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

            var message = new MqttApplicationMessage
            {
                Topic = parameters.Topic,
                Payload = parameters.Payload,
                QualityOfServiceLevel = parameters.QualityOfServiceLevel,
                Retain = parameters.Retain
            };

            _mqttServer.InjectApplicationMessage(new InjectedMqttApplicationMessage(message)).GetAwaiter().GetResult();
            
            _outboundCounter.Increment();
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

        public string Subscribe(string uid, string topicFilter, Action<IncomingMqttMessage> callback)
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
            var retainedMessages = _mqttServer.GetRetainedMessagesAsync().GetAwaiter().GetResult();
            foreach (var retainedMessage in retainedMessages)
            {
                _incomingMessages.Add(new IncomingMqttMessage
                {
                    ApplicationMessage = retainedMessage
                });
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
            
            MqttFactory mqttFactory;
            if (_options.EnableLogging)
            {
                IsLowLevelMqttLoggingEnabled = true;
                mqttFactory = new MqttFactory(new LoggerAdapter(_logger));
            }
            else
            {
                mqttFactory = new MqttFactory();
            }

            _webSocketServerAdapter = new MqttWebSocketServerAdapter();

            var serverAdapters = new List<IMqttServerAdapter>
            {
                new MqttTcpServerAdapter(),
                _webSocketServerAdapter
            };

            var serverOptions = new MqttServerOptionsBuilder()
                .WithDefaultEndpoint()
                .WithDefaultEndpointPort(_options.ServerPort)
                .WithPersistentSessions()
                .Build();
            
            _mqttServer = mqttFactory.CreateMqttServer(serverOptions, serverAdapters);
            
            _mqttServer.InterceptingPublishAsync += e =>
            {
                OnApplicationMessageReceived(e);
                return Task.CompletedTask;
            };

            _mqttServer.ValidatingConnectionAsync += e =>
            {
                ValidateClientConnection(e);
                return Task.CompletedTask;
            };

            if (_options.PersistRetainedMessages)
            {
                var storage = new MqttServerStorage(_storageService, _systemCancellationToken, _logger);
                storage.Start();

                _mqttServer.LoadingRetainedMessageAsync += async e =>
                {
                    var retainedMessages = await storage.LoadRetainedMessagesAsync();
                    e.LoadedRetainedMessages.AddRange(retainedMessages);
                };

                _mqttServer.RetainedMessageChangedAsync += async e =>
                {
                    await storage.SaveRetainedMessagesAsync(e.StoredRetainedMessages);
                };
            }

            _mqttServer.StartAsync().GetAwaiter().GetResult();

            _systemCancellationToken.Token.Register(() => { _mqttServer.StopAsync().GetAwaiter().GetResult(); });

            ParallelTask.StartLongRunning(ProcessIncomingMqttMessages, _systemCancellationToken.Token, _logger);
        }

        void OnApplicationMessageReceived(InterceptingPublishEventArgs eventArgs)
        {
            _inboundCounter.Increment();
            
            _incomingMessages.Add(new IncomingMqttMessage
            {
                ClientId = eventArgs.ClientId,
                ApplicationMessage = eventArgs.ApplicationMessage
            });
        }

        void ProcessIncomingMqttMessages()
        {
            var cancellationToken = _systemCancellationToken.Token;

            while (!cancellationToken.IsCancellationRequested)
            {
                IncomingMqttMessage incomingMqttMessage = null;
                try
                {
                    incomingMqttMessage = _incomingMessages.Take(cancellationToken);
                    if (incomingMqttMessage == null || cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }

                    var affectedSubscribers = new List<MqttSubscriber>();
                    lock (_subscribers)
                    {
                        foreach (var subscriber in _subscribers.Values)
                        {
                            if (subscriber.IsFilterMatch(incomingMqttMessage.ApplicationMessage.Topic))
                            {
                                affectedSubscribers.Add(subscriber);
                            }
                        }
                    }

                    foreach (var subscriber in affectedSubscribers)
                    {
                        TryNotifySubscriber(subscriber, incomingMqttMessage);
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
                    _logger.Log(LogLevel.Error, exception, $"Error while processing MQTT message with topic '{incomingMqttMessage?.ApplicationMessage?.Topic}'.");
                }
            }
        }

        void TryNotifySubscriber(MqttSubscriber subscriber, IncomingMqttMessage incomingMqttMessage)
        {
            try
            {
                subscriber.Notify(incomingMqttMessage);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _logger.Log(LogLevel.Error, exception, $"Error while notifying subscriber '{subscriber.Uid}'.");
            }
        }

        void ValidateClientConnection(ValidatingConnectionEventArgs eventArgs)
        {
            eventArgs.ReasonCode = MqttConnectReasonCode.Success;

            if (_options.BlockedClients == null)
            {
                return;
            }

            if (_options.BlockedClients.Contains(eventArgs.ClientId ?? string.Empty))
            {
                eventArgs.ReasonCode = MqttConnectReasonCode.Banned;
            }
        }
    }
}