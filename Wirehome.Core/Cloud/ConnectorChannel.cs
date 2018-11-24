using System;
using System.IO;
using System.IO.Compression;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Wirehome.Core.Cloud.Messages;

namespace Wirehome.Core.Cloud
{
    public class ConnectorChannel
    {
        private readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            },
            Formatting = Formatting.None,
            DateParseHandling = DateParseHandling.None,
            TypeNameHandling = TypeNameHandling.All
        };

        private readonly JsonSerializer _serializer;

        private readonly ArraySegment<byte> _receiveBuffer = WebSocket.CreateServerBuffer(1024);
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly WebSocket _webSocket;
        private readonly ILogger _logger;

        public ConnectorChannel(WebSocket webSocket, ILogger logger)
        {
            _webSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _serializer = JsonSerializer.Create(_serializerSettings);
        }

        public bool IsConnected => _webSocket.State == WebSocketState.Open;

        public async Task SendMessageAsync(CloudMessage message, CancellationToken cancellationToken)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            using (var messageBuffer = new MemoryStream())
            using (var streamWriter = new StreamWriter(messageBuffer))
            {
                _serializer.Serialize(streamWriter, message);
                await streamWriter.FlushAsync().ConfigureAwait(false);
                messageBuffer.Position = 0;

                var compressedBuffer = Compress(messageBuffer);

                await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    var sendBuffer = new ArraySegment<byte>(compressedBuffer.GetBuffer(), 0, (int)compressedBuffer.Length);
                    await _webSocket.SendAsync(sendBuffer, WebSocketMessageType.Binary, true, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    _semaphore.Release();
                }
            }
        }

        public async Task<ConnectorChannelReceiveResult> ReceiveMessageAsync(CancellationToken cancellationToken)
        {
            using (var messageBuffer = new MemoryStream())
            {
                WebSocketReceiveResult result;
                do
                {
                    result = await _webSocket.ReceiveAsync(_receiveBuffer, cancellationToken).ConfigureAwait(false);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        return new ConnectorChannelReceiveResult(null, true);
                    }

                    if (result.Count > 0)
                    {
                        messageBuffer.Write(_receiveBuffer.Array, 0, result.Count);
                    }
                } while (!result.EndOfMessage && _webSocket.State == WebSocketState.Open);

                if (_webSocket.State != WebSocketState.Open)
                {
                    return new ConnectorChannelReceiveResult(null, true);
                }

                messageBuffer.Position = 0;
                var decompressedBuffer = Decompress(messageBuffer);

                if (!TryParseCloudMessage(decompressedBuffer, out var cloudMessage))
                {
                    return new ConnectorChannelReceiveResult(null, true);
                }

                return new ConnectorChannelReceiveResult(cloudMessage, false);
            }
        }

        private bool TryParseCloudMessage(Stream buffer, out CloudMessage cloudMessage)
        {
            try
            {
                using (var streamReader = new StreamReader(buffer, Encoding.UTF8))
                {
                    cloudMessage = _serializer.Deserialize(streamReader, typeof(CloudMessage)) as CloudMessage;
                    return cloudMessage != null;
                }
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Error while parsing cloud message.");

                cloudMessage = null;
                return false;
            }
        }

        public Task CloseAsync()
        {
            if (_webSocket.State == WebSocketState.Open)
            {
                return _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
            }

            return Task.CompletedTask;
        }

        private static MemoryStream Compress(Stream data)
        {
            var result = new MemoryStream((int)data.Length / 2);

            using (var gzipStream = new GZipStream(result, CompressionLevel.Optimal, true))
            {
                data.CopyTo(gzipStream);
                gzipStream.Flush();
            }

            result.Position = 0;
            return result;
        }

        private static MemoryStream Decompress(Stream data)
        {
            var result = new MemoryStream((int)data.Length);

            using (var gzipStream = new GZipStream(data, CompressionMode.Decompress, true))
            {
                gzipStream.CopyTo(result);
            }

            result.Position = 0;
            return result;
        }
    }
}
