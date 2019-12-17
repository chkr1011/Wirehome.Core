using MsgPack.Serialization;
using System;
using System.Collections.Generic;

namespace Wirehome.Core.Cloud.Protocol
{
    public class CloudMessage
    {
        [MessagePackMember(0)]
        public string Type { get; set; }

        [MessagePackMember(1)]
        public string CorrelationId { get; set; }

        [MessagePackMember(2)]
        public ArraySegment<byte> Payload { get; set; }

        [MessagePackMember(3)]
        public Dictionary<string, string> Properties { get; set; }
    }
}
