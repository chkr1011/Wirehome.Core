using System.Collections.Generic;

namespace Wirehome.Core.Hardware.MQTT;

public sealed class MqttServiceOptions
{
    public const string Filename = "MqttServiceConfiguration.json";

    public List<string> BlockedClients { get; set; }

    public List<BlockedMqttMessage> BlockedMessages { get; set; }

    public bool EnableLogging { get; set; }

    public bool PersistRetainedMessages { get; set; } = true;

    public int ServerPort { get; set; } = 1883;

    public string RemoteServerHost { get; set; }

    public int RemoteServerPort { get; set; } = 1883;
}