using MQTTnet.Protocol;

namespace Wirehome.Core.Hardware.MQTT;

public sealed class MqttImportTopicParameters
{
    public string ClientId { get; set; }

    public string Password { get; set; }

    public int? Port { get; set; }

    public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; } = MqttQualityOfServiceLevel.AtMostOnce;
    public string Server { get; set; }

    public string Topic { get; set; }

    public string Username { get; set; }

    public bool UseTls { get; set; }
}