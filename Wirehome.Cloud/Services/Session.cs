using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Cloud;
using Wirehome.Core.Cloud.Messages;

namespace Wirehome.Cloud.Services
{
    public class Session
    {
        private readonly Channel _channel;
        private readonly AuthorizationScope _scope;
        private readonly ILogger _logger;

        public Session(Channel channel, AuthorizationScope scope, ILogger logger)
        {
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            _scope = scope ?? throw new ArgumentNullException(nameof(scope));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        public Task SendMessageAsync(BaseCloudMessage message, CancellationToken cancellationToken)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            return _channel.SendMessageAsync(message, cancellationToken);
        }

        public async Task ListenAsync(CancellationToken cancellationToken)
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

                    // TODO: Check if timestamp is expired to prevent replay attacks.

                    MessageReceived?.Invoke(this, new MessageReceivedEventArgs(receiveResult.Message));
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, $"Error while processing message of session '{_scope}'.");
                }
            }
        }
    }
}