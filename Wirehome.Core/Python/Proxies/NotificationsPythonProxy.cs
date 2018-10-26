using System;
using Wirehome.Core.Notifications;

#pragma warning disable IDE1006 // Naming Styles
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Global

namespace Wirehome.Core.Python.Proxies
{
    public class NotificationsPythonProxy : IPythonProxy
    {
        private readonly NotificationsService _notificationsService;

        public NotificationsPythonProxy(NotificationsService notificationsService)
        {
            _notificationsService = notificationsService ?? throw new ArgumentNullException(nameof(notificationsService));
        }

        public string ModuleName { get; } = "notifications";

        public void publish(string type, string message, string ttl)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (message == null) throw new ArgumentNullException(nameof(message));

            var typeBuffer = (NotificationType)Enum.Parse(typeof(NotificationType), type, true);
            TimeSpan? ttlBuffer = null;

            if (!string.IsNullOrEmpty(ttl))
            {
                ttlBuffer = TimeSpan.Parse(ttl);
            }

            _notificationsService.Publish(typeBuffer, message, ttlBuffer);
        }

        public void publish_from_resource(string type, string resourceUid, string ttl)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            if (resourceUid == null) throw new ArgumentNullException(nameof(resourceUid));

            var parameters = new PublishFromResourceParameters
            {
                Type = (NotificationType) Enum.Parse(typeof(NotificationType), type, true),
                ResourceUid = resourceUid
            };

            if (!string.IsNullOrEmpty(ttl))
            {
                parameters.TimeToLive = TimeSpan.Parse(ttl);
            }

            _notificationsService.PublishFromResource(parameters);
        }
    }
}