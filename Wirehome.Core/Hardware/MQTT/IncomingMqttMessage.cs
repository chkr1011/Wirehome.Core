using MQTTnet;

namespace Wirehome.Core.Hardware.MQTT;

public sealed class IncomingMqttMessage
{
    public IncomingMqttMessage(string clientId, MqttApplicationMessage applicationMessage)
    {
        ClientId = clientId;
        ApplicationMessage = applicationMessage;
    }

    public string ClientId { get; }
        
    public MqttApplicationMessage ApplicationMessage { get; }
}