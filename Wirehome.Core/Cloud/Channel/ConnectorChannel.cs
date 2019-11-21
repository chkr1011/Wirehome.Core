using Microsoft.Extensions.Logging;
using MsgPack.Serialization;
using System;
using System.IO;
using System.IO.Compression;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Wirehome.Core.Cloud.Protocol;
using Wirehome.Core.Foundation;

namespace Wirehome.Core.Cloud.Channel
{
    public class ConnectorChannel
    {
        private const int MessageContentCompressionThreshold = 4096;

        private readonly MessagePackSerializer<CloudMessage> _serializer = MessagePackSerializer.Get<CloudMessage>();

        private readonly ArraySegment<byte> _receiveBuffer = WebSocket.CreateServerBuffer(4096);
        private readonly AsyncLock _lock = new AsyncLock();
        private readonly WebSocket _webSocket;
        private readonly ILogger _logger;
        private readonly DateTime _connected = DateTime.UtcNow;

        private long _bytesSent;
        private long _bytesReceived;
        private long _malformedMessagesReceived;
        private long _messagesSent;
        private long _messagesReceived;
        private long _receiveErrors;
        private long _sendErrors;
        private DateTime _statisticsReset = DateTime.UtcNow;
        private DateTime? _lastMessageSent;
        private DateTime? _lastMessageReceived;

        public ConnectorChannel(WebSocket webSocket, ILogger logger)
        {
            _webSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public bool IsConnected => _webSocket.State == WebSocketState.Open || _webSocket.State == WebSocketState.CloseReceived;

        public async Task SendMessageAsync(CloudMessage message, CancellationToken cancellationToken)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            try
            {
                if (message.Content?.Data.Count > MessageContentCompressionThreshold)
                {
                    message.Content.IsCompressed = true;
                    message.Content.Data = Compress(message.Content.Data);
                }

                var sendBuffer = await _serializer.PackSingleObjectAsBytesAsync(message, cancellationToken).ConfigureAwait(false);

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

        public async Task<ConnectorChannelReceiveResult> ReceiveMessageAsync(CancellationToken cancellationToken)
        {
            try
            {
                var buffer = await ReceiveMessageInternalAsync(cancellationToken).ConfigureAwait(false);
                if (buffer == null)
                {
                    return new ConnectorChannelReceiveResult(null, true);
                }

                Interlocked.Add(ref _bytesReceived, buffer.Count);
                Interlocked.Increment(ref _messagesReceived);
                _lastMessageReceived = DateTime.UtcNow;

                var cloudMessage = await TryParseCloudMessageAsync(buffer, cancellationToken).ConfigureAwait(false);
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

        private async Task<ArraySegment<byte>> ReceiveMessageInternalAsync(CancellationToken cancellationToken)
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
                return new ArraySegment<byte>(_receiveBuffer.Array, 0, receiveResult.Count);
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

        private async Task<CloudMessage> TryParseCloudMessageAsync(ArraySegment<byte> buffer, CancellationToken cancellationToken)
        {
            try
            {
                // Use a memory stream to avoid useless memory allocation because the MsgPack lib does
                // not support an ArraySegment.
                using (var memoryStream = new MemoryStream(buffer.Array, buffer.Offset, buffer.Count, false))
                {
                    var cloudMessage = await _serializer.UnpackAsync(memoryStream, cancellationToken).ConfigureAwait(false);
                    if (cloudMessage.Content?.IsCompressed == true)
                    {
                        cloudMessage.Content.IsCompressed = false;
                        cloudMessage.Content.Data = Decompress(cloudMessage.Content.Data);
                    }

                    return cloudMessage;
                }
            }
            catch (Exception exception)
            {
                Interlocked.Increment(ref _malformedMessagesReceived);

                _logger.LogWarning(exception, "Error while parsing cloud message.");

                return null;
            }
        }

        private static ArraySegment<byte> Compress(ArraySegment<byte> input)
        {
            using (var outputBuffer = new MemoryStream(input.Count / 2))
            {
                using (var inputBuffer = new MemoryStream(input.Array, input.Offset, input.Count))
                using (var compressor = new BrotliStream(outputBuffer, CompressionMode.Compress, true))
                {
                    inputBuffer.CopyTo(compressor);
                }

                return new ArraySegment<byte>(outputBuffer.GetBuffer(), 0, (int)outputBuffer.Length);
            }
        }

        private static ArraySegment<byte> Decompress(ArraySegment<byte> data)
        {
            using (var outputBuffer = new MemoryStream(data.Count * 2))
            {
                using (var inputBuffer = new MemoryStream(data.Array, data.Offset, data.Count))
                using (var decompressor = new BrotliStream(inputBuffer, CompressionMode.Decompress, false))
                {
                    decompressor.CopyTo(outputBuffer);
                }

                outputBuffer.Position = 0;
                return new ArraySegment<byte>(outputBuffer.GetBuffer(), 0, (int)outputBuffer.Length);
            }
        }
    }
}
