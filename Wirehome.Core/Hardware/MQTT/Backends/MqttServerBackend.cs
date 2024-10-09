using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.AspNetCore;
using MQTTnet.Protocol;
using MQTTnet.Server;
using MQTTnet.Server.Internal.Adapter;
using Wirehome.Core.Hardware.MQTT.Storage;
using Wirehome.Core.Storage;
using Wirehome.Core.System;

namespace Wirehome.Core.Hardware.MQTT.Backends;

public sealed class MqttServerBackend : IMqttBackend
{
    readonly ILogger _logger;
    readonly MqttServiceOptions _options;
    readonly StorageService _storageService;
    readonly SystemCancellationToken _systemCancellationToken;

    MqttServer _mqttServer;
    MqttWebSocketServerAdapter _webSocketServerAdapter;

    public MqttServerBackend(MqttServiceOptions options, StorageService storageService, SystemCancellationToken systemCancellationToken, ILogger logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _systemCancellationToken = systemCancellationToken ?? throw new ArgumentNullException(nameof(systemCancellationToken));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public event EventHandler<InterceptingPublishEventArgs> MessageReceived;

    public Task DeleteRetainedMessagesAsync()
    {
        return _mqttServer.DeleteRetainedMessagesAsync();
    }

    public Task<IList<MqttClientStatus>> GetClientsAsync()
    {
        return _mqttServer.GetClientsAsync();
    }

    public async Task<int> GetConnectedClientsCount()
    {
        return (await _mqttServer.GetClientsAsync()).Count;
    }

    public Task<IList<MqttApplicationMessage>> GetRetainedMessagesAsync()
    {
        return _mqttServer.GetRetainedMessagesAsync();
    }

    public Task<IList<MqttSessionStatus>> GetSessionsAsync()
    {
        return _mqttServer.GetSessionsAsync();
    }

    public void Initialize()
    {
        MqttServerFactory mqttServerFactory;
        if (_options.EnableLogging)
        {
            mqttServerFactory = new MqttServerFactory(new LoggerAdapter(_logger));
        }
        else
        {
            mqttServerFactory = new MqttServerFactory();
        }

        _webSocketServerAdapter = new MqttWebSocketServerAdapter();

        var serverAdapters = new List<IMqttServerAdapter>
        {
            new MqttTcpServerAdapter(),
            _webSocketServerAdapter
        };

        var serverOptions = new MqttServerOptionsBuilder().WithDefaultEndpoint().WithDefaultEndpointPort(_options.ServerPort).WithPersistentSessions()
            .WithMaxPendingMessagesPerClient(int.MaxValue).Build();

        _mqttServer = mqttServerFactory.CreateMqttServer(serverOptions, serverAdapters);

        _mqttServer.InterceptingPublishAsync += e =>
        {
            MessageReceived?.Invoke(this, e);
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
                var retainedMessages = await storage.Load();
                e.LoadedRetainedMessages.AddRange(retainedMessages);
            };

            _mqttServer.RetainedMessageChangedAsync += e => storage.Update(e.StoredRetainedMessages);
        }
    }

    public Task Publish(InjectedMqttApplicationMessage applicationMessage)
    {
        if (applicationMessage == null)
        {
            throw new ArgumentNullException(nameof(applicationMessage));
        }

        return _mqttServer.InjectApplicationMessage(applicationMessage);
    }

    public Task RunWebSocketConnectionAsync(WebSocket webSocket, HttpContext context)
    {
        if (webSocket == null)
        {
            throw new ArgumentNullException(nameof(webSocket));
        }

        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        return _webSocketServerAdapter.RunWebSocketConnectionAsync(webSocket, context);
    }

    public Task StartAsync()
    {
        return _mqttServer.StartAsync();
    }

    public Task StopAsync()
    {
        return _mqttServer.StopAsync();
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