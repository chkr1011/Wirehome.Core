using System;
using MQTTnet.Server;

namespace Wirehome.Core.Hardware.MQTT
{
    public sealed class MqttSubscriber
    {
        readonly Action<IncomingMqttMessage> _callback;

        public MqttSubscriber(string uid, string topicFilter, Action<IncomingMqttMessage> callback)
        {
            Uid = uid ?? throw new ArgumentNullException(nameof(uid));
            TopicFilter = topicFilter ?? throw new ArgumentNullException(nameof(topicFilter));
            _callback = callback ?? throw new ArgumentNullException(nameof(callback));
        }

        public string TopicFilter { get; }

        public string Uid { get; }

        public bool IsFilterMatch(string topic)
        {
            if (topic == null)
            {
                throw new ArgumentNullException(nameof(topic));
            }

            return MqttTopicFilterComparer.Compare(topic, TopicFilter) == MqttTopicFilterCompareResult.IsMatch;
        }

        public void Notify(IncomingMqttMessage incomingMqttMessage)
        {
            if (incomingMqttMessage == null)
            {
                throw new ArgumentNullException(nameof(incomingMqttMessage));
            }

            _callback(incomingMqttMessage);
        }
    }
}