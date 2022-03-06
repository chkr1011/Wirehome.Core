using System;

namespace Wirehome.Core.Notifications;

public sealed class Notification
{
    public string Message { get; set; }

    public string Tag { get; set; }

    public DateTime Timestamp { get; set; }

    public TimeSpan TimeToLive { get; set; }

    public NotificationType Type { get; set; }
    
    public Guid Uid { get; set; }
}