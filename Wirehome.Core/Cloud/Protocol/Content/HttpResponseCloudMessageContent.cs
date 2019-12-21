using MessagePack;
using System.Collections.Generic;

namespace Wirehome.Core.Cloud.Protocol.Content
{
    [MessagePackObject]
    public class HttpResponseCloudMessageContent
    {
        [Key(0)]
        public int StatusCode { get; set; }
              
        [Key(1)]
        public Dictionary<string, string> Headers { get; set; }

        [Key(2)]
        public byte[] Content { get; set; }
    }
}
