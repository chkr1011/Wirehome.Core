using MQTTnet.Protocol;
using System;

namespace Wirehome.Core.Hardware.MQTT
{
    public sealed class MqttPublishParameters
    {
        public string Topic { get; set; }

        public byte[] Payload { get; set; } = Array.Empty<byte>();

        public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; } = MqttQualityOfServiceLevel.AtMostOnce;

        public bool Retain { get; set; }
    }
}
