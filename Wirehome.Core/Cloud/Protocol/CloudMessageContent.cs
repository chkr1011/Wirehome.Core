using MsgPack.Serialization;
using System;

namespace Wirehome.Core.Cloud.Protocol
{
    public class CloudMessageContent
    {
        [MessagePackMember(0)]
        public ArraySegment<byte> Data { get; set; }

        [MessagePackMember(1)]
        public bool IsCompressed { get; set; }
    }
}
