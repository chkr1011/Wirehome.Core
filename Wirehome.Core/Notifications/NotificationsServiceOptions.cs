using System;

namespace Wirehome.Core.Notifications
{
    public class NotificationsServiceOptions
    {
        public const string Filename = "NotificationServiceConfiguration.json";

        public TimeSpan DefaultTimeToLiveForInformation { get; set; } = TimeSpan.FromDays(1);

        public TimeSpan DefaultTimeToLiveForWarning { get; set; } = TimeSpan.FromDays(2);

        public TimeSpan DefaultTimeToLiveForError { get; set; } = TimeSpan.FromDays(7);
    }
}
