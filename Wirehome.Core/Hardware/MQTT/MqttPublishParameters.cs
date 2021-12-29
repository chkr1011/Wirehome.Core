using System;
using MQTTnet.Protocol;

namespace Wirehome.Core.Hardware.MQTT
{
    public sealed class MqttPublishParameters
    {
        public byte[] Payload { get; set; } = Array.Empty<byte>();

        public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; } = MqttQualityOfServiceLevel.AtMostOnce;

        public bool Retain { get; set; }
        public string Topic { get; set; }
    }
}