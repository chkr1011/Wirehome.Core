using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Wirehome.Cloud.Filters;
using Wirehome.Cloud.Services.Authorization;
using Wirehome.Core.Cloud.Channel;
using Wirehome.Core.Cloud.Protocol;
using Wirehome.Core.Cloud.Protocol.Content;

namespace Wirehome.Cloud.Services.DeviceConnector
{
    public class DeviceConnectorService
    {
        private readonly Dictionary<string, DeviceSession> _deviceSessions = new Dictionary<string, DeviceSession>();
        private readonly AuthorizationService _authorizationService;
        private readonly CloudMessageFactory _cloudMessageFactory;
        private readonly CloudMessageSerializer _cloudMessageSerializer;
        private readonly ILogger _logger;

        public DeviceConnectorService(AuthorizationService authorizationService, CloudMessageFactory cloudMessageFactory, CloudMessageSerializer cloudMessageSerializer, ILogger<DeviceConnectorService> logger)
        {
            _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
            _cloudMessageFactory = cloudMessageFactory ?? throw new ArgumentNullException(nameof(cloudMessageFactory));
            _cloudMessageSerializer = cloudMessageSerializer ?? throw new ArgumentNullException(nameof(cloudMessageSerializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public object GetStatistics()
        {
            lock (_deviceSessions)
            {
                return new
                {
                    connectedDevicesCount = _deviceSessions.Count,
                    connectedDevices = _deviceSessions.Keys
                };
            }
        }

        public ConnectorChannelStatistics GetChannelStatistics(DeviceSessionIdentifier deviceSessionIdentifier)
        {
            lock (_deviceSessions)
            {
                if (!_deviceSessions.TryGetValue(deviceSessionIdentifier.ToString(), out var deviceSession))
                {
                    return null;
                }

                return deviceSession.GetStatistics();
            }      
        }

        public void ResetChannelStatistics(DeviceSessionIdentifier deviceSessionIdentifier)
        {
            lock (_deviceSessions)
            {
                if (!_deviceSessions.TryGetValue(deviceSessionIdentifier.ToString(), out var deviceSession))
                {
                    return;
                }

                deviceSession.ResetStatistics();
            }
        }

        public async Task RunAsync(DeviceSessionIdentifier deviceSessionIdentifier, WebSocket webSocket, CancellationToken cancellationToken)
        {
            if (webSocket == null) throw new ArgumentNullException(nameof(webSocket));

            var channelOptions = new ConnectorChannelOptions
            {
                UseCompression = true
            };

            var channel = new ConnectorChannel(channelOptions, webSocket, _cloudMessageSerializer, _logger);
            try
            {
                var deviceSession = new DeviceSession(deviceSessionIdentifier, channel, _logger);
                lock (_deviceSessions)
                {
                    _deviceSessions[deviceSessionIdentifier.ToString()] = deviceSession;
                }
                
                await deviceSession.RunAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while connecting device.");
            }
            finally
            {
                lock (_deviceSessions)
                {
                    _deviceSessions.Remove(deviceSessionIdentifier.ToString());
                }
            }
        }

        public async Task<CloudMessage> Invoke(DeviceSessionIdentifier deviceSessionIdentifier, CloudMessage requestMessage, CancellationToken cancellationToken)
        {
            if (deviceSessionIdentifier == null) throw new ArgumentNullException(nameof(deviceSessionIdentifier));
            if (requestMessage == null) throw new ArgumentNullException(nameof(requestMessage));

            DeviceSession deviceSession;
            lock (_deviceSessions)
            {
                if (!_deviceSessions.TryGetValue(deviceSessionIdentifier.ToString(), out deviceSession))
                {
                    throw new DeviceSessionNotFoundException(deviceSessionIdentifier);
                }
            }
            
            requestMessage.CorrelationId = Guid.NewGuid().ToString();

            var resultAwaiter = new TaskCompletionSource<CloudMessage>();

            try
            {
                deviceSession.AddAwaiter(requestMessage.CorrelationId, resultAwaiter);

                using (cancellationToken.Register(() =>
                {
                    resultAwaiter.TrySetCanceled();
                }))
                {
                    await deviceSession.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false);
                    return await resultAwaiter.Task.ConfigureAwait(false);
                }
            }
            finally
            {
                deviceSession.RemoveAwaiter(requestMessage.CorrelationId);
            }
        }

        public async Task TryDispatchHttpRequestAsync(HttpContext httpContext)
        {
            if (httpContext == null) throw new ArgumentNullException(nameof(httpContext));

            try
            {
                (string username, string password) = ParseBasicAuthenticationHeader(httpContext.Request);
                if (username != null)
                {
                    await _authorizationService.AuthorizeUser(httpContext, username, password).ConfigureAwait(false);
                }

                var deviceSessionIdentifier = await _authorizationService.GetDeviceSessionIdentifier(httpContext).ConfigureAwait(false);
                if (deviceSessionIdentifier == null)
                {
                    httpContext.Response.Redirect("/Cloud/Account/Login");
                    return;
                }
                
                var requestMessage = await _cloudMessageFactory.Create(httpContext.Request).ConfigureAwait(false);
                var responseMessage = await Invoke(deviceSessionIdentifier, requestMessage, httpContext.RequestAborted).ConfigureAwait(false);

                var responseContent = _cloudMessageSerializer.Unpack<HttpResponseCloudMessageContent>(responseMessage.Payload);
                await PatchHttpResponseWithResponseFromDevice(httpContext.Response, responseContent).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                DefaultExceptionFilter.HandleException(exception, httpContext);
            }
        }

        (string username, string password) ParseBasicAuthenticationHeader(HttpRequest request)
        {
            if (!request.Headers.TryGetValue("Authorization", out var headerValues))
            {
                return (null, null);
            }
            
            var authenticationHeader = AuthenticationHeaderValue.Parse(headerValues);
            var credentialBytes = Convert.FromBase64String(authenticationHeader.Parameter);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(new[] { ':' }, 2);
            var username = credentials[0];
            var password = credentials[1];

            return (username, password);
        }

        async Task PatchHttpResponseWithResponseFromDevice(HttpResponse httpResponse, HttpResponseCloudMessageContent responseContent)
        {
            httpResponse.StatusCode = responseContent.StatusCode;

            if (responseContent.Headers?.Any() == true)
            {
                foreach (var header in responseContent.Headers)
                {
                    httpResponse.Headers.Add(header.Key, new StringValues(header.Value));
                }
            }

            if (responseContent.Content?.Length > 0)
            {
                await httpResponse.Body.WriteAsync(responseContent.Content).ConfigureAwait(false);
            }
        }
    }
}
