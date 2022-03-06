using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MQTTnet;
using MQTTnet.Protocol;
using MQTTnet.Server;
using Wirehome.Core.Hardware.MQTT;
using Wirehome.Core.HTTP.Controllers.Models;
using Wirehome.Core.HTTP.Filters;

namespace Wirehome.Core.HTTP.Controllers;

[ApiController]
public sealed class MqttController : Controller
{
    readonly MqttService _mqttService;

    public MqttController(MqttService mqttService)
    {
        _mqttService = mqttService ?? throw new ArgumentNullException(nameof(mqttService));
    }

    [HttpDelete]
    [Route("/api/v1/mqtt/imports/{uid}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public Task DeleteImport(string uid)
    {
        if (uid == null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        return _mqttService.StopTopicImport(uid);
    }

    [HttpDelete]
    [Route("api/v1/mqtt/retained_messages/{topic}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public void DeleteRetainedMessage(string topic)
    {
        _mqttService.Publish(new MqttPublishParameters
        {
            Topic = topic,
            Payload = Array.Empty<byte>(),
            QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
            Retain = true
        });
    }

    [HttpDelete]
    [Route("api/v1/mqtt/retained_messages")]
    [ApiExplorerSettings(GroupName = "v1")]
    public Task DeleteRetainedMessages()
    {
        return _mqttService.DeleteRetainedMessagesAsync();
    }

    [HttpDelete]
    [Route("api/v1/mqtt/subscribers/{uid}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public void DeleteSubscriber(string uid)
    {
        _mqttService.Unsubscribe(uid);
    }

    [HttpGet]
    [Route("api/v1/mqtt/clients")]
    [ApiExplorerSettings(GroupName = "v1")]
    public Task<IList<MqttClientStatus>> GetClients()
    {
        return _mqttService.GetClientsAsync();
    }

    [HttpGet]
    [Route("/api/v1/mqtt/imports/uids")]
    [ApiExplorerSettings(GroupName = "v1")]
    public List<string> GetImports()
    {
        return _mqttService.GetTopicImportUids();
    }

    [HttpGet]
    [Route("api/v1/mqtt/retained_messages")]
    [ApiExplorerSettings(GroupName = "v1")]
    public Task<IList<MqttApplicationMessage>> GetRetainedMessages()
    {
        return _mqttService.GetRetainedMessagesAsync();
    }

    [HttpGet]
    [Route("api/v1/mqtt/sessions")]
    [ApiExplorerSettings(GroupName = "v1")]
    public Task<IList<MqttSessionStatus>> GetSessions()
    {
        return _mqttService.GetSessionsAsync();
    }

    [HttpGet]
    [Route("api/v1/mqtt/subscribers")]
    [ApiExplorerSettings(GroupName = "v1")]
    public Dictionary<string, MqttSubscriberModel> GetSubscriptions()
    {
        return _mqttService.GetSubscribers().ToDictionary(s => s.Uid, s => new MqttSubscriberModel
        {
            TopicFilter = s.TopicFilter
        });
    }

    [HttpPost]
    [Route("/api/v1/mqtt/imports/{uid}")]
    [ApiExplorerSettings(GroupName = "v1")]
    public Task PostImport(string uid, [FromBody] MqttImportTopicParameters parameters)
    {
        if (uid == null)
        {
            throw new ArgumentNullException(nameof(uid));
        }

        if (parameters == null)
        {
            throw new ArgumentNullException(nameof(parameters));
        }

        return _mqttService.StartTopicImport(uid, parameters);
    }

    [HttpPost]
    [Route("/api/v1/mqtt/publish")]
    [ApiExplorerSettings(GroupName = "v1")]
    [BinaryContent]
    public async Task PostPublish(string topic, int qos = 0, bool retain = false)
    {
        if (topic == null)
        {
            throw new ArgumentNullException(nameof(topic));
        }

        var buffer = new byte[Request.ContentLength ?? 0];
        if (buffer.Length > 0)
        {
            await Request.Body.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
        }

        _mqttService.Publish(new MqttPublishParameters
        {
            Topic = topic,
            Payload = buffer,
            QualityOfServiceLevel = (MqttQualityOfServiceLevel)qos,
            Retain = retain
        });
    }
}