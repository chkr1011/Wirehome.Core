using MessagePack;
using System.Collections.Generic;

namespace Wirehome.Core.Cloud.Protocol.Content
{
    [MessagePackObject]
    public class HttpRequestCloudMessageContent
    {
        [Key(0)]
        public string Method { get; set; }

        [Key(1)]
        public string Uri { get; set; }

        [Key(2)]
        public Dictionary<string, string> Headers { get; set; }

        [Key(3)]
        public byte[] Content { get; set; }
    }
}
