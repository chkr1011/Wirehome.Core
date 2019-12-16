using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Wirehome.Core.Cloud.Channel;
using Wirehome.Core.Cloud.Protocol;

namespace Wirehome.Cloud.Services.DeviceConnector
{
    public class DeviceSession
    {
        readonly Dictionary<string, TaskCompletionSource<CloudMessage>> _messageAwaiters = new Dictionary<string, TaskCompletionSource<CloudMessage>>();
        readonly DeviceSessionIdentifier _identifier;
        readonly ConnectorChannel _channel;
        readonly ILogger _logger;
        
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

        public Task SendAsync(CloudMessage cloudMessage, CancellationToken cancellationToken)
        {
            if (cloudMessage == null) throw new ArgumentNullException(nameof(cloudMessage));

            return _channel.SendAsync(cloudMessage, cancellationToken);
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
                    _logger.LogError(exception, $"Error while processing message of session '{_identifier}'.");
                }
            }
        }

        public void AddAwaiter(string correlationId, TaskCompletionSource<CloudMessage> awaiter)
        {
            lock (_messageAwaiters)
            {
                _messageAwaiters[correlationId] = awaiter ?? throw new ArgumentNullException(nameof(awaiter));
            }
        }

        public void RemoveAwaiter(string correlationId)
        {
            lock (_messageAwaiters)
            {
                _messageAwaiters.Remove(correlationId);
            }
        }
    }
}