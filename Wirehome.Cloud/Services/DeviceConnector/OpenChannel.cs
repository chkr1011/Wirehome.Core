using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Cloud.Channel;
using Wirehome.Core.Cloud.Protocol;

namespace Wirehome.Cloud.Services.DeviceConnector;

public class OpenChannel
{
    readonly ConnectorChannel _channel;
    readonly ChannelIdentifier _identifier;
    readonly ILogger _logger;
    readonly Dictionary<string, TaskCompletionSource<CloudMessage>> _messageAwaiters = new();

    public OpenChannel(ChannelIdentifier identifier, ConnectorChannel channel, ILogger logger)
    {
        _identifier = identifier ?? throw new ArgumentNullException(nameof(identifier));
        _channel = channel ?? throw new ArgumentNullException(nameof(channel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void AddAwaiter(string correlationId, TaskCompletionSource<CloudMessage> awaiter)
    {
        lock (_messageAwaiters)
        {
            _messageAwaiters[correlationId] = awaiter ?? throw new ArgumentNullException(nameof(awaiter));
        }
    }

    public ConnectorChannelStatistics GetStatistics()
    {
        return _channel.GetStatistics();
    }

    public void RemoveAwaiter(string correlationId)
    {
        lock (_messageAwaiters)
        {
            _messageAwaiters.Remove(correlationId);
        }
    }

    public void ResetStatistics()
    {
        _channel.ResetStatistics();
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var receiveResult = await _channel.ReceiveAsync(cancellationToken).ConfigureAwait(false);
                if (receiveResult.CloseConnection)
                {
                    break;
                }

                if (receiveResult.Message == null)
                {
                    continue;
                }

                if (receiveResult.Message.CorrelationId != null)
                {
                    lock (_messageAwaiters)
                    {
                        if (_messageAwaiters.Remove(receiveResult.Message.CorrelationId, out var awaiter))
                        {
                            awaiter.TrySetResult(receiveResult.Message);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, $"Error while processing message of channel '{_identifier}'.");
            }
        }
    }

    public Task SendAsync(CloudMessage cloudMessage, CancellationToken cancellationToken)
    {
        if (cloudMessage == null)
        {
            throw new ArgumentNullException(nameof(cloudMessage));
        }

        return _channel.SendAsync(cloudMessage, cancellationToken);
    }
}