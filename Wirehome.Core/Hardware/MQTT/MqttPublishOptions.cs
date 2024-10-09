using MQTTnet.Protocol;

namespace Wirehome.Core.Hardware.MQTT;

public sealed class MqttPublishOptions
{
    public byte[] Payload { get; set; } = [];

    public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; } = MqttQualityOfServiceLevel.AtMostOnce;

    public bool Retain { get; set; }

    public string Topic { get; set; }
}