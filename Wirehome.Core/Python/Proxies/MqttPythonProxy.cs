#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTTnet;
using MQTTnet.Adapter;
using MQTTnet.Client;
using MQTTnet.Protocol;
using Wirehome.Core.Hardware.MQTT;
using Wirehome.Core.Model;
using Wirehome.Core.Python.Models;

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

        public void publish(WirehomeDictionary parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            var topic = Convert.ToString(parameters.GetValueOrDefault("topic"));
            var payload = parameters.GetValueOrDefault("payload", new byte[0]);
            var qos = Convert.ToInt32(parameters.GetValueOrDefault("qos", 0));
            var retain = Convert.ToBoolean(parameters.GetValueOrDefault("retain", false));

            _mqttService.Publish(new MqttPublishParameters
            {
                Topic = topic,
                Payload = ConvertPayload(payload),
                QualityOfServiceLevel = (MqttQualityOfServiceLevel)qos,
                Retain = retain
            });
        }

        public WirehomeDictionary publish_external(WirehomeDictionary parameters)
        {
            var server = Convert.ToString(parameters.GetValueOrDefault("server"));
            var port = Convert.ToInt32(parameters.GetValueOrDefault("port", 1883));
            var username = Convert.ToString(parameters.GetValueOrDefault("username"));
            var password = Convert.ToString(parameters.GetValueOrDefault("password"));
            var clientId = Convert.ToString(parameters.GetValueOrDefault("client_id", Guid.NewGuid().ToString("N")));
            var topic = Convert.ToString(parameters.GetValueOrDefault("topic"));
            var qos = Convert.ToInt32(parameters.GetValueOrDefault("qos", 0));
            var retain = Convert.ToBoolean(parameters.GetValueOrDefault("retain", false));
            var tls = Convert.ToBoolean(parameters.GetValueOrDefault("tls", false));
            var timeout = Convert.ToInt32(parameters.GetValueOrDefault("timeout", 5000));
            var payload = parameters.GetValueOrDefault("payload", null);

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

        public string subscribe(string subscription_uid, string topic_filter, Action<object> callback)
        {
            if (topic_filter == null) throw new ArgumentNullException(nameof(topic_filter));
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            if (string.IsNullOrEmpty(subscription_uid))
            {
                subscription_uid = Guid.NewGuid().ToString("D");
            }

            _mqttService.Subscribe(subscription_uid, topic_filter, message =>
            {
                var properties = new WirehomeDictionary()
                    .WithValue("client_id", message.ClientId)
                    .WithValue("topic", message.ApplicationMessage.Topic)
                    .WithValue("payload", message.ApplicationMessage.Payload)
                    .WithValue("qos", message.ApplicationMessage.QualityOfServiceLevel)
                    .WithValue("retain", message.ApplicationMessage.Retain);

                callback(PythonConvert.ToPython(properties));
            });

            return subscription_uid;
        }

        public void unsubscribe(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            _mqttService.Unsubscribe(uid);
        }

        public string start_topic_import(string uid, WirehomeDictionary parameters)
        {
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            var topicImportParameters = new MqttImportTopicParameters
            {
                Server = Convert.ToString(parameters.GetValueOrDefault("server")),
                Port = Convert.ToInt32(parameters.GetValueOrDefault("port")),
                UseTls = Convert.ToBoolean(parameters.GetValueOrDefault("tls")),
                Username = Convert.ToString(parameters.GetValueOrDefault("username")),
                Password = Convert.ToString(parameters.GetValueOrDefault("password")),
                ClientId = Convert.ToString(parameters.GetValueOrDefault("client_id", Guid.NewGuid().ToString("N"))),
                Topic = Convert.ToString(parameters.GetValueOrDefault("topic")),
                QualityOfServiceLevel = (MqttQualityOfServiceLevel)Convert.ToInt32(parameters.GetValueOrDefault("qos"))
            };

            return _mqttService.StartTopicImport(uid, topicImportParameters);
        }

        public void stop_topic_import(string uid)
        {
            _mqttService.StopTopicImport(uid);
        }

        private static byte[] ConvertPayload(object payload)
        {
            if (payload == null)
            {
                return new byte[0];
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

            throw new NotSupportedException();
        }
    }
}