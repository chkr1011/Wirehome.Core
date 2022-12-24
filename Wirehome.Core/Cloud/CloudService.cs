using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Wirehome.Core.Cloud.Channel;
using Wirehome.Core.Cloud.Protocol;
using Wirehome.Core.Cloud.Protocol.Content;
using Wirehome.Core.Contracts;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.Extensions;
using Wirehome.Core.Storage;
using Wirehome.Core.System;

namespace Wirehome.Core.Cloud;

public sealed class CloudService : WirehomeCoreService
{
    readonly CloudMessageFactory _cloudMessageFactory;
    readonly CloudMessageSerializer _cloudMessageSerializer = new();
    readonly HttpClient _httpClient = new();

    readonly ILogger _logger;
    readonly Dictionary<string, RawCloudMessageHandler> _rawMessageHandlers = new();

    readonly StorageService _storageService;
    readonly SystemCancellationToken _systemCancellationToken;
    ConnectorChannel _channel;
    Exception _connectionError;
    bool _isConnected;

    CloudServiceConfiguration _options;

    public CloudService(StorageService storageService, SystemCancellationToken systemCancellationToken, SystemStatusService systemStatusService, ILogger<CloudService> logger)
    {
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _systemCancellationToken = systemCancellationToken ?? throw new ArgumentNullException(nameof(systemCancellationToken));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (systemStatusService is null)
        {
            throw new ArgumentNullException(nameof(systemStatusService));
        }

        systemStatusService.Set("cloud.is_connected", () => _isConnected);
        systemStatusService.Set("cloud.connection_error", () => _connectionError?.ToString());
        systemStatusService.Set("cloud.bytes_sent", () => _channel?.GetStatistics()?.BytesSent);
        systemStatusService.Set("cloud.bytes_received", () => _channel?.GetStatistics()?.BytesReceived);
        systemStatusService.Set("cloud.connected", () => _channel?.GetStatistics()?.Connected.ToString("O"));
        systemStatusService.Set("cloud.last_message_received", () => _channel?.GetStatistics()?.LastMessageReceived?.ToString("O"));
        systemStatusService.Set("cloud.last_message_sent", () => _channel?.GetStatistics()?.LastMessageSent?.ToString("O"));
        systemStatusService.Set("cloud.messages_received", () => _channel?.GetStatistics()?.MessagesReceived);
        systemStatusService.Set("cloud.messages_sent", () => _channel?.GetStatistics()?.MessagesSent);
        systemStatusService.Set("cloud.malformed_messages_received", () => _channel?.GetStatistics()?.MalformedMessagesReceived);
        systemStatusService.Set("cloud.receive_errors", () => _channel?.GetStatistics()?.ReceiveErrors);
        systemStatusService.Set("cloud.send_errors", () => _channel?.GetStatistics()?.SendErrors);

        _httpClient.BaseAddress = new Uri("http://127.0.0.1:80");
        // Disable compression for loopback connections
        _httpClient.DefaultRequestHeaders.AcceptEncoding.TryParseAdd("*;q=0");

        _cloudMessageFactory = new CloudMessageFactory(_cloudMessageSerializer);
    }

    public void RegisterMessageHandler(string type, Func<IDictionary<object, object>, IDictionary<object, object>> handler)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (handler == null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        lock (_rawMessageHandlers)
        {
            _rawMessageHandlers[type] = new RawCloudMessageHandler(type, handler);
        }
    }

    protected override void OnStart()
    {
        Task.Run(() => ConnectAsync(_systemCancellationToken.Token), _systemCancellationToken.Token).Forget(_logger);
    }

    async Task ConnectAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                _isConnected = false;

                if (!_storageService.SafeReadSerializedValue(out _options, DefaultDirectoryNames.Configuration, CloudServiceConfiguration.Filename) || _options == null)
                {
                    continue;
                }

