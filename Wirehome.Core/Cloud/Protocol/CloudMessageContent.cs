using System;
using MsgPack.Serialization;

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
