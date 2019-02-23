using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Wirehome.Cloud.Filters;
using Wirehome.Core.Cloud;
using Wirehome.Core.Cloud.Messages;

namespace Wirehome.Cloud.Services.DeviceConnector
{
    public class DeviceConnectorService
    {
        private readonly ConcurrentDictionary<string, DeviceSession> _sessions = new ConcurrentDictionary<string, DeviceSession>();
        private readonly CloudMessageFactory _cloudMessageFactory;
        private readonly ILogger _logger;

        public DeviceConnectorService(CloudMessageFactory cloudMessageFactory, ILogger<DeviceConnectorService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cloudMessageFactory = cloudMessageFactory ?? throw new ArgumentNullException(nameof(cloudMessageFactory));
        }

        public object GetStatistics()
        {
            return new
            {
                connectedDevicesCount = _sessions.Count,
                connectedDevices = _sessions.Keys
            };
        }

        public ConnectorChannelStatistics GetChannelStatistics(DeviceSessionIdentifier deviceSessionIdentifier)
        {
            if (!_sessions.TryGetValue(deviceSessionIdentifier.ToString(), out var deviceSession))
            {
                return null;
            }

            return deviceSession.GetStatistics();
        }

        public void ResetChannelStatistics(DeviceSessionIdentifier deviceSessionIdentifier)
        {
            if (!_sessions.TryGetValue(deviceSessionIdentifier.ToString(), out var deviceSession))
            {
                return;
            }

            deviceSession.ResetStatistics();
        }

        public async Task RunAsync(DeviceSessionIdentifier deviceSessionIdentifier, WebSocket webSocket, CancellationToken cancellationToken)
        {
            if (webSocket == null) throw new ArgumentNullException(nameof(webSocket));

            var channel = new ConnectorChannel(webSocket, _logger);
            try
            {
                var deviceSession = new DeviceSession(deviceSessionIdentifier, channel, _logger);
                _sessions[deviceSessionIdentifier.ToString()] = deviceSession;

                await deviceSession.RunAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while connecting device.");
            }
            finally
            {
                _sessions.TryRemove(deviceSessionIdentifier.ToString(), out _);
            }
        }

        public async Task<CloudMessage> Invoke(DeviceSessionIdentifier deviceSessionIdentifier, CloudMessage requestMessage, CancellationToken cancellationToken)
        {
            if (deviceSessionIdentifier == null) throw new ArgumentNullException(nameof(deviceSessionIdentifier));
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

            if (!_sessions.TryGetValue(deviceSessionIdentifier.ToString(), out var session))
            {
                throw new DeviceSessionNotFoundException(deviceSessionIdentifier);
            }

            try
            {
                session.MessageReceived += messageReceived;
                cancellationToken.Register(() =>
                {
                    if (!result.Task.IsCompleted && !result.Task.IsFaulted && !result.Task.IsCanceled)
                    {
                        result.TrySetCanceled();
                    }
                });

                await session.SendMessageAsync(requestMessage, cancellationToken).ConfigureAwait(false);
                return await result.Task.ConfigureAwait(false);
            }
            finally
            {
                session.MessageReceived -= messageReceived;
            }
        }

        public async Task TryDispatchHttpRequestAsync(HttpContext httpContext)
        {
            if (httpContext == null) throw new ArgumentNullException(nameof(httpContext));

            try
            {
                var deviceSessionIdentifier = httpContext.GetDeviceSessionIdentifier();
                if (deviceSessionIdentifier == null)
                {
                    httpContext.Response.Redirect("/Cloud/Account/Login");
                    return;
                }

                var requestContent = new HttpRequestMessageContent
                {
                    Method = httpContext.Request.Method,
                    Uri = httpContext.Request.Path + httpContext.Request.QueryString,
                    Content = LoadContent(httpContext.Request)
                };

                if (!string.IsNullOrEmpty(httpContext.Request.ContentType))
                {
                    requestContent.Headers.Add("Content-Type", httpContext.Request.ContentType);
                }

                var requestMessage = _cloudMessageFactory.CreateMessage(CloudMessageType.HttpInvoke, requestContent);
                var responseMessage = await Invoke(deviceSessionIdentifier, requestMessage, httpContext.RequestAborted).ConfigureAwait(false);

                var responseContent = responseMessage.Content.ToObject<HttpResponseMessageContent>();
                httpContext.Response.StatusCode = responseContent.StatusCode;

                foreach (var header in responseContent.Headers)
                {
                    httpContext.Response.Headers.Add(header.Key, new StringValues(header.Value));
                }

                if (responseContent.Content?.Length > 0)
                {
                    httpContext.Response.Body.Write(responseContent.Content);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                DefaultExceptionFilter.HandleException(exception, httpContext);
            }
        }

        private static byte[] LoadContent(HttpRequest httpRequest)
        {
            if (!httpRequest.ContentLength.HasValue)
            {
                return null;
            }

            var buffer = new byte[httpRequest.ContentLength.Value];
            httpRequest.Body.Read(buffer, 0, buffer.Length);

            return buffer;
        }
    }
}
