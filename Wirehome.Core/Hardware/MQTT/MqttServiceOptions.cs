namespace Wirehome.Core.Hardware.MQTT
{
    public class MqttServiceOptions
    {
        public bool EnableLogging { get; set; } = false;

        public bool PersistRetainedMessages { get; set; } = true;

        public int ServerPort { get; set; } = 1883;
    }
}
