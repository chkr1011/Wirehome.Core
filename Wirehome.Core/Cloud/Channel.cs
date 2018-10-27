using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Wirehome.Core.Extensions;

namespace Wirehome.Core.Cloud
{
    public class Channel
    {
        private readonly JsonSerializerSettings _serializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy()
            },
            Formatting = Formatting.None
        };

        private readonly WebSocket _webSocket;

        public Channel(WebSocket webSocket)
        {
            _webSocket = webSocket ?? throw new ArgumentNullException(nameof(webSocket));
        }

        public bool IsConnected => _webSocket.State == WebSocketState.Open;
        
        public Task SendMessageAsync<TMessage>(TMessage message, CancellationToken cancellationToken) where TMessage : class
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var json = JsonConvert.SerializeObject(message, _serializerSettings);

            return SendMessageAsync(json, cancellationToken);
        }

        public Task SendMessageAsync(string message, CancellationToken cancellationToken)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));

            var buffer = Encoding.UTF8.GetBytes(message);
            return _webSocket.SendAsync(buffer.AsArraySegment(), WebSocketMessageType.Text, true, cancellationToken);
        }

        public async Task<ChannelReceiveResult> ReceiveMessageAsync(CancellationToken cancellationToken)
        {
            var messageBuffer = new MemoryStream();

            ValueWebSocketReceiveResult result;
            var buffer = new Memory<byte>(new byte[1024]);

            do
            {
                result = await _webSocket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    return new ChannelReceiveResult(null, true);
                }

                if (result.Count > 0)
                {
                    messageBuffer.Write(buffer.Span);
                }

            } while (!result.EndOfMessage && _webSocket.State == WebSocketState.Open);

            if (_webSocket.State != WebSocketState.Open)
            {
                return new ChannelReceiveResult(null, true);
            }

            var json = Encoding.UTF8.GetString(messageBuffer.GetBuffer(), 0, (int)messageBuffer.Length);
            if (!TryParseJson(json, out var message))
            {
                return null;
            }

            return new ChannelReceiveResult(message, false);
        }

        private bool TryParseJson(string json, out JObject message)
        {
            try
            {
                message = JObject.Parse(json, new JsonLoadSettings());
                return true;
            }
            catch (Exception exception)
            {
                message = null;
                return false;
            }
        }

        public async Task CloseAsync()
        {
            if (_webSocket.State == WebSocketState.Open)
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }
        }
    }
}
