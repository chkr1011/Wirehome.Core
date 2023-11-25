using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Wirehome.Cloud.Filters;
using Wirehome.Cloud.Services.Authorization;
using Wirehome.Core.Cloud.Channel;
using Wirehome.Core.Cloud.Protocol;
using Wirehome.Core.Cloud.Protocol.Content;

namespace Wirehome.Cloud.Services.DeviceConnector;

public class DeviceConnectorService
{
    readonly AuthorizationService _authorizationService;
    readonly CloudMessageFactory _cloudMessageFactory;
    readonly CloudMessageSerializer _cloudMessageSerializer;
    readonly ILogger _logger;
    readonly Dictionary<ChannelIdentifier, OpenChannel> _openChannels = new();

    public DeviceConnectorService(AuthorizationService authorizationService,
        CloudMessageFactory cloudMessageFactory,
        CloudMessageSerializer cloudMessageSerializer,
        ILogger<DeviceConnectorService> logger)
    {
        _authorizationService = authorizationService ?? throw new ArgumentNullException(nameof(authorizationService));
        _cloudMessageFactory = cloudMessageFactory ?? throw new ArgumentNullException(nameof(cloudMessageFactory));
        _cloudMessageSerializer = cloudMessageSerializer ?? throw new ArgumentNullException(nameof(cloudMessageSerializer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ConnectorChannelStatistics GetChannelStatistics(ChannelIdentifier channelIdentifier)
    {
        lock (_openChannels)
        {
            if (!_openChannels.TryGetValue(channelIdentifier, out var deviceSession))
            {
                return null;
            }

            return deviceSession.GetStatistics();
        }
    }

    public object GetStatistics()
    {
        lock (_openChannels)
        {
            return new
            {
                connectedDevicesCount = _openChannels.Count,
                connectedDevices = _openChannels.Keys
            };
        }
    }

    public async Task<CloudMessage> Invoke(ChannelIdentifier channelIdentifier, CloudMessage requestMessage, CancellationToken cancellationToken)
    {
        if (channelIdentifier == null)
        {
            throw new ArgumentNullException(nameof(channelIdentifier));
        }

        if (requestMessage == null)
        {
            throw new ArgumentNullException(nameof(requestMessage));
        }

        OpenChannel deviceSession;
        lock (_openChannels)
        {
            if (!_openChannels.TryGetValue(channelIdentifier, out deviceSession))
            {
                throw new OpenChannelNotFoundException(channelIdentifier);
            }
        }

        requestMessage.CorrelationId = Guid.NewGuid().ToString();

        var resultAwaiter = new TaskCompletionSource<CloudMessage>();

        try
        {
            deviceSession.AddAwaiter(requestMessage.CorrelationId, resultAwaiter);

            using (cancellationToken.Register(() => { resultAwaiter.TrySetCanceled(); }))
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

    public void ResetChannelStatistics(ChannelIdentifier channelIdentifier)
    {
        lock (_openChannels)
        {
            if (!_openChannels.TryGetValue(channelIdentifier, out var deviceSession))
            {
                return;
            }

            deviceSession.ResetStatistics();
        }
    }

    public async Task RunAsync(ChannelIdentifier channelIdentifier, WebSocket webSocket, CancellationToken cancellationToken)
    {
        if (webSocket == null)
        {
            throw new ArgumentNullException(nameof(webSocket));
        }

        var channelOptions = new ConnectorChannelOptions
        {
            UseCompression = true
        };

        var channel = new ConnectorChannel(channelOptions, webSocket, _cloudMessageSerializer, _logger);
        try
        {
            var openChannel = new OpenChannel(channelIdentifier, channel, _logger);
            lock (_openChannels)
            {
                _openChannels[channelIdentifier] = openChannel;
            }

            await openChannel.RunAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            lock (_openChannels)
            {
                _openChannels.Remove(channelIdentifier);
            }
        }
    }

    public async Task TryDispatchHttpRequestAsync(HttpContext httpContext)
    {
        if (httpContext == null)
        {
            throw new ArgumentNullException(nameof(httpContext));
        }

        try
        {
            var (username, password) = ParseBasicAuthenticationHeader(httpContext.Request);
            if (username != null)
            {
                await _authorizationService.AuthorizeUser(httpContext, username, password).ConfigureAwait(false);
            }

            var channelIdentifier = await _authorizationService.GetChannelIdentifier(httpContext).ConfigureAwait(false);
            if (channelIdentifier == null)
            {
                httpContext.Response.Redirect("/Cloud/Account/Login");
                return;
            }

            var requestMessage = await _cloudMessageFactory.Create(httpContext.Request).ConfigureAwait(false);
            var responseMessage = await Invoke(channelIdentifier, requestMessage, httpContext.RequestAborted).ConfigureAwait(false);

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
        var credentials = Encoding.UTF8.GetString(credentialBytes).Split(new[]
        {
            ':'
        }, 2);
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
                httpResponse.Headers[header.Key] = new StringValues(header.Value);
            }
        }

        if (responseContent.Content?.Length > 0)
        {
            await httpResponse.Body.WriteAsync(responseContent.Content).ConfigureAwait(false);
        }
    }
}