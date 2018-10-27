using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Wirehome.Core.Cloud.Messages;
using Wirehome.Core.Diagnostics;
using Wirehome.Core.MessageBus;
using Wirehome.Core.Model;
using Wirehome.Core.Python;
using Wirehome.Core.Storage;

namespace Wirehome.Core.Cloud
{
    public class CloudService
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private readonly StorageService _storageService;
        private readonly MessageBusService _messageBusService;
        private readonly ILogger _logger;
        private CloudServiceOptions _options;

        private Channel _channel;

        private bool _isConnected;

        public CloudService(StorageService storageService, MessageBusService messageBusService, SystemStatusService systemStatusService, ILoggerFactory loggerFactory)
        {
            _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));

            if (loggerFactory == null) throw new ArgumentNullException(nameof(loggerFactory));
            _logger = loggerFactory.CreateLogger<CloudService>();

            if (systemStatusService == null) throw new ArgumentNullException(nameof(systemStatusService));
            systemStatusService.Set("cloud.is_connected", () => _isConnected);
        }

        public void Start()
        {
            _storageService.TryReadOrCreate(out _options, "CloudServiceConfiguration.json");

            if (!_options.IsEnabled)
            {
                return;
            }

            _options.Host = "localhost:5001";

            Task.Run(() => ListenAsync(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
        }
        
        private async Task ListenAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    _isConnected = false;

                    using (var webSocketClient = new ClientWebSocket())
                    {
                        using (var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
                        {
                            var url = $"wss://{_options.Host}/Connector";
                            await webSocketClient.ConnectAsync(new Uri(url), timeout.Token).ConfigureAwait(false);
                        }

                        _channel = new Channel(webSocketClient);

                        var authorizeMessage = new AuthorizeCloudMessage
                        {
                            IdentityUid = _options.IdentityUid,
                            Password = _options.Password,
                            ChannelUid = _options.ChannelUid
                        };

                        await _channel.SendMessageAsync(authorizeMessage, cancellationToken).ConfigureAwait(false);

                        _isConnected = true;

                        while (_channel.IsConnected && !cancellationToken.IsCancellationRequested)
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

                            ProcessCloudMessage(receiveResult.Message);
                        }
                    }
                }
                catch (Exception exception)
                {
                    _isConnected = false;
                    _channel = null;

                    _logger.LogError(exception, "Error while connecting Cloud service.");

                    await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken).ConfigureAwait(false);
                }
            }
        }

        private void SendCloudMessage(string message)
        {
            //_channel.SendMessageAsync()
        }

        private void ProcessCloudMessage(JObject message)
        {
            var type = Convert.ToString(message["type"]);
            if (type == "wirehome.cloud.message.message_bus.publish")
            {
                _messageBusService.Publish((WirehomeDictionary)PythonConvert.ToPython(message["message"]));
            }
        }
    }
}
