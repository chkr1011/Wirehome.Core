namespace Wirehome.Core.Hardware.MQTT;

public sealed class BlockedMqttMessage
{
    public string Payload { get; set; }

    public string Topic { get; set; }
}