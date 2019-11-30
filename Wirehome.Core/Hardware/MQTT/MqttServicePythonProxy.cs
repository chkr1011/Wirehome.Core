#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

using IronPython.Runtime;
using MQTTnet;
using MQTTnet.Adapter;
using MQTTnet.Client;
using MQTTnet.Client.Options;
using MQTTnet.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Wirehome.Core.Foundation.Model;
using Wirehome.Core.Python;
using Wirehome.Core.Python.Models;

namespace Wirehome.Core.Hardware.MQTT
{
    public class MqttServicePythonProxy : IInjectedPythonProxy
    {
        private readonly MqttService _mqttService;

        public MqttServicePythonProxy(MqttService mqttService)
        {
            _mqttService = mqttService ?? throw new ArgumentNullException(nameof(mqttService));
        }

        public string ModuleName { get; } = "mqtt";

        public void publish(PythonDictionary parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            var topic = Convert.ToString(parameters.get("topic"));
            var payload = parameters.get("payload", new byte[0]);
            var qos = Convert.ToInt32(parameters.get("qos", 0));
            var retain = Convert.ToBoolean(parameters.get("retain", false));

            _mqttService.Publish(new MqttPublishParameters
            {
                Topic = topic,
                Payload = ConvertPayload(payload),
                QualityOfServiceLevel = (MqttQualityOfServiceLevel)qos,
                Retain = retain
            });
        }

        public WirehomeDictionary publish_external(PythonDictionary parameters)
        {
            var server = Convert.ToString(parameters.get("server"));
            var port = Convert.ToInt32(parameters.get("port", 1883));
            var username = Convert.ToString(parameters.get("username"));
            var password = Convert.ToString(parameters.get("password"));
            var clientId = Convert.ToString(parameters.get("client_id", Guid.NewGuid().ToString("N")));
            var topic = Convert.ToString(parameters.get("topic"));
            var qos = Convert.ToInt32(parameters.get("qos", 0));
            var retain = Convert.ToBoolean(parameters.get("retain", false));
            var tls = Convert.ToBoolean(parameters.get("tls", false));
            var timeout = Convert.ToInt32(parameters.get("timeout", 5000));
            var payload = parameters.get("payload", null);

            var mqttClient = new MqttFactory().CreateMqttClient();
            try
            {
                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer(server, port)
                    .WithCredentials(username, password)
                    .WithClientId(clientId)
                    .WithCommunicationTimeout(TimeSpan.FromMilliseconds(timeout))
                    .WithTls(new MqttClientOptionsBuilderTlsParameters
                    {
                        UseTls = tls
                    })
                    .Build();

                mqttClient.ConnectAsync(options).GetAwaiter().GetResult();

                var message = new MqttApplicationMessageBuilder()
                    .WithTopic(topic)
                    .WithPayload(ConvertPayload(payload))
                    .WithQualityOfServiceLevel((MqttQualityOfServiceLevel)qos)
                    .WithRetainFlag(retain)
                    .Build();

                mqttClient.PublishAsync(message).GetAwaiter().GetResult();

                return new WirehomeDictionary().WithType("success");
            }
            catch (MqttConnectingFailedException)
            {
                return new WirehomeDictionary().WithType("exception.connecting_failed");
            }
            catch (Exception exception)
            {
                return new ExceptionPythonModel(exception).ConvertToPythonDictionary();
            }
            finally
            {
                mqttClient?.DisconnectAsync().GetAwaiter().GetResult();
                mqttClient?.Dispose();
            }
        }

        public string subscribe(string uid, string topic_filter, Action<PythonDictionary> callback)
        {
            if (topic_filter == null) throw new ArgumentNullException(nameof(topic_filter));
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            
            return _mqttService.Subscribe(uid, topic_filter, message =>
            {
                var pythonMessage = new PythonDictionary
                {
                    ["subscription_uid"] = uid,
                    ["client_id"] = message.ClientId,
                    ["topic"] = message.ApplicationMessage.Topic,
                    ["payload"] = message.ApplicationMessage.Payload,
                    ["qos"] = (int)message.ApplicationMessage.QualityOfServiceLevel,
                    ["retain"] = message.ApplicationMessage.Retain
                };

                callback(pythonMessage);
            });
        }

        public void unsubscribe(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            _mqttService.Unsubscribe(uid);
        }

        public string start_topic_import(string uid, PythonDictionary parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            var topicImportParameters = new MqttImportTopicParameters
            {
                Server = Convert.ToString(parameters.get("server")),
                Port = Convert.ToInt32(parameters.get("port", 1883)),
                UseTls = Convert.ToBoolean(parameters.get("tls", false)),
                Username = Convert.ToString(parameters.get("username")),
                Password = Convert.ToString(parameters.get("password")),
                ClientId = Convert.ToString(parameters.get("client_id", Guid.NewGuid().ToString("N"))),
                Topic = Convert.ToString(parameters.get("topic")),
                QualityOfServiceLevel = (MqttQualityOfServiceLevel)Convert.ToInt32(parameters.get("qos"))
            };

            return _mqttService.StartTopicImport(uid, topicImportParameters).GetAwaiter().GetResult();
        }

        public void stop_topic_import(string uid)
        {
            _mqttService.StopTopicImport(uid).GetAwaiter().GetResult();
        }

        private static byte[] ConvertPayload(object payload)
        {
            if (payload == null)
            {
                return new byte[0];
            }

            if (payload is ByteArray byteArray)
            {
                return byteArray.ToArray();
            }

            if (payload is byte[] bytes)
            {
                return bytes;
            }

            if (payload is string s)
            {
                return Encoding.UTF8.GetBytes(s);
            }

            if (payload is List<byte> b)
            {
                return b.ToArray();
            }
            
            if (payload is IEnumerable<int> i)
            {
                return i.Select(Convert.ToByte).ToArray();
            }

            throw new NotSupportedException($"MQTT Payload format '{payload.GetType().Name}' is not supported.");
        }
    }
}