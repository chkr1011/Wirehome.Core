#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedType.Global

using System;
using System.Buffers;
using IronPython.Runtime;
using MQTTnet;
using MQTTnet.Adapter;
using MQTTnet.Protocol;
using Wirehome.Core.Hardware.MQTT.TopicImport;
using Wirehome.Core.Python;
using Wirehome.Core.Python.Models;

namespace Wirehome.Core.Hardware.MQTT;

public sealed class MqttServicePythonProxy : IInjectedPythonProxy
{
    static readonly object TrueBox = true;
    static readonly object FalseBox = false;
    static readonly object QoS0Box = 0;
    static readonly object QoS1Box = 1;
    static readonly object QoS2Box = 2;

    readonly MqttService _mqttService;

    public MqttServicePythonProxy(MqttService mqttService)
    {
        _mqttService = mqttService ?? throw new ArgumentNullException(nameof(mqttService));
    }

    public string ModuleName { get; } = "mqtt";

    public void publish(PythonDictionary parameters)
    {
        if (parameters is null)
        {
            throw new ArgumentNullException(nameof(parameters));
        }

        var topic = Convert.ToString(parameters.get("topic"));
        var payload = parameters.get("payload", Array.Empty<byte>());
        var qos = Convert.ToInt32(parameters.get("qos", 0));
        var retain = Convert.ToBoolean(parameters.get("retain", FalseBox));

        _mqttService.Publish(new MqttPublishOptions
        {
            Topic = topic,
            Payload = PythonConvert.ToPayload(payload),
            QualityOfServiceLevel = (MqttQualityOfServiceLevel)qos,
            Retain = retain
        }).GetAwaiter().GetResult();
    }

    public static PythonDictionary publish_external(PythonDictionary parameters)
    {
        if (parameters is null)
        {
            throw new ArgumentNullException(nameof(parameters));
        }

        var server = Convert.ToString(parameters.get("server"));
        var port = Convert.ToInt32(parameters.get("port", 1883));
        var username = Convert.ToString(parameters.get("username"));
        var password = Convert.ToString(parameters.get("password"));
        var clientId = Convert.ToString(parameters.get("client_id", Guid.NewGuid().ToString("N")));
        var topic = Convert.ToString(parameters.get("topic"));
        var qos = Convert.ToInt32(parameters.get("qos", 0));
        var retain = Convert.ToBoolean(parameters.get("retain", FalseBox));
        var tls = Convert.ToBoolean(parameters.get("tls", FalseBox));
        var timeout = Convert.ToInt32(parameters.get("timeout", 5000));
        var payload = parameters.get("payload", null);

        var mqttClient = new MqttClientFactory().CreateMqttClient();
        try
        {
            var options = new MqttClientOptionsBuilder().WithTcpServer(server, port).WithCredentials(username, password).WithClientId(clientId)
                .WithTimeout(TimeSpan.FromMilliseconds(timeout)).WithTlsOptions(o => o.UseTls(tls)).Build();

            mqttClient.ConnectAsync(options).GetAwaiter().GetResult();

            var message = new MqttApplicationMessageBuilder().WithTopic(topic).WithPayload(PythonConvert.ToPayload(payload))
                .WithQualityOfServiceLevel((MqttQualityOfServiceLevel)qos).WithRetainFlag(retain).Build();

            mqttClient.PublishAsync(message).GetAwaiter().GetResult();

            return new PythonDictionary
            {
                ["type"] = "success"
            };
        }
        catch (MqttConnectingFailedException)
        {
            return new PythonDictionary
            {
                ["type"] = "exception.connecting_failed"
            };
        }
        catch (Exception exception)
        {
            return PythonConvert.ToPythonDictionary(new ExceptionPythonModel(exception).ToDictionary());
        }
        finally
        {
            mqttClient.DisconnectAsync().GetAwaiter().GetResult();
            mqttClient.Dispose();
        }
    }

    public string start_topic_import(string uid, PythonDictionary parameters)
    {
        if (parameters == null)
        {
            throw new ArgumentNullException(nameof(parameters));
        }

        var topicImportParameters = new MqttTopicImportOptions
        {
            Server = Convert.ToString(parameters.get("server")),
            Port = Convert.ToInt32(parameters.get("port", 1883)),
            UseTls = Convert.ToBoolean(parameters.get("tls", FalseBox)),
            Username = Convert.ToString(parameters.get("username")),
            Password = Convert.ToString(parameters.get("password")),
            ClientId = Convert.ToString(parameters.get("client_id", Guid.NewGuid().ToString("N"))),
            Topic = Convert.ToString(parameters.get("topic")),
            EnableLogging = Convert.ToBoolean(parameters.get("enable_logging", FalseBox)),
            QualityOfServiceLevel = (MqttQualityOfServiceLevel)Convert.ToInt32(parameters.get("qos"))
        };

        return _mqttService.StartTopicImport(uid, topicImportParameters).GetAwaiter().GetResult();
    }

    public void stop_topic_import(string uid)
    {
        _mqttService.StopTopicImport(uid).GetAwaiter().GetResult();
    }

    public string subscribe(string uid, string topic_filter, Action<PythonDictionary> callback)
    {
        if (topic_filter == null)
        {
            throw new ArgumentNullException(nameof(topic_filter));
        }

        if (callback == null)
        {
            throw new ArgumentNullException(nameof(callback));
        }

        return _mqttService.Subscribe(uid, topic_filter, eventArgs =>
        {
            var payload = eventArgs.ApplicationMessage.Payload.ToArray();

            var pythonMessage = new PythonDictionary
            {
                ["subscription_uid"] = uid,
                ["client_id"] = eventArgs.ClientId,
                ["topic"] = eventArgs.ApplicationMessage.Topic,
                ["payload"] = new Bytes(payload),
                ["qos"] = ConvertQoS(eventArgs.ApplicationMessage.QualityOfServiceLevel),
                ["retain"] = eventArgs.ApplicationMessage.Retain ? TrueBox : FalseBox
            };

            callback(pythonMessage);
        });
    }

    public void unsubscribe(string uid)
    {
        if (uid == null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        _mqttService.Unsubscribe(uid);
    }

    static object ConvertQoS(MqttQualityOfServiceLevel qos)
    {
        switch (qos)
        {
            case MqttQualityOfServiceLevel.ExactlyOnce:
                return QoS2Box;

            case MqttQualityOfServiceLevel.AtLeastOnce:
                return QoS1Box;

            case MqttQualityOfServiceLevel.AtMostOnce:
                return QoS0Box;
        }

        throw new NotSupportedException();
    }
}