using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client;
using Wirehome.Core.Extensions;

namespace Wirehome.Core.Hardware.MQTT
{
    public sealed class MqttTopicImporter
    {
        readonly CancellationTokenSource _cancellationTokenSource = new();
        readonly ILogger _logger;
        readonly MqttService _mqttService;

        readonly MqttImportTopicParameters _parameters;

        MqttClient _mqttClient;
        MqttClientOptions _options;

        public MqttTopicImporter(MqttImportTopicParameters parameters, MqttService mqttService, ILogger logger)
        {
            _parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            _mqttService = mqttService ?? throw new ArgumentNullException(nameof(mqttService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Start()
        {
            var optionsBuilder = new MqttClientOptionsBuilder().WithTcpServer(_parameters.Server, _parameters.Port).WithCredentials(_parameters.Username, _parameters.Password)
                .WithClientId(_parameters.ClientId).WithTls(new MqttClientOptionsBuilderTlsParameters
                {
                    UseTls = _parameters.UseTls
                });

            if (!string.IsNullOrEmpty(_parameters.ClientId))
            {
                optionsBuilder = optionsBuilder.WithClientId(_parameters.ClientId);
            }

            _options = optionsBuilder.Build();

            if (_mqttService.IsLowLevelMqttLoggingEnabled)
            {
                _mqttClient = new MqttFactory(new LoggerAdapter(_logger)).CreateMqttClient();
            }
            else
            {
                _mqttClient = new MqttFactory().CreateMqttClient();
            }

            await _mqttClient.SubscribeAsync(_parameters.Topic, _parameters.QualityOfServiceLevel).ConfigureAwait(false);
            _mqttClient.ApplicationMessageReceivedAsync += e =>
            {
                OnApplicationMessageReceived(e);
                return Task.CompletedTask;
            };

            _ = Task.Run(() => MaintainConnectionLoop(_cancellationTokenSource.Token), _cancellationTokenSource.Token).Forget(_logger);
        }

        public async Task Stop()
        {
            try
            {
                _cancellationTokenSource?.Cancel();

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
                        await _mqttClient.ConnectAsync(_options, cancellationToken).ConfigureAwait(false);
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

        void OnApplicationMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            _mqttService.Publish(new MqttPublishParameters
            {
                Topic = e.ApplicationMessage.Topic,
                Payload = e.ApplicationMessage.Payload,
                QualityOfServiceLevel = e.ApplicationMessage.QualityOfServiceLevel,
                Retain = e.ApplicationMessage.Retain
            });
        }
    }
}