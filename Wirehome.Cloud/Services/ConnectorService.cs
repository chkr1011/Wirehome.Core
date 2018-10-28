using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Wirehome.Cloud.Services.Exceptions;
using Wirehome.Core.Cloud;
using Wirehome.Core.Cloud.Messages;

namespace Wirehome.Cloud.Services
{
    public class ConnectorService
    {
        private readonly Dictionary<string, Session> _sessions = new Dictionary<string, Session>();
        private readonly CloudMessageParser _cloudMessageParser = new CloudMessageParser();

        private readonly ILogger _logger;
        private readonly AuthorizationService _authorizationService;

        public ConnectorService(AuthorizationService authorizationService, ILoggerFactory loggerFactory)
        {
            _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));

            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<ConnectorService>();
        }

        public async Task ConnectAsync(WebSocket webSocket, CancellationToken cancellationToken)
        {
            if (webSocket == null) throw new ArgumentNullException(nameof(webSocket));

            var channel = new Channel(webSocket);
            try
            {
                AuthorizationScope scope;
                using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                {
                    var initialResult = await channel.ReceiveMessageAsync(timeout.Token).ConfigureAwait(false);
                    if (initialResult.CloseConnection)
                    {
                        return;
                    }

                    if (!_cloudMessageParser.TryParse(initialResult.Message, out AuthorizeCloudMessage authorizeMessage))
                    {
                        throw new UnauthorizedAccessException();
                    }

                    scope = _authorizationService.Authorize(authorizeMessage);
                    if (scope == null)
                    {
                        throw new UnauthorizedAccessException();
                    }
                }

                await RunSessionAsync(channel, scope, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while connecting client.");

                await channel.CloseAsync().ConfigureAwait(false);
            }
        }

        public async Task<JObject> Invoke(string identityUid, string channelUid, JObject request, CancellationToken cancellationToken)
        {
            if (identityUid == null) throw new ArgumentNullException(nameof(identityUid));
            if (channelUid == null) throw new ArgumentNullException(nameof(channelUid));
            if (request == null) throw new ArgumentNullException(nameof(request));

            var result = new TaskCompletionSource<JObject>();

            var requestMessage = new RpcRequestCloudMessage
            {
                Message = request
            };

            void messageReceived(object sender, MessageReceivedEventArgs eventArgs)
            {
                var cloudMessageParser = new CloudMessageParser();
                if (cloudMessageParser.TryParse(eventArgs.Message, out RpcResponseCloudMessage responseMessage))
                {
                    if (string.Equals(requestMessage.CorrelationUid, responseMessage.CorrelationUid))
                    {
                        result.TrySetResult(responseMessage.Message);
                    }
                }
            }

            var session = GetSession(identityUid, channelUid);
            try
            {
                session.MessageReceived += messageReceived;
                cancellationToken.Register(() => result.TrySetCanceled());

                await session.SendMessageAsync(requestMessage, cancellationToken).ConfigureAwait(false);
                return await result.Task.ConfigureAwait(false);
            }
            finally
            {
                session.MessageReceived -= messageReceived;
            }
        }

        public Task SendMessage(string identityUid, string channelUid, JObject message, CancellationToken cancellationToken)
        {
            if (identityUid == null) throw new ArgumentNullException(nameof(identityUid));
            if (channelUid == null) throw new ArgumentNullException(nameof(channelUid));
            if (message == null) throw new ArgumentNullException(nameof(message));

            var session = GetSession(identityUid, channelUid);

            throw new NotImplementedException();
        }

        private async Task RunSessionAsync(Channel channel, AuthorizationScope scope, CancellationToken cancellationToken)
        {
            var sessionKey = scope.ToString();
            try
            {
                var session = new Session(channel, scope, _logger);
                lock (_sessions)
                {
                    _sessions[sessionKey] = session;
                }

                await session.ListenAsync(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                lock (_sessions)
                {
                    _sessions.Remove(sessionKey);
                }
            }
        }

        private Session GetSession(string identityUid, string channelUid)
        {
            var key = new AuthorizationScope(identityUid, channelUid).ToString();

            lock (_sessions)
            {
                if (!_sessions.TryGetValue(key, out var session))
                {
                    throw new SessionNotFoundException(key);
                }

                return session;
            }
        }
    }
}
