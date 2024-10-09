using System;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet;
using Wirehome.Core.Extensions;

namespace Wirehome.Core.Hardware.MQTT.TopicImport;

public sealed class MqttTopicImporter
{
    readonly CancellationTokenSource _cancellationTokenSource = new();
    readonly ILogger _logger;
    readonly MqttService _mqttService;
    readonly MqttTopicImportOptions _options;

    IMqttClient _mqttClient;
    MqttClientOptions _mqttClientOptions;

    public MqttTopicImporter(MqttTopicImportOptions options, MqttService mqttService, ILogger logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _mqttService = mqttService ?? throw new ArgumentNullException(nameof(mqttService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task Start()
    {
        var optionsBuilder = new MqttClientOptionsBuilder().WithTcpServer(_options.Server, _options.Port).WithCredentials(_options.Username, _options.Password)
            .WithClientId(_options.ClientId).WithTlsOptions(o => o.UseTls(_options.UseTls));

        if (!string.IsNullOrEmpty(_options.ClientId))
        {
            optionsBuilder = optionsBuilder.WithClientId(_options.ClientId);
        }

        _mqttClientOptions = optionsBuilder.Build();

        if (_options.EnableLogging)
        {
            _mqttClient = new MqttClientFactory(new LoggerAdapter(_logger)).CreateMqttClient();
        }
        else
        {
            _mqttClient = new MqttClientFactory().CreateMqttClient();
        }

        await _mqttClient.SubscribeAsync(_options.Topic, _options.QualityOfServiceLevel).ConfigureAwait(false);
        _mqttClient.ApplicationMessageReceivedAsync += OnApplicationMessageReceived;

        _ = Task.Run(() => MaintainConnectionLoop(_cancellationTokenSource.Token), _cancellationTokenSource.Token).Forget(_logger);
    }

    public async Task Stop()
    {
        try
        {
            await _cancellationTokenSource.CancelAsync();

            if (_mqttClient != null)
            {
                await _mqttClient.DisconnectAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            _mqttClient?.Dispose();
            _cancellationTokenSource?.Dispose();
        }
    }

    async Task MaintainConnectionLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                if (!_mqttClient.IsConnected)
                {
                    await _mqttClient.ConnectAsync(_mqttClientOptions, cancellationToken).ConfigureAwait(false);
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while maintaining MQTT client connection.");
            }
            finally
            {
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
            }
        }
    }

    Task OnApplicationMessageReceived(MqttApplicationMessageReceivedEventArgs e)
    {
        return _mqttService.Publish(new MqttPublishOptions
        {
            Topic = e.ApplicationMessage.Topic,
            Payload = e.ApplicationMessage.Payload.ToArray(),
            QualityOfServiceLevel = e.ApplicationMessage.QualityOfServiceLevel,
            Retain = e.ApplicationMessage.Retain
        });
    }
}