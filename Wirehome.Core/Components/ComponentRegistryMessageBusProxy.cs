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

        public void PublishStatusChangedBusMessage(string componentUid, string propertyUid, object oldValue, object newValue)
        {
            var properties = new WirehomeDictionary
            {
                ["type"] = "component_registry.event.status_changed",
                ["component_uid"] = componentUid,
                ["status_uid"] = propertyUid,
                ["old_value"] = oldValue,
                ["new_value"] = newValue,
                ["timestamp"] = DateTime.Now
            };

            _messageBusService.Publish(properties);
        }

        public void PublishStatusReportedBusMessage(string componentUid, string propertyUid, object oldValue, object newValue, bool valueHasChanged)
        {
            var properties = new WirehomeDictionary
            {
                ["type"] = "component_registry.event.status_reported",
                ["component_uid"] = componentUid,
                ["status_uid"] = propertyUid,
                ["old_value"] = oldValue,
                ["new_value"] = newValue,
                ["value_has_changed"] = valueHasChanged,
                ["timestamp"] = DateTime.Now
            };

            _messageBusService.Publish(properties);
        }

        public void PublishSettingChangedBusMessage(string componentUid, string settingUid, object oldValue, object newValue)
        {
            var properties = new WirehomeDictionary
            {
                ["type"] = "component_registry.event.setting_changed",
                ["component_uid"] = componentUid,
                ["setting_uid"] = settingUid,
                ["old_value"] = oldValue,
                ["new_value"] = newValue,
                ["timestamp"] = DateTime.Now
            };

            _messageBusService.Publish(properties);
        }
    }
}
