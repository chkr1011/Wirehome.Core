using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Wirehome.Core.Notifications;

namespace Wirehome.Core.HTTP.Controllers
{
    public class NotificationsController : Controller
    {
        private readonly NotificationsService _notificationsService;

        public NotificationsController(NotificationsService notificationsService)
        {
            _notificationsService = notificationsService ?? throw new ArgumentNullException(nameof(notificationsService));
        }

        [HttpGet]
        [Route("api/v1/notifications")]
        [ApiExplorerSettings(GroupName = "v1")]
        public List<Notification> GetNotifications()
        {
            return _notificationsService.GetNotifications();
        }

        [HttpPost]
        [Route("api/v1/notification")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void PostNotification(NotificationType type, string message, TimeSpan? timetoLive)
        {
            _notificationsService.Publish(type, message, timetoLive);
        }

        [HttpDelete]
        [Route("api/v1/notifications/{uid}")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void DeleteNotification(Guid uid)
        {
            _notificationsService.DeleteNotification(uid);
        }

        [HttpDelete]
        [Route("api/v1/notifications")]
        [ApiExplorerSettings(GroupName = "v1")]
        public void DeleteNotifications()
        {
            _notificationsService.Clear();
        }
    }
}
