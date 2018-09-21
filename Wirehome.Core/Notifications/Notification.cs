using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Wirehome.Core.Notifications
{
    public class Notification
    {
        public Guid Uid { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public NotificationType Type { get; set; }

        public DateTime Timestamp { get; set; }

        public string Message { get; set; }

        public TimeSpan TimeToLive { get; set; }
    }
}
