namespace Wirehome.Core.Hardware.MQTT
{
    public class MqttServiceSettings
    {
        public bool EnableLogging { get; set; } = false;

        public int ServerPort { get; set; } = 1883;
    }
}
