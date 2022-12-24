using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using MessagePack;
using Wirehome.Core.Hardware.MQTT;

namespace Wirehome.Core.Diagnostics.Log;

public sealed class LogSender
{
    readonly MemoryStream _buffer = new();
    readonly MqttService _mqttService;
    readonly Socket _udpSender = new(SocketType.Dgram, ProtocolType.Udp);

    public LogSender(MqttService mqttService)
    {
        _mqttService = mqttService ?? throw new ArgumentNullException(nameof(mqttService));
    }

    public bool PublishToMqtt { get; set; }

    public IPEndPoint UdpReceiverEndPoint { get; set; } = new(IPAddress.Parse("192.168.1.120"), 55521);

    public void TrySend(LogEntry logEntry)
    {
        if (logEntry == null)
        {
            throw new ArgumentNullException(nameof(logEntry));
        }

        var logReceiverEndpoint = UdpReceiverEndPoint;
        if (logReceiverEndpoint == null && !PublishToMqtt)
        {
            return;
        }

        try
        {
            lock (_buffer)
            {
                _buffer.Seek(0, SeekOrigin.Begin);
                _buffer.SetLength(0);

                MessagePackSerializer.Serialize(_buffer, logEntry);
                var buffer = _buffer.GetBuffer();
                var bufferLength = (int)_buffer.Length;

                if (logReceiverEndpoint != null)
                {
                    _udpSender.SendTo(buffer, 0, bufferLength, SocketFlags.None, logReceiverEndpoint);
                }

                if (PublishToMqtt)
                {
                    var mqttPayload = new ArraySegment<byte>(buffer, 0, bufferLength).ToArray();
                    _mqttService.Publish(new MqttPublishParameters
                    {
                        Topic = "wirehome/log",
                        Payload = mqttPayload
                    });
                }
            }
        }
        catch (Exception exception)
        {
            // Logging is not possible here!
            Console.Write(exception);
        }
    }
}