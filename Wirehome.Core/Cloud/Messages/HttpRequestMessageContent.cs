using System.Collections.Generic;

namespace Wirehome.Core.Cloud.Messages
{
    public class HttpRequestMessageContent
    {
        public string Method { get; set; }

        public string Uri { get; set; }

        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        public byte[] Content { get; set; }
    }
}
