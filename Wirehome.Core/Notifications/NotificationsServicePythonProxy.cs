using System;
using System.Linq;
using IronPython.Runtime;
using Wirehome.Core.Python;

#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace Wirehome.Core.Notifications;

public sealed class NotificationsServicePythonProxy : IInjectedPythonProxy
{
    readonly NotificationsService _notificationsService;

    public NotificationsServicePythonProxy(NotificationsService notificationsService)
    {
        _notificationsService = notificationsService ?? throw new ArgumentNullException(nameof(notificationsService));
    }

    public string ModuleName { get; } = "notifications";

    public void delete(string uid)
    {
        _notificationsService.DeleteNotification(Guid.Parse(uid));
    }

    public List find_all_by_tag(string tag)
    {
        if (tag is null)
        {
            throw new ArgumentNullException(nameof(tag));
        }

        var matchingNotifications = _notificationsService.GetNotifications().Where(n => n.Tag == tag).Select(n => n.Uid);
        var result = new List();

        foreach (var notificationUid in matchingNotifications)
        {
            result.Add(notificationUid);
        }

        return result;
    }

    public string find_first_by_tag(string tag)
    {
        if (tag is null)
        {
            throw new ArgumentNullException(nameof(tag));
        }

        var matchingNotification = _notificationsService.GetNotifications().FirstOrDefault(n => n.Tag == tag);
        if (matchingNotification != null)
        {
            return matchingNotification.Uid.ToString();
        }

        return null;
    }

    public void publish(string type, string message, string ttl = null)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        var typeBuffer = (NotificationType)Enum.Parse(typeof(NotificationType), type, true);
        TimeSpan? ttlBuffer = null;

        if (!string.IsNullOrEmpty(ttl))
        {
            ttlBuffer = TimeSpan.Parse(ttl);
        }

        _notificationsService.Publish(typeBuffer, message, ttlBuffer);
    }

    public void publish_from_resource(string type, string resourceUid, string ttl = null)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }

        if (resourceUid == null)
        {
            throw new ArgumentNullException(nameof(resourceUid));
        }

        var parameters = new PublishFromResourceParameters
        {
            Type = (NotificationType)Enum.Parse(typeof(NotificationType), type, true),
            ResourceUid = resourceUid
        };

        if (!string.IsNullOrEmpty(ttl))
        {
            parameters.TimeToLive = TimeSpan.Parse(ttl);
        }

        _notificationsService.PublishFromResource(parameters);
    }
}