using System;

namespace Wirehome.Core.Cloud.Messages
{
    public class BaseCloudMessage
    {
        public string Type { get; set; }

        public string Timestamp { get; set; } = DateTime.UtcNow.ToString("O");
    }
}
