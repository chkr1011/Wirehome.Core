using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Server;

namespace Wirehome.Core.Hardware.MQTT;

public interface IMqttBackend
{
    event EventHandler<InterceptingPublishEventArgs> MessageReceived;

    Task DeleteRetainedMessagesAsync();

    Task<IList<MqttClientStatus>> GetClientsAsync();

    Task<int> GetConnectedClientsCount();

    Task<IList<MqttApplicationMessage>> GetRetainedMessagesAsync();

    Task<IList<MqttSessionStatus>> GetSessionsAsync();

    void Initialize();

    Task Publish(InjectedMqttApplicationMessage applicationMessage);

    Task StartAsync();

    Task StopAsync();
}