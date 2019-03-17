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

namespace Wirehome.Core.HTTP.Controllers.Hardware
{
    [ApiController]
    public class MqttController : Controller
    {
        private readonly MqttService _mqttService;

        public MqttController(MqttService mqttService)
        {
            _mqttService = mqttService ?? throw new ArgumentNullException(nameof(mqttService));
        }

        [HttpPost]
        [Route("/api/v1/mqtt/publish")]
        [ApiExplorerSettings(GroupName = "v1")]
        public async Task PostPublish(string topic, int qos = 0, bool retain = false)
        {
            if (topic == null) throw new ArgumentNullException(nameof(topic));

            var buffer = new byte[Request.ContentLength ?? 0];
            if (buffer.Length > 0)
            {
                await Request.Body.ReadAsync(buffer, 0, buffer.Length);
            }

            _mqttService.Publish(new MqttPublishParameters
            {
                Topic = topic,
                Payload = buffer,
                QualityOfServiceLevel = (MqttQualityOfServiceLevel)qos,
                Retain = retain
            });
        }

        [HttpPost]
        [Route("/api/v1/mqtt/import/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostImport(string uid, [FromBody] MqttImportTopicParameters parameters)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));
            if (parameters == null) throw new ArgumentNullException(nameof(parameters));

            _mqttService.StartTopicImport(uid, parameters);
        }

        [HttpDelete]
        [Route("/api/v1/mqtt/import/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void DeleteImport(string uid)
        {
            if (uid == null) throw new ArgumentNullException(nameof(uid));

            _mqttService.StopTopicImport(uid);
        }

        [HttpGet]
        [Route("api/v1/mqtt/clients")]
        [ApiExplorerSettings(GroupName = "v1")]
        public IList<IMqttClientSessionStatus> GetClients()
        {
            return _mqttService.GetClients();
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

        [HttpDelete]
        [Route("api/v1/mqtt/subscribers/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void DeleteSubscriber(string uid)
        {
            _mqttService.Unsubscribe(uid);
        }

        [HttpGet]
        [Route("api/v1/mqtt/retained_messages")]
        [ApiExplorerSettings(GroupName = "v1")]
        public IList<MqttApplicationMessage> GetRetainedMessages()
        {
            return _mqttService.GetRetainedMessages();
        }

        [HttpDelete]
        [Route("api/v1/mqtt/retained_messages/{topic}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void DeleteRetainedMessage(string topic)
        {
            _mqttService.Publish(new MqttPublishParameters
            {
                Topic = topic,
                Payload = new byte[0],
                QualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce,
                Retain = true
            });
        }

        [HttpDelete]
        [Route("api/v1/mqtt/retained_messages")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void DeleteRetainedMessages()
        {
            _mqttService.DeleteRetainedMessages();
        }
    }
}
