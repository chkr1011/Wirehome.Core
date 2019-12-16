using MsgPack.Serialization;
using System.Collections.Generic;

namespace Wirehome.Core.Cloud.Protocol.Content
{
    public class HttpResponseCloudMessageContent
    {
        [MessagePackMember(0)]
        public Dictionary<string, string> Headers { get; set; }

        [MessagePackMember(1)]
        public byte[] Content { get; set; }

        [MessagePackMember(2)]
        public int? StatusCode { get; set; }
    }
}
