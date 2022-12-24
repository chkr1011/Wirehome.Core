using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Wirehome.Core.Hardware.MQTT;

namespace Wirehome.Core.MessageBus;

public sealed class MessageBusSender
{
    readonly MemoryStream _buffer = new();
    readonly MqttService _mqttService;

    public MessageBusSender(MqttService mqttService)
    {
        _mqttService = mqttService ?? throw new ArgumentNullException(nameof(mqttService));
    }

    public bool PublishToMqtt { get; set; }

    public void TrySend(MessageBusMessage messageBusMessage)
    {
        if (!PublishToMqtt)
        {
            return;
        }

        if (messageBusMessage == null)
        {
            throw new ArgumentNullException(nameof(messageBusMessage));
        }

        try
        {
            lock (_buffer)
            {
                // _buffer.Seek(0, SeekOrigin.Begin);
                // _buffer.SetLength(0);
                //
                // MessagePackSerializer.Serialize(_buffer, messageBusMessage);
                // var buffer = _buffer.GetBuffer();
                // var bufferLength = (int)_buffer.Length;
                //
                // var mqttPayload = new ArraySegment<byte>(buffer, 0, bufferLength).ToArray();

                _mqttService.Publish(new MqttPublishParameters
                {
                    Topic = "wirehome/message_bus",
                    Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(messageBusMessage))
                });
            }
        }
        catch (Exception exception)
        {
            // Logging is not possible here!
            Console.Write(exception);
        }
    }
}