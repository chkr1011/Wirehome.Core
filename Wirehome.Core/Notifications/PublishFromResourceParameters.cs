using System;
using System.Collections;

namespace Wirehome.Core.Notifications;

public sealed class PublishFromResourceParameters
{
    public IDictionary Parameters { get; set; }

    public string ResourceUid { get; set; }

    public TimeSpan? TimeToLive { get; set; }
    
    public NotificationType Type { get; set; } = NotificationType.Information;
}