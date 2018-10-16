using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Server;
using Wirehome.Core.Storage;

namespace Wirehome.Core.Hardware.MQTT
{
    public class MqttServerStorage : IMqttServerStorage
    {
        private readonly List<MqttApplicationMessage> _messages = new List<MqttApplicationMessage>();
        private readonly StorageService _storageService;
        
        public MqttServerStorage(StorageService storageService)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        }

        public Task SaveRetainedMessagesAsync(IList<MqttApplicationMessage> messages)
        {
            lock (_messages)
            {
                _messages.Clear();
                _messages.AddRange(messages);
            }

            // TODO: Consider starting a async task which stores the messages (from field) all 30 Seconds.
            //_storageService.Write(messages ?? new List<MqttApplicationMessage>(), "RetainedMqttMessages.json");
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
