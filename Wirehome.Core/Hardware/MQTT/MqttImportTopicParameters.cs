using MQTTnet.Protocol;

namespace Wirehome.Core.Hardware.MQTT
{
    public class MqttImportTopicParameters
    {
        public string Server { get; set; }

        public int? Port { get; set; }

        public string Topic { get; set; }

        public MqttQualityOfServiceLevel QualityOfServiceLevel { get; set; } = MqttQualityOfServiceLevel.AtMostOnce;
    }
}
