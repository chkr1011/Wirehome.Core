using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Wirehome.Core.Cloud.Channel;
using Wirehome.Core.Cloud.Protocol;

namespace Wirehome.Cloud.Services.DeviceConnector
{
    public class DeviceSession
    {
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<CloudMessage>> _messageAwaiters = new ConcurrentDictionary<Guid, TaskCompletionSource<CloudMessage>>();
        private readonly DeviceSessionIdentifier _identifier;
        private readonly ConnectorChannel _channel;
        private readonly ILogger _logger;
        
        public DeviceSession(DeviceSessionIdentifier identifier, ConnectorChannel channel, ILogger logger)
        {
            _identifier = identifier ?? throw new ArgumentNullException(nameof(identifier));
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public ConnectorChannelStatistics GetStatistics()
        {
            return _channel.GetStatistics();
        }

        public void ResetStatistics()
        {
            _channel.ResetStatistics();
        }

        public Task SendMessageAsync(CloudMessage message, CancellationToken cancellationToken)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            return _channel.SendMessageAsync(message, cancellationToken);
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var receiveResult = await _channel.ReceiveMessageAsync(cancellationToken).ConfigureAwait(false);
                    if (receiveResult.CloseConnection)
                    {
                        break;
                    }

                    if (receiveResult.Message == null)
                    {
                        continue;
                    }

                    if (receiveResult.Message.CorrelationUid.HasValue)
                    {
                        if (_messageAwaiters.TryRemove(receiveResult.Message.CorrelationUid.Value, out var awaiter))
                        {
                            awaiter.TrySetResult(receiveResult.Message);
                        }
                    }
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, $"Error while processing message of session '{_identifier}'.");
                }
            }
        }

        public void AddMessageAwaiter(TaskCompletionSource<CloudMessage> awaiter, Guid messageCorrelationUid)
        {
            _messageAwaiters[messageCorrelationUid] = awaiter ?? throw new ArgumentNullException(nameof(awaiter));
        }

        public void RemoveMessageAwaiter(Guid correlationUidValue)
        {
            _messageAwaiters.TryRemove(correlationUidValue, out _);
        }
    }
}