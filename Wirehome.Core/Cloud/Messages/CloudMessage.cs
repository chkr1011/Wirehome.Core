using System;
using Newtonsoft.Json.Linq;

namespace Wirehome.Core.Cloud.Messages
{
    public class CloudMessage
    {
        public int ProtocolVersion { get; set; } = 1;

        public string Type { get; set; }

        public Guid? CorrelationUid { get; set; }

        public string Timestamp { get; set; } = DateTime.UtcNow.ToString("O");

        public JToken Content { get; set; }
    }
}
