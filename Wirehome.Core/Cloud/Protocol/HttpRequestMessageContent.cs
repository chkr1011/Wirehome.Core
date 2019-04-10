using System.Collections.Generic;
using MsgPack.Serialization;

namespace Wirehome.Core.Cloud.Protocol
{
    public class HttpRequestMessageContent
    {
        [MessagePackMember(0)]
        public string Method { get; set; }

        [MessagePackMember(1)]
        public string Uri { get; set; }

        [MessagePackMember(2)]
        public Dictionary<string, string> Headers { get; set; }

        [MessagePackMember(3)]
        public byte[] Content { get; set; }
    }
}
