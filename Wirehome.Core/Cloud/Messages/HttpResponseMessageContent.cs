using System.Collections.Generic;

namespace Wirehome.Core.Cloud.Messages
{
    public class HttpResponseMessageContent
    {
        public int StatusCode { get; set; } = 200;

        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        public byte[] Content { get; set; }
    }
}
