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
    readonly List<MqttApplicationMessage> _messages = new();
    readonly StorageService _storageService;
    readonly SystemCancellationToken _systemCancellationToken;

    bool _messagesHaveChanged;

    public MqttServerStorage(StorageService storageService, SystemCancellationToken systemCancellationToken, ILogger logger)
    {
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _systemCancellationToken = systemCancellationToken ?? throw new ArgumentNullException(nameof(systemCancellationToken));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task<IList<MqttApplicationMessage>> LoadRetainedMessagesAsync()
    {
        _storageService.TryReadSerializedValue(out List<MqttApplicationMessage> messages, "RetainedMqttMessages.json");

        if (messages == null)
        {
            messages = new List<MqttApplicationMessage>();
        }

        return Task.FromResult<IList<MqttApplicationMessage>>(messages);
    }

    public Task SaveRetainedMessagesAsync(IList<MqttApplicationMessage> messages)
    {
        lock (_messages)
        {
            _messages.Clear();
            _messages.AddRange(messages);

            _messagesHaveChanged = true;
        }

        return Task.CompletedTask;
    }

    public void Start()
    {
        ParallelTask.Start(SaveRetainedMessagesInternalAsync, CancellationToken.None, _logger);
    }

    async Task SaveRetainedMessagesInternalAsync()
    {
        while (!_systemCancellationToken.Token.IsCancellationRequested)
        {
            try
            {
                List<MqttApplicationMessage> messages;
                lock (_messages)
                {
                    if (!_messagesHaveChanged)
                    {
                        continue;
                    }

                    messages = new List<MqttApplicationMessage>(_messages);
                    _messagesHaveChanged = false;
                }

                _storageService.WriteSerializedValue(messages, "RetainedMqttMessages.json");

                _logger.LogInformation($"{messages.Count} retained MQTT messages written to storage.");
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while writing retained MQTT messages to storage.");
            }
            finally
            {
                await Task.Delay(TimeSpan.FromSeconds(60), _systemCancellationToken.Token).ConfigureAwait(false);
            }
        }
    }
}