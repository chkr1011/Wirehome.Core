using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Server;
using Wirehome.Core.Storage;

namespace Wirehome.Core.Hardware.MQTT
{
    public class MqttServerStorage : IMqttServerStorage
    {
        private readonly List<MqttApplicationMessage> _messages = new List<MqttApplicationMessage>();
        private readonly StorageService _storageService;
        private readonly ILogger _logger;

        public MqttServerStorage(StorageService storageService, ILogger logger)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void Start()
        {
            Task.Factory.StartNew(SaveRetainedMessagesInternalAsync);
        }

        private async Task SaveRetainedMessagesInternalAsync()
        {
            while (true)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(60)).ConfigureAwait(false);

                    List<MqttApplicationMessage> messages;
                    lock (_messages)
                    {
                        messages = new List<MqttApplicationMessage>(_messages);
                    }

                    _storageService.Write(messages, "RetainedMqttMessages.json");

                    _logger.LogInformation($"{messages.Count} retained MQTT messages written to storage.");
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Error while writing retained MQTT messages to storage.");
                }
            }
        }

        public Task SaveRetainedMessagesAsync(IList<MqttApplicationMessage> messages)
        {
            lock (_messages)
            {
                _messages.Clear();
                _messages.AddRange(messages);
            }

            return Task.CompletedTask;
        }

        public Task<IList<MqttApplicationMessage>> LoadRetainedMessagesAsync()
        {
            _storageService.TryRead(out List<MqttApplicationMessage> messages, "RetainedMqttMessages.json");
            if (messages == null)
            {
                messages = new List<MqttApplicationMessage>();
            }

            return Task.FromResult<IList<MqttApplicationMessage>>(messages);
        }
    }
}
