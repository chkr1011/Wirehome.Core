using System;

namespace Wirehome.Core.Notifications
{
    public class Notification
    {
        public Guid Uid { get; set; }

        public NotificationType Type { get; set; }

        public DateTime Timestamp { get; set; }

        public string Message { get; set; }

        public TimeSpan TimeToLive { get; set; }

        public string Tag { get; set; }
    }
}
