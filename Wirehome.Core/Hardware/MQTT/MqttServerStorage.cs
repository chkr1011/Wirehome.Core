using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MQTTnet;
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
                
                if (!_messagesHaveChanged)
                {
                    continue;
                }
                
                lock (_messagesSyncRoot)
                {
                    messages = _messages;
                    _messages = null;
                    _messagesHaveChanged = false;
                }

                var model = messages.ConvertAll(MqttRetainedMessageModel.Create);
                _storageService.WriteSerializedValue(model, "RetainedMqttMessages.json");

                _logger.LogInformation("{MessagesCount} retained MQTT messages written to storage", messages.Count);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while writing retained MQTT messages to storage");
            }
            finally
            {
                await Task.Delay(TimeSpan.FromMinutes(15), _systemCancellationToken.Token).ConfigureAwait(false);
            }
        }
    }

   
}