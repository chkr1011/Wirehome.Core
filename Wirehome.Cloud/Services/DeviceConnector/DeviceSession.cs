using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Cloud;
using Wirehome.Core.Cloud.Messages;

namespace Wirehome.Cloud.Services.DeviceConnector
{
    public class DeviceSession
    {
        private readonly DeviceSessionIdentifier _identifier;
        private readonly ConnectorChannel _channel;
        private readonly ILogger _logger;
        
        public DeviceSession(DeviceSessionIdentifier identifier, ConnectorChannel channel, ILogger logger)
        {
            _identifier = identifier ?? throw new ArgumentNullException(nameof(identifier));
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

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

                    var eventHandler = MessageReceived;
                    if (eventHandler != null)
                    {
                        var delegates = eventHandler.GetInvocationList();
                        var eventArgs = new MessageReceivedEventArgs(receiveResult.Message);

                        foreach (var @delegate in delegates)
                        {
                            @delegate.DynamicInvoke(this, eventArgs);
                            if (eventArgs.IsHandled)
                            {
                                break;
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
    }
}