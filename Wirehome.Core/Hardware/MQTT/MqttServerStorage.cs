using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet;
using MQTTnet.Packets;
using MQTTnet.Protocol;
using Wirehome.Core.Extensions;
using Wirehome.Core.Storage;
using Wirehome.Core.System;

namespace Wirehome.Core.Hardware.MQTT;

public sealed class MqttServerStorage
{
    readonly ILogger _logger;
    readonly object _messagesSyncRoot = new();
    readonly StorageService _storageService;
    readonly SystemCancellationToken _systemCancellationToken;

    List<MqttApplicationMessage> _messages;
    bool _messagesHaveChanged;

    public MqttServerStorage(StorageService storageService, SystemCancellationToken systemCancellationToken, ILogger logger)
    {
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _systemCancellationToken = systemCancellationToken ?? throw new ArgumentNullException(nameof(systemCancellationToken));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<List<MqttApplicationMessage>> Load()
    {
        await Task.CompletedTask;

        _storageService.TryReadSerializedValue(out List<MqttRetainedMessageModel> messages, "RetainedMqttMessages.json");

        if (messages == null)
        {
            return new List<MqttApplicationMessage>();
        }

        return messages.ConvertAll(m => m.ToApplicationMessage());
    }

    public void Start()
    {
        ParallelTask.Start(Save, CancellationToken.None, _logger);
    }

    public Task Update(List<MqttApplicationMessage> messages)
    {
        lock (_messagesSyncRoot)
        {
            _messages = messages;
            _messagesHaveChanged = true;
        }

        return Task.CompletedTask;
    }

    async Task Save()
    {
        while (!_systemCancellationToken.Token.IsCancellationRequested)
        {
            try
            {
                List<MqttApplicationMessage> messages;
                lock (_messagesSyncRoot)
                {
                    if (!_messagesHaveChanged)
                    {
                        continue;
                    }

                    messages = _messages;
                    _messages = null;
                    _messagesHaveChanged = false;
                }

                var model = messages.ConvertAll(MqttRetainedMessageModel.Create);
                _storageService.WriteSerializedValue(model, "RetainedMqttMessages.json");

                _logger.LogInformation($"{messages.Count} retained MQTT messages written to storage");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while writing retained MQTT messages to storage");
            }
            finally
            {
                await Task.Delay(TimeSpan.FromSeconds(60), _systemCancellationToken.Token).ConfigureAwait(false);
            }
        }
    }

    sealed class MqttRetainedMessageModel
    {
        public string ContentType { get; set; }
        public byte[] CorrelationData { get; set; }
        public byte[] Payload { get; set; }
        public MqttPayloadFormatIndicator PayloadFormatIndicator { get; set; }
        public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; }
        public string ResponseTopic { get; set; }
        public string Topic { get; set; }
        public List<MqttUserProperty> UserProperties { get; set; }

        public static MqttRetainedMessageModel Create(MqttApplicationMessage message)
        {
            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            return new MqttRetainedMessageModel
            {
                Topic = message.Topic,

                // Create a copy of the buffer from the payload segment because 
                // it cannot be serialized and deserialized with the JSON serializer.
                Payload = message.PayloadSegment.ToArray(),
                UserProperties = message.UserProperties,
                ResponseTopic = message.ResponseTopic,
                CorrelationData = message.CorrelationData,
                ContentType = message.ContentType,
                PayloadFormatIndicator = message.PayloadFormatIndicator,
                QualityOfServiceLevel = message.QualityOfServiceLevel

                // Other properties like "Retain" are not if interest in the storage.
                // That's why a custom model makes sense.
            };
        }

        public MqttApplicationMessage ToApplicationMessage()
        {
            return new MqttApplicationMessage
            {
                Topic = Topic,
                PayloadSegment = new ArraySegment<byte>(Payload ?? Array.Empty<byte>()),
                PayloadFormatIndicator = PayloadFormatIndicator,
                ResponseTopic = ResponseTopic,
                CorrelationData = CorrelationData,
                ContentType = ContentType,
                UserProperties = UserProperties,
                QualityOfServiceLevel = QualityOfServiceLevel,
                Dup = false,
                Retain = true
            };
        }
    }
}