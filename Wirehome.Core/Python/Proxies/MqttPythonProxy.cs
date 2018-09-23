#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTTnet.Protocol;
using Wirehome.Core.Hardware.MQTT;
using Wirehome.Core.Model;

namespace Wirehome.Core.Python.Proxies
{
    public class MqttPythonProxy : IPythonProxy
    {
        private readonly MqttService _mqttService;

        public MqttPythonProxy(MqttService mqttService)
        {
            _mqttService = mqttService ?? throw new ArgumentNullException(nameof(mqttService));
        }

        public string ModuleName { get; } = "mqtt";

        public string enable_topic_import(WirehomeDictionary parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            var uid = parameters.GetValueOrDefault("uid", Guid.NewGuid().ToString("D")) as string;
            var topicImportParameters = new MqttImportTopicParameters
            {
                Server = parameters.GetValueOrDefault("server", null) as string,
                Port = parameters.GetValueOrDefault("port", null) as int?,
                Topic = parameters.GetValueOrDefault("topic", null) as string
            };

            _mqttService.EnableTopicImport(uid, topicImportParameters);

            return uid;
        }

        public void publish(WirehomeDictionary parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            var topic = parameters.GetValueOrDefault("topic") as string;
            var payload = parameters.GetValueOrDefault("payload");
            var qos = parameters.GetValueOrDefault("qos", 0);
            var retain = parameters.GetValueOrDefault("retain", false) as bool? ?? false;

            publish(topic, payload, qos, retain);
        }

        public void publish(string topic, object payload, object qos = null, bool retain = false)
        {
            if (payload is string s)
            {
                payload = Encoding.UTF8.GetBytes(s);
            }
            else if (payload is List<byte> b)
            {
                payload = b.ToArray();
            }
            else if (payload is List<int> i)
            {
                payload = i.Cast<byte>().ToArray();
            }
            else
            {
                payload = new byte[0];
            }

            var qosValue = MqttQualityOfServiceLevel.AtMostOnce;
            if (qos != null)
            {
                if (Enum.TryParse(typeof(MqttQualityOfServiceLevel), Convert.ToString(qos), true, out var tmp))
                {
                    qosValue = (MqttQualityOfServiceLevel)tmp;
                }
            }

            _mqttService.Publish(new MqttPublishParameters
            {
                Topic = topic,
                Payload = (byte[])payload,
                QualityOfServiceLevel = qosValue,
                Retain = retain
            });
        }

        public string subscribe(string topicFilter, Action<object> callback)
        {
            if (topicFilter == null) throw new ArgumentNullException(nameof(topicFilter));
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            var uid = Guid.NewGuid().ToString("D");

            _mqttService.Subscribe(uid, topicFilter, message =>
            {
                var properties = new WirehomeDictionary
                {
                    ["client_id"] = message.ClientId,
                    ["topic"] = message.ApplicationMessage.Topic,
                    ["payload"] = message.ApplicationMessage.Payload,
                    ["qos"] = (int)message.ApplicationMessage.QualityOfServiceLevel,
                    ["retain"] = message.ApplicationMessage.Retain
                };

                callback(PythonConvert.ForPython(properties));
            });

            return uid;
        }

        public void unsubscribe(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            _mqttService.Unsubscribe(uid);
        }
    }
}