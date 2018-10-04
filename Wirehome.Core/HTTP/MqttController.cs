using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.Hardware.MQTT;

namespace Wirehome.Core.HTTP
{
    public class MqttController : Controller
    {
        private readonly MqttService _mqttService;

        public MqttController(MqttService mqttService)
        {
            _mqttService = mqttService ?? throw new ArgumentNullException(nameof(mqttService));
        }

        [HttpGet]
        [Route("api/v1/mqtt/subscriptions")]
        [ApiExplorerSettings(GroupName = "v1")]
        public List<string> GetSubscriptions()
        {
            return _mqttService.GetSubscriptions();
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
