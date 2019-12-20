using MessagePack;
using System;
using System.Collections.Generic;

namespace Wirehome.Core.Cloud.Protocol
{
    [MessagePackObject]
    public class TransportCloudMessage
    {
        [Key(0)]
        public string Type { get; set; }

        [Key(1)]
        public string CorrelationId { get; set; }

        [Key(2)]
        public ArraySegment<byte> Payload { get; set; }

        [Key(3)]
        public bool PayloadIsCompressed { get; set; }

        [Key(4)]
        public Dictionary<string, string> Properties { get; set; }
    }
}
