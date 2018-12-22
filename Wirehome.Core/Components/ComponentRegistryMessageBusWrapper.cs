using System;
using Wirehome.Core.MessageBus;
using Wirehome.Core.Model;

namespace Wirehome.Core.Components
{
    public class ComponentRegistryMessageBusWrapper
    {
        private readonly MessageBusService _messageBusService;

        public ComponentRegistryMessageBusWrapper(MessageBusService messageBusService)
        {
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
        }

        public void PublishTagAddedEvent(string componentUid, string tag)
        {
            var message = new WirehomeDictionary
            {
                ["type"] = "component_registry.event.tag_added",
                ["component_uid"] = componentUid,
                ["tag"] = tag,
                ["timestamp"] = DateTime.Now.ToString("O")
            };

            _messageBusService.Publish(message);
        }

        public void PublishTagRemovedEvent(string componentUid, string tag)
        {
            var message = new WirehomeDictionary
            {
                ["type"] = "component_registry.event.tag_removed",
                ["component_uid"] = componentUid,
                ["tag"] = tag,
                ["timestamp"] = DateTime.Now.ToString("O")
            };

            _messageBusService.Publish(message);
        }

        public void PublishStatusChangedEvent(string componentUid, string statusUid, object oldValue, object newValue)
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

        public void PublishSettingChangedEvent(string componentUid, string settingUid, object oldValue, object newValue)
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

        public void PublishSettingRemovedEvent(string componentUid, string settingUid, object value)
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
