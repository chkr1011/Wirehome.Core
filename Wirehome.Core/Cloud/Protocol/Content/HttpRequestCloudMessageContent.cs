using MsgPack.Serialization;
using System.Collections.Generic;

namespace Wirehome.Core.Cloud.Protocol.Content
{
    public class HttpRequestCloudMessageContent
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
