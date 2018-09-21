using System;
using System.Collections;

namespace Wirehome.Core.Notifications
{
    public class PublishFromResourceParameters
    {
        public NotificationType Type { get; set; } = NotificationType.Information;

        public string ResourceUid { get; set; }

        public IDictionary Parameters { get; set; }

        public TimeSpan? TimeToLive { get; set; }
    }
}
