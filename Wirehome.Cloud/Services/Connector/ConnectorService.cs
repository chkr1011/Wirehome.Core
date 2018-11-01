using System;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Wirehome.Cloud.Services.Authorization;
using Wirehome.Cloud.Services.Exceptions;
using Wirehome.Core.Cloud;
using Wirehome.Core.Cloud.Messages;

namespace Wirehome.Cloud.Services.Connector
{
    public class ConnectorService
    {
        private readonly Dictionary<string, Session> _sessions = new Dictionary<string, Session>();
        private readonly CloudMessageFactory _messageFactory = new CloudMessageFactory();
        private readonly AuthorizationService _authorizationService;
        private readonly ILogger _logger;

        public ConnectorService(AuthorizationService authorizationService, ILoggerFactory loggerFactory)
        {
            _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));

            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<ConnectorService>();
        }

        public async Task RunAsync(WebSocket webSocket, CancellationToken cancellationToken)
        {
            if (webSocket == null) throw new ArgumentNullException(nameof(webSocket));

            var channel = new ConnectorChannel(webSocket, _logger);
            try
            {
                AuthorizationContext scope;
                using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30)))
                {
                    var initialResult = await channel.ReceiveMessageAsync(timeout.Token).ConfigureAwait(false);
                    if (initialResult.CloseConnection)
                    {
                        return;
                    }

                    if (initialResult.Message.Type != CloudMessageType.Authorize)
                    {
                        throw new UnauthorizedAccessException();
                    }

                    var authorizationContent = initialResult.Message.Content?.ToObject<AuthorizeContent>();
                    if (authorizationContent == null)
                    {
                        throw new UnauthorizedAccessException();
                    }

                    scope = _authorizationService.AuthorizeConnector(authorizationContent);
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

        public async Task<CloudMessage> Invoke(AuthorizationContext authorizationContext, CloudMessage requestMessage, CancellationToken cancellationToken)
        {
            if (authorizationContext == null) throw new ArgumentNullException(nameof(authorizationContext));
            if (requestMessage == null) throw new ArgumentNullException(nameof(requestMessage));

            requestMessage.CorrelationUid = Guid.NewGuid();

            var result = new TaskCompletionSource<CloudMessage>();
            void messageReceived(object sender, MessageReceivedEventArgs eventArgs)
            {
                if (eventArgs.Message.CorrelationUid.HasValue)
                {
                    if (eventArgs.Message.CorrelationUid.Value.Equals(requestMessage.CorrelationUid))
                    {
                        result.TrySetResult(eventArgs.Message);
                        eventArgs.IsHandled = true;
                    }
                }
            }

            var session = GetSession(authorizationContext);
            try
            {
                session.MessageReceived += messageReceived;
                cancellationToken.Register(() => result.TrySetCanceled());

                await session.SendMessageAsync(requestMessage, CancellationToken.None).ConfigureAwait(false);
                return await result.Task.ConfigureAwait(false);
            }
            finally
            {
                session.MessageReceived -= messageReceived;
            }
        }

        public async Task ForwardHttpRequestAsync(HttpContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            try
            {
                context.Response.Headers.Add("Wirehome-Cloud-Enter", new StringValues(DateTime.UtcNow.ToString("O")));

                var authorizationContext = _authorizationService.AuthorizeBasic(context);

                var requestContent = new HttpRequestMessageContent
                {
                    Method = context.Request.Method,
                    Uri = context.Request.Path + context.Request.QueryString,
                    Content = LoadContent(context.Request)
                };

                if (!string.IsNullOrEmpty(context.Request.ContentType))
                {
                    requestContent.Headers.Add("Content-Type", context.Request.ContentType);
                }

                var requestMessage = _messageFactory.CreateMessage(CloudMessageType.HttpInvoke, requestContent);
                var responseMessage = await Invoke(authorizationContext, requestMessage, context.RequestAborted).ConfigureAwait(false);

                var responseContent = responseMessage.Content.ToObject<HttpResponseMessageContent>();
                context.Response.StatusCode = responseContent.StatusCode;

                foreach (var header in responseContent.Headers)
                {
                    context.Response.Headers.Add(header.Key, new StringValues(header.Value));
                }

                context.Response.Headers.Add("Wirehome-Cloud-Exit", new StringValues(DateTime.UtcNow.ToString("O")));

                if (responseContent.Content?.Length > 0)
                {
                    context.Response.Body.Write(responseContent.Content);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (UnauthorizedAccessException)
            {
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            }
            catch (SessionNotFoundException)
            {
                context.Response.StatusCode = (int)HttpStatusCode.GatewayTimeout;
            }
        }

        private async Task RunSessionAsync(ConnectorChannel channel, AuthorizationContext scope, CancellationToken cancellationToken)
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

        private Session GetSession(AuthorizationContext authorizationContext)
        {
            lock (_sessions)
            {
                var sessionKey = authorizationContext.ToString();
                if (!_sessions.TryGetValue(sessionKey, out var session))
                {
                    throw new SessionNotFoundException(sessionKey);
                }

                return session;
            }
        }

        private static byte[] LoadContent(HttpRequest httpRequest)
        {
            if (httpRequest.ContentLength.HasValue)
            {
                var buffer = new byte[httpRequest.ContentLength.Value];
                httpRequest.Body.Read(buffer, 0, buffer.Length);

                return buffer;
            }

            return null;
        }
    }
}
