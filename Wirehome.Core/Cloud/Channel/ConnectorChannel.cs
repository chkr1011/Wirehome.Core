using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Wirehome.Core.Cloud.Protocol;
using Wirehome.Core.Foundation;

namespace Wirehome.Core.Cloud.Channel
{
    public sealed class ConnectorChannel : IDisposable
    {
        const int MessageContentCompressionThreshold = 4096;
        const int ReceiveBufferSize = 1024 * 1024; // 1 MB

        readonly ArraySegment<byte> _receiveBuffer = new byte[ReceiveBufferSize];
        readonly AsyncLock _lock = new AsyncLock();
        readonly ConnectorChannelOptions _options;
        readonly WebSocket _webSocket;
        readonly CloudMessageSerializer _cloudMessageSerializer;
        readonly ILogger _logger;
        readonly DateTime _connected = DateTime.UtcNow;

        long _bytesSent;
        long _bytesReceived;
        long _malformedMessagesReceived;
        long _messagesSent;
        long _messagesReceived;
        long _receiveErrors;
        long _sendErrors;
        DateTime _statisticsReset = DateTime.UtcNow;
        DateTime? _lastMessageSent;
        DateTime? _lastMessageReceived;

        public ConnectorChannel(ConnectorChannelOptions options, WebSocket webSocket, CloudMessageSerializer cloudMessageSerializer, ILogger logger)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _webSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));
            _cloudMessageSerializer = cloudMessageSerializer ?? throw new ArgumentNullException(nameof(cloudMessageSerializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool IsConnected => _webSocket.State == WebSocketState.Open || _webSocket.State == WebSocketState.CloseReceived;

        public async Task SendAsync(CloudMessage cloudMessage, CancellationToken cancellationToken)
        {
            if (cloudMessage == null) throw new ArgumentNullException(nameof(cloudMessage));

            try
            {
                var transportCloudMessage = CreateTransportCloudMessage(cloudMessage);
                var sendBuffer = _cloudMessageSerializer.Pack(transportCloudMessage);

                await _lock.EnterAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    await _webSocket.SendAsync(sendBuffer, WebSocketMessageType.Binary, true, cancellationToken).ConfigureAwait(false);

                    Interlocked.Add(ref _bytesSent, sendBuffer.Count);
                    Interlocked.Increment(ref _messagesSent);
                    _lastMessageSent = DateTime.UtcNow;
                }
                finally
                {
                    _lock.Exit();
                }
            }
            catch
            {
                Interlocked.Increment(ref _sendErrors);

                throw;
            }
        }

        public async Task<ConnectorChannelReceiveResult> ReceiveAsync(CancellationToken cancellationToken)
        {
            try
            {
                var buffer = await ReceiveInternalAsync(cancellationToken).ConfigureAwait(false);
                if (buffer == null)
                {
                    return new ConnectorChannelReceiveResult(null, true);
                }

                Interlocked.Add(ref _bytesReceived, buffer.Count);
                Interlocked.Increment(ref _messagesReceived);
                _lastMessageReceived = DateTime.UtcNow;

                var cloudMessage = TryParseCloudMessage(buffer);
                if (cloudMessage == null)
                {
                    return new ConnectorChannelReceiveResult(null, true);
                }

                return new ConnectorChannelReceiveResult(cloudMessage, false);
            }
            catch
            {
                Interlocked.Increment(ref _receiveErrors);

                throw;
            }
        }

        public ConnectorChannelStatistics GetStatistics()
        {
            return new ConnectorChannelStatistics
            {
                BytesReceived = Interlocked.Read(ref _bytesReceived),
                BytesSent = Interlocked.Read(ref _bytesSent),
                MalformedMessagesReceived = Interlocked.Read(ref _malformedMessagesReceived),
                MessagesReceived = Interlocked.Read(ref _messagesReceived),
                MessagesSent = Interlocked.Read(ref _messagesSent),
                ReceiveErrors = Interlocked.Read(ref _receiveErrors),
                SendErrors = Interlocked.Read(ref _sendErrors),
                LastMessageReceived = _lastMessageReceived,
                LastMessageSent = _lastMessageSent,
                StatisticsReset = _statisticsReset,
                Connected = _connected,
                UpTime = DateTime.UtcNow - _connected
            };
        }

        public void ResetStatistics()
        {
            Interlocked.Exchange(ref _bytesReceived, 0);
            Interlocked.Exchange(ref _bytesSent, 0);
            Interlocked.Exchange(ref _malformedMessagesReceived, 0);
            Interlocked.Exchange(ref _messagesReceived, 0);
            Interlocked.Exchange(ref _messagesSent, 0);
            Interlocked.Exchange(ref _receiveErrors, 0);
            Interlocked.Exchange(ref _sendErrors, 0);
            _lastMessageReceived = null;
            _lastMessageSent = null;
            _statisticsReset = DateTime.UtcNow;
        }


        public void Dispose()
        {
            _lock.Dispose();
            _webSocket.Dispose();
        }

        async Task<ArraySegment<byte>> ReceiveInternalAsync(CancellationToken cancellationToken)
        {
            if (_webSocket.State != WebSocketState.Open && _webSocket.State != WebSocketState.CloseReceived)
            {
                return null;
            }

            var receiveResult = await _webSocket.ReceiveAsync(_receiveBuffer, cancellationToken).ConfigureAwait(false);

            if (receiveResult.MessageType == WebSocketMessageType.Close)
            {
                return null;
            }

            if (receiveResult.EndOfMessage)
            {
                // The entire message fits into one receive buffer. So there is no need for
                // another MemoryStream which copies all the memory.
                return _receiveBuffer.Slice(0, receiveResult.Count);
            }

            using (var buffer = new MemoryStream(receiveResult.Count * 2))
            {
                // Write the already received part to the buffer. The buffer will be extended later with additional data.
                await buffer.WriteAsync(_receiveBuffer.Array, 0, receiveResult.Count, cancellationToken).ConfigureAwait(false);

                while (!receiveResult.EndOfMessage && (_webSocket.State == WebSocketState.Open || _webSocket.State == WebSocketState.CloseReceived))
                {
                    receiveResult = await _webSocket.ReceiveAsync(_receiveBuffer, cancellationToken).ConfigureAwait(false);

                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        return null;
                    }

                    await buffer.WriteAsync(_receiveBuffer.Array, 0, receiveResult.Count, cancellationToken).ConfigureAwait(false);
                }

                return new ArraySegment<byte>(buffer.GetBuffer(), 0, (int)buffer.Length);
            }
        }

        CloudMessage TryParseCloudMessage(ArraySegment<byte> buffer)
        {
            try
            {
                var transportCloudMessage = _cloudMessageSerializer.Unpack<TransportCloudMessage>(buffer);
                if (transportCloudMessage.PayloadIsCompressed == true)
                {
                    transportCloudMessage.Payload = _cloudMessageSerializer.Decompress(transportCloudMessage.Payload);
                }

                return new CloudMessage
                {
                    Type = transportCloudMessage.Type,
                    CorrelationId = transportCloudMessage.CorrelationId,
                    Payload = transportCloudMessage.Payload,
                    Properties = transportCloudMessage.Properties
                };
            }
            catch (Exception exception)
            {
                Interlocked.Increment(ref _malformedMessagesReceived);

                _logger.LogWarning(exception, "Error while parsing cloud message.");

                return null;
            }
        }

        TransportCloudMessage CreateTransportCloudMessage(CloudMessage cloudMessage)
        {
            var transportCloudMessage = new TransportCloudMessage()
            {
                Type = cloudMessage.Type,
                CorrelationId = cloudMessage.CorrelationId,
                Payload = cloudMessage.Payload,
                PayloadIsCompressed = false,
                Properties = cloudMessage.Properties
            };

            if (_options.UseCompression)
            {
                if (transportCloudMessage.Payload.Count > MessageContentCompressionThreshold)
                {
                    transportCloudMessage.PayloadIsCompressed = true;
                    transportCloudMessage.Payload = _cloudMessageSerializer.Compress(cloudMessage.Payload);
                }
            }

            return transportCloudMessage;
        }
    }
}
