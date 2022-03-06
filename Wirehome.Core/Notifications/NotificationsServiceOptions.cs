using System;

namespace Wirehome.Core.Notifications;

public sealed class NotificationsServiceOptions
{
    public const string Filename = "NotificationServiceConfiguration.json";

    public TimeSpan DefaultTimeToLiveForError { get; set; } = TimeSpan.FromDays(7);

    public TimeSpan DefaultTimeToLiveForInformation { get; set; } = TimeSpan.FromDays(1);

    public TimeSpan DefaultTimeToLiveForWarning { get; set; } = TimeSpan.FromDays(2);
}