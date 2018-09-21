using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Server;
using Wirehome.Core.Storage;

namespace Wirehome.Core.Hardware.MQTT
{
    public partial class MqttService
    {
        public class MqttServerStorage : IMqttServerStorage
        {
            private readonly StorageService _storageService;

            public MqttServerStorage(StorageService storageService)
            {
                _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            }

            public Task SaveRetainedMessagesAsync(IList<MqttApplicationMessage> messages)
            {
                _storageService.Write(messages, "RetainedMqttMessages.json");
                return Task.CompletedTask;
            }

            public Task<IList<MqttApplicationMessage>> LoadRetainedMessagesAsync()
            {
                if (!_storageService.TryRead(out List<MqttApplicationMessage> messages, "RetainedMqttMessages.json"))
                {
                    messages = new List<MqttApplicationMessage>();
                }

                return Task.FromResult<IList<MqttApplicationMessage>>(messages);
            }
        }
    }
}
