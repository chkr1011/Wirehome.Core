using MQTTnet;

namespace Wirehome.Core.Hardware.MQTT;

public sealed class IncomingMqttMessage
{
    public string ClientId { get; set; }
        
    public MqttApplicationMessage ApplicationMessage { get; set; }
}