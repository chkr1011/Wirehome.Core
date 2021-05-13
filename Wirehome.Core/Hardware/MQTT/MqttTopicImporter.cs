using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using System;
using System.Threading.Tasks;

namespace Wirehome.Core.Hardware.MQTT
{
    public sealed class MqttTopicImporter
    {
        readonly MqttImportTopicParameters _parameters;
        readonly MqttService _mqttService;
        readonly ILogger _logger;

        IManagedMqttClient _mqttClient;

        public MqttTopicImporter(MqttImportTopicParameters parameters, MqttService mqttService, ILogger logger)
        {
            _parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
            _mqttService = mqttService ?? throw new ArgumentNullException(nameof(mqttService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Start()
        {
            var optionsBuilder = new ManagedMqttClientOptionsBuilder();
            optionsBuilder = optionsBuilder.WithClientOptions(
                o => o
                    .WithTcpServer(_parameters.Server, _parameters.Port)
                    .WithCredentials(_parameters.Username, _parameters.Password)
                    .WithClientId(_parameters.ClientId)
                    .WithTls(new MqttClientOptionsBuilderTlsParameters
                    {
                        UseTls = _parameters.UseTls
                    }));

            if (!string.IsNullOrEmpty(_parameters.ClientId))
            {
                optionsBuilder = optionsBuilder.WithClientOptions(o => o.WithClientId(_parameters.ClientId));
            }

            var options = optionsBuilder.Build();

            if (_mqttService.IsLowLevelMqttLoggingEnabled)
            {
                _mqttClient = new MqttFactory().CreateManagedMqttClient(); //new LoggerAdapter(_logger));
            }
            else
            {
                _mqttClient = new MqttFactory().CreateManagedMqttClient();
            }

            await _mqttClient.SubscribeAsync(_parameters.Topic, _parameters.QualityOfServiceLevel).ConfigureAwait(false);
            _mqttClient.UseApplicationMessageReceivedHandler(e => OnApplicationMessageReceived(e));
            await _mqttClient.StartAsync(options).ConfigureAwait(false);
        }

        public async Task Stop()
        {
            try
            {
                if (_mqttClient != null)
                {
                    await _mqttClient.StopAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                _mqttClient?.Dispose();
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
