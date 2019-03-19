using System.Collections.Generic;

namespace Wirehome.Core.Cloud.Protocol
{
    public class HttpResponseMessageContent
    {
        public int StatusCode { get; set; } = 200;

        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        public byte[] Content { get; set; }
    }
}
