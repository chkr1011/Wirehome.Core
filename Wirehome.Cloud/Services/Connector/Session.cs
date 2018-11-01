using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wirehome.Cloud.Services.Authorization;
using Wirehome.Core.Cloud;
using Wirehome.Core.Cloud.Messages;

namespace Wirehome.Cloud.Services.Connector
{
    public class Session
    {
        private readonly ConnectorChannel _channel;
        private readonly AuthorizationContext _authorizationContext;
        private readonly ILogger _logger;

        public Session(ConnectorChannel channel, AuthorizationContext authorizationContext, ILogger logger)
        {
            _channel = channel ?? throw new ArgumentNullException(nameof(channel));
            _authorizationContext = authorizationContext ?? throw new ArgumentNullException(nameof(authorizationContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        public Task SendMessageAsync(CloudMessage message, CancellationToken cancellationToken)
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

                    if (!DateTime.TryParseExact(receiveResult.Message.Timestamp, "O", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var timestamp))
                    {
                        continue;
                    }

                    if (DateTime.UtcNow - timestamp > TimeSpan.FromMinutes(10))
                    {
                        _logger.LogWarning("Truncated expired cloud message.");
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
                    _logger.LogError(exception, $"Error while processing message of session '{_authorizationContext}'.");
                }
            }
        }
    }
}