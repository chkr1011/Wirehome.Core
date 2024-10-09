using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Formatter;
using MQTTnet.Protocol;
using MQTTnet.Server;
using Wirehome.Core.System;

namespace Wirehome.Core.Hardware.MQTT.Backends;

public sealed class MqttClientBackend : IMqttBackend
{
    static readonly Dictionary<string, object> SessionItems = new();

    readonly ILogger _logger;
    readonly MqttServiceOptions _options;
    readonly SystemCancellationToken _systemCancellationToken;

    IMqttClient _mqttClient;

    public MqttClientBackend(MqttServiceOptions options, SystemCancellationToken systemCancellationToken, ILogger logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _systemCancellationToken = systemCancellationToken ?? throw new ArgumentNullException(nameof(systemCancellationToken));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public event EventHandler<InterceptingPublishEventArgs> MessageReceived;

    public Task DeleteRetainedMessagesAsync()
    {
        // Not supported.
        return Task.CompletedTask;
    }

    public Task<IList<MqttClientStatus>> GetClientsAsync()
    {
        // Not supported.
        return Task.FromResult((IList<MqttClientStatus>)new List<MqttClientStatus>(0));
    }

    public Task<int> GetConnectedClientsCount()
    {
        // Not supported.
        return Task.FromResult(0);
    }

    public Task<IList<MqttApplicationMessage>> GetRetainedMessagesAsync()
    {
        return Task.FromResult((IList<MqttApplicationMessage>)new List<MqttApplicationMessage>(0));
    }

    public Task<IList<MqttSessionStatus>> GetSessionsAsync()
    {
        return Task.FromResult((IList<MqttSessionStatus>)new List<MqttSessionStatus>(0));
    }

    public void Initialize()
    {
        MqttClientFactory mqttClientFactory;
        if (_options.EnableLogging)
        {
            mqttClientFactory = new MqttClientFactory(new LoggerAdapter(_logger));
        }
        else
        {
            mqttClientFactory = new MqttClientFactory();
        }

        _mqttClient = mqttClientFactory.CreateMqttClient();
        _mqttClient.ApplicationMessageReceivedAsync += OnApplicationMessageReceived;
    }

    public async Task Publish(InjectedMqttApplicationMessage applicationMessage)
    {
        if (applicationMessage == null)
        {
            throw new ArgumentNullException(nameof(applicationMessage));
        }

        await _mqttClient.PublishAsync(applicationMessage.ApplicationMessage).ConfigureAwait(false);
    }

    public Task StartAsync()
    {
        Task.Run(() => MaintainConnectionLoop(_systemCancellationToken.Token), _systemCancellationToken.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        await _mqttClient.DisconnectAsync();

        _mqttClient.Dispose();
    }

    async Task MaintainConnectionLoop(CancellationToken cancellationToken)
    {
        var options = new MqttClientOptionsBuilder().WithTcpServer(_options.RemoteServerHost, _options.RemoteServerPort).WithProtocolVersion(MqttProtocolVersion.V500).Build();
        var defaultSubscription = new MqttClientSubscribeOptionsBuilder().WithTopicFilter("#").Build();

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (!await _mqttClient.TryPingAsync(cancellationToken).ConfigureAwait(false))
                {
                    await _mqttClient.ConnectAsync(options, cancellationToken).ConfigureAwait(false);
                    await _mqttClient.SubscribeAsync(defaultSubscription, cancellationToken).ConfigureAwait(false);

                    _logger.LogInformation("MQTT client connected");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error while maintaining MQTT connection");
            }
            finally
            {
                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken).ConfigureAwait(false);
            }
        }
    }

    Task OnApplicationMessageReceived(MqttApplicationMessageReceivedEventArgs eventArgs)
    {
        var interceptingPublish = new InterceptingPublishEventArgs(eventArgs.ApplicationMessage, CancellationToken.None, "CLIENT", SessionItems);

        MessageReceived?.Invoke(this, interceptingPublish);

        return Task.CompletedTask;
    }
}