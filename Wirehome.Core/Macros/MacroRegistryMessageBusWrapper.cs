using System;
using System.Collections.Generic;
using Wirehome.Core.MessageBus;

namespace Wirehome.Core.Macros
{
    public class MacroRegistryMessageBusWrapper
    {
        readonly MessageBusService _messageBusService;

        public MacroRegistryMessageBusWrapper(MessageBusService messageBusService)
        {
            _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
        }

        public void PublishSettingChangedBusMessage(string macroUid, string settingUid, object oldValue, object newValue)
        {
            var message = new Dictionary<object, object>
            {
                ["type"] = "macro_registry.event.setting_changed",
                ["macro_uid"] = macroUid,
                ["setting_uid"] = settingUid,
                ["old_value"] = oldValue,
                ["new_value"] = newValue,
                ["timestamp"] = DateTime.Now.ToString("O")
            };

            _messageBusService.Publish(message);
        }

        public void PublishSettingRemovedBusMessage(string macroUid, string settingUid, object value)
        {
            var message = new Dictionary<object, object>
            {
                ["type"] = "macro_registry.event.setting_removed",
                ["macro_uid"] = macroUid,
                ["setting_uid"] = settingUid,
                ["value"] = value,
                ["timestamp"] = DateTime.Now.ToString("O")
            };

            _messageBusService.Publish(message);
        }

        public void PublishMacroExecutedBusMessage(string macroUid, IDictionary<object, object> result)
        {
            var message = new Dictionary<object, object>
            {
                ["type"] = "macro_registry.event.macro_executed",
                ["macro_uid"] = macroUid,
                ["result"] = result,
                ["timestamp"] = DateTime.Now.ToString("O")
            };

            _messageBusService.Publish(message);
        }
    }
}
