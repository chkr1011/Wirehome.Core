using System;
using Wirehome.Core.MessageBus;
using Wirehome.Core.Model;

namespace Wirehome.Core.Components
{
    public class ComponentRegistryMessageBusProxy
    {
        private readonly MessageBusService _messageBusService;

        public ComponentRegistryMessageBusProxy(MessageBusService messageBusService)
        {
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
        }

        public void PublishStatusChangedBusMessage(string componentUid, string statusUid, object oldValue, object newValue)
        {
            var message = new WirehomeDictionary
            {
                ["type"] = "component_registry.event.status_changed",
                ["component_uid"] = componentUid,
                ["status_uid"] = statusUid,
                ["old_value"] = oldValue,
                ["new_value"] = newValue,
                ["timestamp"] = DateTime.Now.ToString("O")
            };

            _messageBusService.Publish(message);
        }

        public void PublishSettingChangedBusMessage(string componentUid, string settingUid, object oldValue, object newValue)
        {
            var message = new WirehomeDictionary
            {
                ["type"] = "component_registry.event.setting_changed",
                ["component_uid"] = componentUid,
                ["setting_uid"] = settingUid,
                ["old_value"] = oldValue,
                ["new_value"] = newValue,
                ["timestamp"] = DateTime.Now.ToString("O")
            };

            _messageBusService.Publish(message);
        }

        public void PublishSettingRemovedBusMessage(string componentUid, string settingUid, object value)
        {
            var message = new WirehomeDictionary
            {
                ["type"] = "component_registry.event.setting_removed",
                ["component_uid"] = componentUid,
                ["setting_uid"] = settingUid,
                ["value"] = value,
                ["timestamp"] = DateTime.Now.ToString("O")
            };

            _messageBusService.Publish(message);
        }
    }
}
