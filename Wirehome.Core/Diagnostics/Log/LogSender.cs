using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using MessagePack;
using Wirehome.Core.Hardware.MQTT;

namespace Wirehome.Core.Diagnostics.Log;

public sealed class LogSender
{
    readonly Socket _logSender = new(SocketType.Dgram, ProtocolType.Udp);
    readonly MemoryStream _logSenderBuffer = new();
    readonly MqttService _mqttService;

    public LogSender(MqttService mqttService)
    {
        _mqttService = mqttService ?? throw new ArgumentNullException(nameof(mqttService));
    }

    public IPEndPoint UdpReceiverEndPoint { get; set; } = new(IPAddress.Parse("192.168.1.120"), 55521);

    public bool PublishToMqtt { get; set; }

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
            lock (_logSenderBuffer)
            {
                _logSenderBuffer.Seek(0, SeekOrigin.Begin);
                MessagePackSerializer.Serialize(_logSenderBuffer, logEntry);
                var buffer = _logSenderBuffer.GetBuffer();
                var bufferLength = (int)_logSenderBuffer.Length;
                
                if (logReceiverEndpoint != null)
                {
                   _logSender.SendTo(buffer, 0, bufferLength, SocketFlags.None, logReceiverEndpoint);
                }

                if (PublishToMqtt)
                {
                    var mqttPayload = new ArraySegment<byte>(buffer, 0, bufferLength).ToArray();
                    _mqttService.Publish(new MqttPublishParameters { Topic = "wirehome/log", Payload = mqttPayload});    
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