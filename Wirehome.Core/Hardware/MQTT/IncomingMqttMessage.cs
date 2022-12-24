using MQTTnet;

namespace Wirehome.Core.Hardware.MQTT;

public sealed class IncomingMqttMessage
{
    public IncomingMqttMessage(string clientId, MqttApplicationMessage applicationMessage)
    {
        ClientId = clientId;
        ApplicationMessage = applicationMessage;
    }

    public MqttApplicationMessage ApplicationMessage { get; }

    public string ClientId { get; }
}