                if (!_options.IsEnabled)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(_options.ChannelAccessToken))
                {
                    continue;
                }

                using (var webSocketClient = new ClientWebSocket())
                {
                    using (var timeout = new CancellationTokenSource(_options.ReconnectDelay))
                    {
                        var url = $"wss://{_options.Host}/Connector";

                        webSocketClient.Options.SetRequestHeader(CloudHeaderNames.ChannelAccessToken, _options.ChannelAccessToken);

                        webSocketClient.Options.SetRequestHeader(CloudHeaderNames.Version, WirehomeCoreVersion.Version);

                        await webSocketClient.ConnectAsync(new Uri(url, UriKind.Absolute), timeout.Token).ConfigureAwait(false);
                    }

                    var channelOptions = new ConnectorChannelOptions
                    {
                        UseCompression = _options.UseCompression
                    };

                    _channel = new ConnectorChannel(channelOptions, webSocketClient, _cloudMessageSerializer, _logger);
                    _isConnected = true;
                    _connectionError = null;
                    _logger.LogInformation($"Connected with Wirehome.Cloud at host '{_options.Host}'.");

                    while (_channel.IsConnected && !cancellationToken.IsCancellationRequested)
                    {
                        var receiveResult = await _channel.ReceiveAsync(cancellationToken).ConfigureAwait(false);
                        if (receiveResult.CloseConnection || cancellationToken.IsCancellationRequested)
                        {
                            webSocketClient.Abort();
                            break;
                        }

                        if (receiveResult.Message == null)
                        {
                            continue;
                        }

                        ParallelTask.Start(() => TryProcessCloudMessageAsync(receiveResult.Message, cancellationToken), cancellationToken, _logger);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _connectionError = exception;

                _logger.LogError(exception, $"Error while connecting with Wirehome.Cloud service at host '{_options?.Host}'.");
            }
            finally
            {
                _isConnected = false;
                _channel = null;

                var delay = TimeSpan.FromSeconds(10);
                if (_options != null && _options.ReconnectDelay > TimeSpan.Zero)
                {
                    delay = _options.ReconnectDelay;
                }

                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    async Task<CloudMessage> InvokeHttpRequestAsync(CloudMessage requestMessage, CancellationToken cancellationToken)
    {
        try
        {
            var requestContent = _cloudMessageSerializer.Unpack<HttpRequestCloudMessageContent>(requestMessage.Payload);

            using (var httpRequestMessage = new HttpRequestMessage())
            {
                httpRequestMessage.Method = new HttpMethod(requestContent.Method);
                httpRequestMessage.RequestUri = new Uri(requestContent.Uri, UriKind.Relative);

                if (requestContent.Content.Count > 0)
                {
                    httpRequestMessage.Content = new ByteArrayContent(requestContent.Content.Array, requestContent.Content.Offset, requestContent.Content.Count);
                }

                if (requestContent.Headers?.Any() == true)
                {
                    foreach (var (key, value) in requestContent.Headers)
                    {
                        if (!httpRequestMessage.Headers.TryAddWithoutValidation(key, value))
                        {
                            httpRequestMessage.Content.Headers.TryAddWithoutValidation(key, value);
                        }
                    }
                }

                using (var httpResponse = await _httpClient.SendAsync(httpRequestMessage, cancellationToken).ConfigureAwait(false))
                {
                    return await _cloudMessageFactory.Create(httpResponse, requestMessage.CorrelationId).ConfigureAwait(false);
                }
            }
        }
        catch (Exception exception)
        {
            return _cloudMessageFactory.Create(exception);
        }
    }

    CloudMessage InvokeRawRequest(CloudMessage requestMessage)
    {
        try
        {
            var jsonRequestPayload = Encoding.UTF8.GetString(requestMessage.Payload);
            var invokeParameter = JsonConvert.DeserializeObject<IDictionary<object, object>>(jsonRequestPayload);

            if (!invokeParameter.TryGetValue("type", out var handlerType))
            {
                throw new NotSupportedException("Mandatory key 'type' not found in request parameter.");
            }

            RawCloudMessageHandler cloudMessageHandler;
            lock (_rawMessageHandlers)
            {
                if (!_rawMessageHandlers.TryGetValue(Convert.ToString(handlerType), out cloudMessageHandler))
                {
                    throw new NotSupportedException($"RAW message handler '{handlerType}' not supported.");
                }
            }

            var invokeResult = cloudMessageHandler.Invoke(invokeParameter);

            var jsonResponse = JsonConvert.SerializeObject(invokeResult);

            return new CloudMessage
            {
                CorrelationId = requestMessage.CorrelationId,
                Payload = Encoding.UTF8.GetBytes(jsonResponse)
            };
        }
        catch (Exception exception)
        {
            return _cloudMessageFactory.Create(exception);
        }
    }

    Task SendMessageAsync(CloudMessage message, CancellationToken cancellationToken)
    {
        var channel = _channel;
        if (channel == null)
        {
            return Task.CompletedTask;
        }

        return _channel.SendAsync(message, cancellationToken);
    }

    async Task TryProcessCloudMessageAsync(CloudMessage requestMessage, CancellationToken cancellationToken)
    {
        try
        {
            CloudMessage responseMessage = null;
            if (requestMessage.Type == CloudMessageType.Ping)
            {
                responseMessage = new CloudMessage();
            }
            else if (requestMessage.Type == CloudMessageType.HttpInvoke)
            {
                responseMessage = await InvokeHttpRequestAsync(requestMessage, cancellationToken).ConfigureAwait(false);
            }
            else if (requestMessage.Type == CloudMessageType.Raw)
            {
                responseMessage = InvokeRawRequest(requestMessage);
            }

            if (responseMessage != null)
            {
                responseMessage.CorrelationId = requestMessage.CorrelationId;
                await SendMessageAsync(responseMessage, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error while processing cloud message.");
        }
    }
}