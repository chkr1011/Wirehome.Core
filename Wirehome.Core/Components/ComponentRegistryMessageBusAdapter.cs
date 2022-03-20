using System;
using System.Collections.Generic;
using Wirehome.Core.MessageBus;

namespace Wirehome.Core.Components;

public sealed class ComponentRegistryMessageBusAdapter
{
    readonly MessageBusService _messageBusService;

    public ComponentRegistryMessageBusAdapter(MessageBusService messageBusService)
    {
        _messageBusService = messageBusService ?? throw new ArgumentNullException(nameof(messageBusService));
    }

    public void PublishDisabledEvent(string componentUid)
    {
        var message = new Dictionary<object, object>(3)
        {
            ["type"] = "component_registry.event.disabled",
            ["component_uid"] = componentUid,
            ["timestamp"] = DateTime.Now.ToString("O")
        };

        _messageBusService.Publish(message);
    }

    public void PublishEnabledEvent(string componentUid)
    {
        var message = new Dictionary<object, object>(3)
        {
            ["type"] = "component_registry.event.enabled",
            ["component_uid"] = componentUid,
            ["timestamp"] = DateTime.Now.ToString("O")
        };

        _messageBusService.Publish(message);
    }

    public void PublishSettingChangedEvent(string componentUid, string settingUid, object oldValue, object newValue)
    {
        var message = new Dictionary<object, object>(6)
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
        var message = new Dictionary<object, object>(5)
        {
            ["type"] = "component_registry.event.setting_removed",
            ["component_uid"] = componentUid,
            ["setting_uid"] = settingUid,
            ["value"] = value,
            ["timestamp"] = DateTime.Now.ToString("O")
        };

        _messageBusService.Publish(message);
    }

    public void PublishStatusChangedEvent(ComponentStatusChangedEventArgs eventArgs)
    {
        if (eventArgs is null)
        {
            throw new ArgumentNullException(nameof(eventArgs));
        }

        var message = new Dictionary<object, object>(6)
        {
            ["type"] = "component_registry.event.status_changed",
            ["timestamp"] = eventArgs.Timestamp.ToString("O"),
            ["component_uid"] = eventArgs.Component.Uid,
            ["status_uid"] = eventArgs.StatusUid,
            ["old_value"] = eventArgs.OldValue,
            ["new_value"] = eventArgs.NewValue
        };

        _messageBusService.Publish(message);
    }

    public void PublishStatusChangingEvent(ComponentStatusChangingEventArgs eventArgs)
    {
        if (eventArgs is null)
        {
            throw new ArgumentNullException(nameof(eventArgs));
        }

        var message = new Dictionary<object, object>(6)
        {
            ["type"] = "component_registry.event.status_changing",
            ["timestamp"] = eventArgs.Timestamp.ToString("O"),
            ["component_uid"] = eventArgs.Component.Uid,
            ["status_uid"] = eventArgs.StatusUid,
            ["old_value"] = eventArgs.OldValue,
            ["new_value"] = eventArgs.NewValue
        };

        _messageBusService.Publish(message);
    }

    public void PublishTagAddedEvent(string componentUid, string tag)
    {
        var message = new Dictionary<object, object>(4)
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
        var message = new Dictionary<object, object>(4)
        {
            ["type"] = "component_registry.event.tag_removed",
            ["component_uid"] = componentUid,
            ["tag"] = tag,
            ["timestamp"] = DateTime.Now.ToString("O")
        };

        _messageBusService.Publish(message);
    }
}