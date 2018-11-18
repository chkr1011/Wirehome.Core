using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using IronPython.Runtime;
using Microsoft.Extensions.Logging;
using Wirehome.Core.Cloud.Messages;
using Wirehome.Core.Constants;
using Wirehome.Core.Contracts;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.Extensions;
using Wirehome.Core.Model;
using Wirehome.Core.Python;
using Wirehome.Core.Python.Models;
using Wirehome.Core.Storage;

namespace Wirehome.Core.Cloud
{
    public class CloudService : IService
    {
        private readonly ConcurrentDictionary<string, CloudMessageHandler> _messageHandlers = new ConcurrentDictionary<string, CloudMessageHandler>();
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly CloudMessageFactory _messageFactory = new CloudMessageFactory();
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly StorageService _storageService;

        private readonly ILogger _logger;

        private CloudServiceOptions _options;
        private ConnectorChannel _channel;
        private bool _isConnected;

        public CloudService(StorageService storageService, SystemStatusService systemStatusService, ILoggerFactory loggerFactory)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));

            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<CloudService>();

            if (systemStatusService == null) throw new ArgumentNullException(nameof(systemStatusService));
            systemStatusService.Set("cloud.is_connected", () => _isConnected);

            _httpClient.BaseAddress = new Uri("http://127.0.0.1:80");
        }

        public void Start()
        {
            _storageService.TryReadOrCreate(out _options, "CloudServiceConfiguration.json");

            if (!_options.IsEnabled)
            {
                return;
            }

            Task.Run(() => ConnectAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token).Forget(_logger);
        }

        public void RegisterMessageHandler(string type, Func<WirehomeDictionary, WirehomeDictionary> handler)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            _messageHandlers[type] = new CloudMessageHandler(type, handler);
        }

        private async Task ConnectAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _isConnected = false;

                    if (string.IsNullOrEmpty(_options.IdentityUid) || string.IsNullOrEmpty(_options.Password))
                    {
                        continue;
                    }

                    using (var webSocketClient = new ClientWebSocket())
                    {
                        using (var timeout = new CancellationTokenSource(_options.ReconnectDelay))
                        {
                            var encodedIdentityUid = HttpUtility.UrlEncode(_options.IdentityUid);
                            var encodedChannelUid = HttpUtility.UrlEncode(_options.ChannelUid);
                            var encodedPassword = HttpUtility.UrlEncode(_options.Password);

                            var url = $"wss://{_options.Host}/Connectors/{encodedIdentityUid}/Channels/{encodedChannelUid}?password={encodedPassword}";
                            await webSocketClient.ConnectAsync(new Uri(url, UriKind.Absolute), timeout.Token).ConfigureAwait(false);
                        }

                        _channel = new ConnectorChannel(webSocketClient, _logger);
                        _logger.LogInformation($"Connected with Wirehome.Cloud at host '{_options.Host}'.");
                        _isConnected = true;

                        while (_channel.IsConnected && !cancellationToken.IsCancellationRequested)
                        {
                            var receiveResult = await _channel.ReceiveMessageAsync(cancellationToken).ConfigureAwait(false);
                            if (receiveResult.CloseConnection || cancellationToken.IsCancellationRequested)
                            {
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
                catch (Exception exception)
                {
                    _isConnected = false;
                    _channel = null;

                    _logger.LogError(exception, $"Error while connecting with Wirehome.Cloud service at host '{_options.Host}'.");
                }
                finally
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async Task TryProcessCloudMessageAsync(CloudMessage message, CancellationToken cancellationToken)
        {
            try
            {
                CloudMessage response;
                if (message.Type == CloudMessageType.Ping)
                {
                    response = InvokePingRequest(message);
                }
                else if (message.Type == CloudMessageType.HttpInvoke)
                {
                    response = await InvokeHttpRequestAsync(message, cancellationToken).ConfigureAwait(false);
                }
                else if (message.Type == CloudMessageType.Raw)
                {
                    response = InvokeRawRequest(message);
                }
                else
                {
                    response = _messageFactory.CreateMessage(ControlType.NotSupportedException, null);
                }

                await SendMessageAsync(response, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Error while processing cloud message.");
            }
        }

        private CloudMessage InvokePingRequest(CloudMessage message)
        {
            return _messageFactory.CreateResponseMessage(message, new PongCloudMessageContent
            {
                Timestamp = DateTime.Now.ToString("O"),
                Message = "pong",
                Version = WirehomeCoreVersion.Version
            });
        }

        private CloudMessage InvokeRawRequest(CloudMessage requestMessage)
        {
            try
            {
                object content;

                // TODO: Refactor this and build converter for JSON to WirehomeDictionary and WirehomeList
                if (!(PythonConvert.ToPython(requestMessage.Content) is PythonDictionary parameters))
                {
                    content = new WirehomeDictionary().WithType(ControlType.ParameterInvalidException);
                }
                else
                {
                    if (!_messageHandlers.TryGetValue(parameters.GetValueOr("type", string.Empty), out var messageHandler))
                    {
                        content = new WirehomeDictionary().WithType(ControlType.NotSupportedException);
                    }
                    else
                    {
                        content = messageHandler.Invoke(parameters);
                    }
                }

                return _messageFactory.CreateResponseMessage(requestMessage, content);
            }
            catch (Exception exception)
            {
                return _messageFactory.CreateMessage(ControlType.Exception, new ExceptionPythonModel(exception).ConvertToPythonDictionary());
            }
        }

        private async Task<CloudMessage> InvokeHttpRequestAsync(CloudMessage requestMessage, CancellationToken cancellationToken)
        {
            try
            {
                var responseContent = new HttpResponseMessageContent();
                responseContent.Headers.Add("Wirehome-Core-Enter", DateTime.UtcNow.ToString("O"));

                var requestContent = requestMessage.Content.ToObject<HttpRequestMessageContent>();

                using (var httpRequestMessage = new HttpRequestMessage())
                {
                    httpRequestMessage.Method = new HttpMethod(requestContent.Method);
                    httpRequestMessage.RequestUri = new Uri(requestContent.Uri, UriKind.Relative);
                    httpRequestMessage.Content = new ByteArrayContent(requestContent.Content ?? new byte[0]);

                    foreach (var header in requestContent.Headers)
                    {
                        if (!httpRequestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value))
                        {
                            httpRequestMessage.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                        }
                    }
                    
                    using (var httpResponse = await _httpClient.SendAsync(httpRequestMessage, cancellationToken).ConfigureAwait(false))
                    {
                        responseContent.StatusCode = (int)httpResponse.StatusCode;
                        responseContent.Content = await httpResponse.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

                        if (httpResponse.Content.Headers.ContentType != null)
                        {
                            responseContent.Headers.Add("Content-Type", httpResponse.Content.Headers.ContentType.ToString());
                        }
                    }
                }

                responseContent.Headers.Add("Wirehome-Core-Exit", DateTime.UtcNow.ToString("O"));
                return _messageFactory.CreateResponseMessage(requestMessage, responseContent);
            }
            catch (Exception exception)
            {
                return _messageFactory.CreateMessage(ControlType.Exception, new ExceptionPythonModel(exception).ConvertToPythonDictionary());
            }
        }

        private Task SendMessageAsync(CloudMessage message, CancellationToken cancellationToken)
        {
            var channel = _channel;
            if (channel == null)
            {
                return Task.CompletedTask;
            }

            return _channel.SendMessageAsync(message, cancellationToken);
        }
    }
}
